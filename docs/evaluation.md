# Undo/Redo Spike – Final Evaluation

## 1. Goal

This spike compared four Undo/Redo approaches for MobiFlight-style project mutations:

- Command
- Snapshot
- Patch
- Hybrid

The evaluated mutation types were:

- Update: Toggle Active
- Delete: Delete Config Item
- Compound edit: Name + Active + Move
- Broad update: Bulk Toggle
- Broad delete: Bulk Delete
- Create: Duplicate Config Item
- Move / reorder: Move First to Last

The purpose was not to select one mechanism solely from microbenchmark results, but to understand the trade-offs in history representation, implementation effort, identity handling, scalability, and extensibility.

---

## 2. Approach Summary

| Approach | History Representation | Main Strength | Main Trade-off |
|---|---|---|---|
| Command | One action-specific command per user action | Clear semantic intent and compact action-specific state | New actions often require new command classes and custom Undo/Redo logic |
| Snapshot | Previous ProjectState clone | Generic Undo/Redo with little mutation-specific logic | Full-state copying, reference identity replacement, limited semantic information |
| Patch | PatchTransaction containing reusable low-level operations | Composable, compact for localized changes, reusable operations | Requires patch types and efficient target resolution |
| Hybrid v2 | Semantic action + Patch-backed or Snapshot-backed operation | Keeps semantic history while allowing different reversal strategies | Requires a policy for choosing Patch vs Snapshot |

The final Hybrid v2 structure is:

```
HybridHistoryEntry
    |
    +-- semantic ActionName
    |
    +-- IHybridHistoryOperation
            |
            +-- HybridPatchOperation
            |       -> PatchTransaction
            |
            +-- HybridSnapshotOperation
                    -> ProjectState snapshot swap
```

The same Undo/Redo stack can therefore contain both Patch-backed and Snapshot-backed history entries.

---

## 3. Benchmark Method

The benchmark results are exploratory spike measurements, not production-grade performance numbers.

Later experiments used:

```
1 warm-up cycle:
Reset -> Execute -> Undo -> Redo

then 10 measured cycles:
Reset -> Execute -> Undo -> Redo
```

Median values are used as the primary summary statistic.

The Indexed Bulk Toggle Execute timing includes dictionary creation.

---

## 4. Main Performance Findings

### 4.1 Individual Toggle

100 individual Toggle actions showed the main weakness of full-state Snapshot when many small actions are recorded.

| Approach | 100 Items | 1000 Items |
|---|---:|---:|
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

Snapshot clones the complete ProjectState for every committed action, so state size and history depth both matter.

---

### 4.2 Bulk Toggle – 1000 Items

The original Patch and Hybrid implementations used a linear ID lookup for every Patch operation.

| Variant | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0628 ms | 0.0333 ms | 0.0199 ms |
| Snapshot | 0.0913 ms | 0.2571 ms | 0.2677 ms |
| Patch - linear ID lookup | 1.7906 ms | 4.6173 ms | 4.3340 ms |
| Hybrid - linear ID lookup | 1.8936 ms | 2.8551 ms | 1.8171 ms |
| Patch - direct reference | 0.0470 ms | 0.0281 ms | 0.0286 ms |
| Hybrid - direct reference | 0.0446 ms | 0.0274 ms | 0.0290 ms |
| Patch - indexed ID lookup | 0.0902 ms | 0.0477 ms | 0.0526 ms |
| Hybrid - indexed ID lookup | 0.1114 ms | 0.0535 ms | 0.0521 ms |

The important result is that the original Patch/Hybrid slowdown was mainly caused by repeated linear lookup, not by having many Patch operations.

Direct references were useful as a control experiment, but they are not a preferred production representation because references can become stale after a Snapshot restore.

Indexed Guid lookup keeps logical identity while avoiding repeated full-list scans.

---

### 4.3 Bulk Delete – 1000 Items

| Variant | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.1632 ms | 0.0775 ms | 2.4196 ms |
| Snapshot | 0.1484 ms | 0.1405 ms | 0.1409 ms |
| Patch - linear lookup | 1.3968 ms | 0.0806 ms | 1.9467 ms |
| Hybrid - linear lookup | 1.3031 ms | 0.0459 ms | 1.3286 ms |
| Patch - stored index | 0.2912 ms | 0.0616 ms | 0.0697 ms |
| Hybrid - stored index | 0.2283 ms | 0.1110 ms | 0.0519 ms |

Removing repeated lookup greatly improved Execute and especially Redo.

Bulk Delete still has structural List mutation costs because repeated `RemoveAt` / `Insert` operations can shift elements. This is different from Bulk Toggle, where target resolution was the dominant issue.

Snapshot has comparatively uniform work for broad structural changes, but it replaces object instances on restore.

---

## 5. History and Storage Findings

### Command

History storage contains only action-specific reversal data.

This is compact, but each semantic action must define its own Execute, Undo, and Redo behavior.

### Snapshot

The current prototype stores full ProjectState snapshots.

For 100 individual Toggle actions:

```
100 items  -> 10,000 stored ConfigItem copies
1000 items -> 100,000 stored ConfigItem copies
```

This makes Snapshot simple but potentially expensive when many small edits are recorded.

Snapshot restore also replaces ConfigItem object instances, so C# reference identity is not preserved.

### Patch

History stores only the low-level operations and the before/after data required by those operations.

Patch storage grows mainly with the amount of changed data rather than the complete project size.

The experiments showed that target-resolution strategy is important. A production implementation should avoid repeated linear scans for high-volume Patch actions.

### Hybrid v2

Hybrid adds semantic action information on top of a selectable reversal strategy.

Patch-backed and Snapshot-backed entries can coexist in the same history stack. This was verified by the Hybrid v2 test that executes:

```
Patch action
-> Snapshot action
-> Undo Snapshot
-> Undo Patch
-> Redo Patch
-> Redo Snapshot
```

---

## 6. Spike Recommendation by Mutation Type

**Historical recommendation:** This section documents the initial architecture recommendation based on project-level mutation experiments. It should not be interpreted as the final production decision. The recommendation was subsequently reassessed in Section 7, taking implementation complexity and the narrower ConfigItem editor scope into account.

The following table is the conclusion of this spike, based on the current prototype and measurements.

| Mutation type | Preferred representation from this spike | Reason |
|---|---|---|
| Simple property update | Hybrid + Patch | Small before/after state, easy to compose, full Snapshot is unnecessary |
| Single item delete | Hybrid + Patch | Store removed item + original position; compact and identity-preserving |
| Create / Duplicate | Hybrid + Patch (`AddConfigItemPatch`) | Store created item + insertion position; Undo is a simple remove and Redo can restore the same logical item |
| Move / Reorder | Hybrid + Patch (`MoveConfigItemPatch`) | Source/target positions and item ID are sufficient; Snapshot would copy unrelated state |
| Compound edit | Hybrid + PatchTransaction | Several reusable patches can form one semantic user action without a dedicated command class |
| Bulk property update | Hybrid + indexed Patch | Benchmark shows fine-grained Patch remains inexpensive when target lookup is efficient |
| Bulk delete / broad structural edit | Hybrid; choose Snapshot or optimized Patch by constraints | Snapshot is simple and uniform; optimized Patch preserves object identity and gives fast Undo/Redo |
| Very broad / import / merge-like change | Snapshot-backed Hybrid candidate | A snapshot can be simpler than maintaining a very large set of structural inverse operations |

### Create / Duplicate

For the current Duplicate scenario, Patch-backed Hybrid is the strongest fit.

```
Duplicate Config Item
-> HybridHistoryEntry
-> HybridPatchOperation
-> AddConfigItemPatch
```

Only the created item and insertion position need to be stored. A full ProjectState Snapshot would copy unrelated data, while a dedicated Command would duplicate logic that is already reusable as a Patch.

### Delete

For a normal single-item Delete, Patch-backed Hybrid is preferred.

The Patch can retain the removed item and its original index. This naturally restores both the item and its position.

For a very large Bulk Delete, there is no single universal winner:

- Snapshot is simpler and had lower Execute time in the measured 1000-item case.
- Stored-index Patch had faster Undo/Redo and preserves object identity.

The choice should therefore depend on whether preserving live object identity is important and how complex the structural mutation becomes.

### Move / Reorder

Patch-backed Hybrid is preferred.

A move can be represented with:

```
item ID
from index
to index
```

This is much smaller than cloning the complete project and can be reused in compound edits.

### Compound Actions

Hybrid + PatchTransaction is particularly useful when one user action performs several low-level changes.

Example:

```
"Compound Edit"
    -> ReplaceNamePatch
    -> ReplaceActivePatch
    -> MoveConfigItemPatch
```

The UI/history still sees one semantic action while the reversal logic remains reusable.

---

## 7. Spike Conclusion and Architecture Reassessment

### 7.1 Technical Findings

The spike successfully implemented and compared four Undo/Redo approaches: Command, Snapshot, Patch, and Hybrid.

The experiments demonstrated that:

- Command provides clear semantic actions and explicit reversal logic.
- Snapshot provides generic restoration without requiring action-specific inverse operations.
- Patch provides reusable, fine-grained reversible mutations.
- Hybrid can combine different history representations.

The benchmarks showed lower costs for Command and Patch in many localized operations, while Snapshot was generally simpler to implement for broad mutations.

However, these results were obtained using a simplified ProjectState.

They do not directly establish the performance or memory costs of the real MobiFlight Connector.

### 7.2 Findings from Sebastian's Review

The architecture review raised several important concerns.

First, the implementation complexity of PatchTransaction and individual Patch operations may not be justified by the measured performance improvements.

Second, Snapshot operations may be sufficiently fast for realistic MobiFlight projects, where the number of ConfigItems is expected to be substantially smaller than the largest stress-test cases.

Third, Snapshot history entries should retain semantic action descriptions. This can be achieved by attaching metadata to the history entry and does not require using Command-based reversal.

Fourth, the production architecture must account for the existing React frontend and .NET backend separation.

Undo/Redo must restore the authoritative Project state while keeping the frontend synchronized with the resulting state.

### 7.3 Revised Architecture Decision

The initial spike favored Hybrid with Patch as the default reversal strategy.

After the architecture review, this recommendation is no longer considered a final production decision.

The preferred candidates for further evaluation are:

1. Command-based Undo/Redo
2. Semantic Snapshot-based Undo/Redo

Patch and Hybrid remain documented alternatives but are not currently the preferred starting point for production integration.

The final choice should prioritize:

- simplicity of implementation,
- maintainability,
- correct state restoration,
- frontend/backend synchronization,
- side-effect handling,
- realistic memory consumption.

Raw execution speed should only become a deciding factor when measurements demonstrate a meaningful impact on the application.

### 7.4 Production-Oriented Evaluation Status

The following additional investigations have been completed:

- Semantic action descriptions and failure handling for Snapshot history.
- ConfigItem editing with Apply/Cancel boundaries.
- Local Item Snapshot Undo/Redo with experimental history limits of 10, 25, and 50 steps.
- Performance and memory benchmarks using actual MobiFlight ConfigItems.
- Preliminary verification of nested configuration object cloning.
- Automated tests for local history behavior, including Undo/Redo, history overflow, Redo invalidation, and history resizing.

The frontend history unit tests, production build, and lint checks have passed.

These findings support Item Snapshot as the current preferred candidate for editor-local Undo/Redo, subject to review.

The findings do not imply that Item Snapshot is necessarily the preferred strategy for committed ConfigItem list actions or project-wide Undo/Redo.

Remaining validation:

- Manual verification of editor interactions.
- Integration tests with the real ConfigItem editor and nested settings.
- Confirmation of the initial production scope and history capacity.
- Verification of synchronization and side effects for committed configuration changes.

**Decision status: Ready for architecture and scope review. Final production approval pending.**

---

## 8. History Navigation Experiment

The prototype also evaluated Photoshop-style history navigation.

The frontend exposes the current Undo and Redo stacks through the
History Inspector. A user can select an earlier or later history
entry and move through several entries at once.

This is implemented through:

```
UndoTo(n)
RedoTo(n)
```

The selected history entries are temporarily represented as one grouped History Jump entry.

Example:

```
Action A
Action B
Action C
    |
    | UndoTo(2)
    v
Action A
Redo:
History Jump (2 actions)
```


Redoing the grouped entry restores B and C and expands them back into their original individual history entries.

The same mechanism was implemented and tested for:

- Command
- Snapshot
- Patch
- Hybrid

History Jump entries preserve their semantic action count even when an existing History Jump is included in another jump.

For example:

```
Action A
History Jump (2 actions)
```

grouped together represents:

```
History Jump (3 actions)
```

rather than two actions.

### Failure Handling

Multi-step history navigation must be atomic.

A failure while executing one operation in `UndoTo` or `RedoTo` must not leave either the ProjectState or the history stacks in a partially modified state.

The Patch and Hybrid implementations therefore:

1. inspect the target history entries without modifying the stack,
2. execute the requested Undo or Redo operations,
3. roll back already completed operations if a later operation fails,
4. modify the history stacks only after the complete jump succeeds.

This behavior is covered by tests for failed multi-step Undo and Redo.

### Hybrid History Navigation

History navigation was also tested with Patch-backed and Snapshot-backed entries mixed in the same Hybrid history.

For example:

```
Patch-backed Toggle
-> Snapshot-backed Delete
-> Patch-backed Rename
-> UndoTo(3)
-> Redo grouped History Jump
```

The sequence restores the expected ProjectState even though the Snapshot operation replaces object instances.

This reinforces the earlier finding that Patch operations should resolve their targets by persistent logical ID rather than relying on long-lived direct object references.

### Finding

History navigation is primarily a UX capability layered on top of the history mechanism.

It does not materially change the architectural comparison between Command, Snapshot, Patch, and Hybrid.

The experiment therefore does not change the main recommendation:

> Hybrid history with Patch as the default reversal strategy and
> Snapshot available for selected broad changes remains the strongest
> direction demonstrated by this spike.

---

## 9. Interaction Boundary Experiment

The spike also evaluated where Undo history should be created during form-based editing.

A small Config Item editor was used to distinguish temporary UI state from committed ProjectState mutations.

The tested interaction was:

```
Open editor
-> modify Name
-> modify Active
-> Apply
```

While the editor is open, the modified values remain local draft state in the React frontend.

No ProjectState mutation and no global history entry are created until Apply is selected.

### Draft State
Opening the editor copies the current Config Item values into local frontend state.

For example:

```
ProjectState

Name   = "Config Item 1"
Active = true

        |
        | Open editor
        v

Draft

Name   = "Config Item 1"
Active = true
```

Subsequent typing or checkbox changes modify only the draft.

```
Draft

Name   = "Landing Light"
Active = false

ProjectState

Name   = "Config Item 1"
Active = true
```

The ProjectState and Project history therefore remain unchanged during an unfinished interaction.

### Apply

Apply sends the final draft values to the backend as one request.

```
PUT /api/hybrid/config-items/{id}
```

The backend compares the committed state with the submitted draft and creates Patch operations only for values that actually changed.
Example:

```
Edit Config Item
    |
    +-- ReplaceNamePatch
    |
    +-- ReplaceActivePatch
```

Both low-level changes are stored inside one semantic Hybrid history entry:

```
Edit Config Item
[replace Name + replace Active]
```

Therefore:

```
several draft edits
-> one Apply
-> one global Undo step
```

Undoing that entry once restores the complete state from before Apply.

Redoing it once reapplies the complete committed edit.

### No-op Apply

Applying an editor without changing any values does not create a history entry.

```
Open
-> no changes
-> Apply
-> history unchanged
```

This avoids recording meaningless Undo steps.

### Cancel and Escape

Cancel and Escape discard the frontend draft without sending a mutation request to the backend.

```
Open editor
-> modify draft
-> Cancel / Escape
-> discard draft
```

This is intentionally different from Undo.

Cancel operates on an unfinished interaction.

Undo operates on an already committed ProjectState transition.

### Local and Global Undo

The prototype also verified that native text editing and global Project Undo can coexist.

When a text input is focused:

```
Ctrl+Z
-> native browser text Undo
```

The global Project history handler ignores the shortcut.

While the draft editor is open, other global mutation shortcuts are also suppressed so that unfinished editor state does not accidentally interact with Project history.

After Apply closes the editor:

```
Ctrl+Z
-> global Project Undo
```

The complete committed edit is reverted as one history action.

### Finding

The experiment supports a transaction-level model for form-based Project editing:

```
Draft UI state
    |
    | Apply
    v
Committed semantic action
    |
    v
Global Project history
```

For MobiFlight-style editors, temporary field edits should normally remain outside the global Undo history.

A global history entry should be created at the point where the user commits the interaction, such as:

- Apply
- successful Drop
- confirmed Add
- confirmed Delete
- completed inline rename

This keeps Project Undo aligned with meaningful user actions rather than individual UI events.

---

## 10. Benchmark Using Real MobiFlight Configuration Items

### Benchmark Setup

A .NET benchmark was implemented to compare the execution time and memory usage of three Undo/Redo approaches:

- Command: Stores only the information required to reverse and reapply an action.
- Item Snapshot: Stores a copy of the affected ConfigItem.
- Full Snapshot: Stores a copy of the entire ConfigItem collection.

The benchmark used 14 real MobiFlight ConfigItems as templates to generate datasets containing 25, 100, and 500 items.

For each dataset, 100 Toggle Active operations were executed, followed by 100 Undo and 100 Redo operations.

The measurements included execution time, memory allocated during the execution phase, and an approximation of retained memory.

### Benchmark Results

The following table presents the results for the dataset containing 500 ConfigItems, with 100 operations performed in each phase.

| Measured Metric | Command | Item Snapshot | Full Snapshot |
|---|---|---|---|
| Execute (100 operations) | 0.023 ms | 0.075 ms | 75.345 ms |
| Undo (100 operations) | 0.007 ms | 0.119 ms | 134.444 ms |
| Redo (100 operations) | 0.002 ms | 0.091 ms | 155.968 ms |
| Memory allocated during Execute | 3 kB | 77 kB | 37,557 kB |
| Estimated retained memory after Execute | 5.5 kB | 126.9 kB | 25,807 kB |

Across 100 Toggle Active operations, Command and Item Snapshot demonstrated considerably lower execution and memory allocation costs than Full Snapshot.

For the 500-item dataset, Full Snapshot was approximately 1,005 times slower than Item Snapshot during the execution phase and allocated approximately 485 times more memory.

However, the total execution time of 75.345 ms represents 100 consecutive operations. This corresponds to an average of approximately 0.75 ms per operation, which may not result in a noticeable delay for users.

Memory consumption is potentially more significant. Full Snapshot copies the entire collection even when only a single ConfigItem is modified.

Consequently, executing 100 individual changes in a collection of 500 ConfigItems results in approximately 50,000 ConfigItem copies being created during the execution phase alone.

The reported retained memory values are approximate measurements based on garbage collection and should not be interpreted as exact memory footprints of the history entries.

Furthermore, these results were obtained from a single benchmark run and should be treated as preliminary rather than statistically conclusive.

### Relevance to the Current Implementation Scope

Although the benchmark demonstrates the scalability differences between the approaches, the larger datasets do not directly represent the current implementation target.

The current goal is to implement Undo/Redo within an individual ConfigItem editor, rather than project-wide or global Undo/Redo.

Each editor is expected to maintain a temporary history of changes made to its associated ConfigItem. When the user applies or cancels the changes and closes the editor, the temporary history can be discarded.

Therefore, copying hundreds of ConfigItems for every editing operation would not be necessary for the current scope.

The evaluation should instead focus on the performance and correctness of Command and Item Snapshot when handling individual ConfigItems, including their nested configuration data.

### Maximum History Depth

An additional consideration for Snapshot-based Undo/Redo is the maximum number of history entries retained during an editing session.

Even when only one ConfigItem is involved, storing a new snapshot after every meaningful change may increase memory usage as the history grows.

Therefore, a maximum history depth should be defined to prevent unbounded memory consumption.

For the next evaluation, history limits of 10, 25, and 50 entries can be used as experimental scenarios. These values are not proposed as final product limits.

The evaluation should examine:

- Memory consumption at different history depths.
- The performance of Undo and Redo with bounded history.
- The behavior when the maximum history capacity is reached.
- Whether the oldest history entries can be discarded without affecting the remaining Undo/Redo operations.
- Whether the history is correctly discarded when the editor is closed.

The final history limit should balance practical editing needs, memory consumption, and implementation simplicity.

### Bounded Item Snapshot History Benchmark

An additional benchmark was conducted to evaluate the memory consumption of bounded Item Snapshot history using actual MobiFlight ConfigItems.

The experiment loaded 14 real ConfigItems and applied 100 modifications to each item. Every ConfigItem maintained its own independent snapshot history with a maximum capacity of 10, 25, or 50 stored snapshots.

| Measured Metric | 10 Snapshots | 25 Snapshots | 50 Snapshots |
|---|---|---|---|
| Execute time | 0.787 ms | 0.904 ms | 0.634 ms |
| Allocated memory | 1,119.6 KB | 1,121.5 KB | 1,125.1 KB |
| Estimated retained memory | 111.6 KB | 234.8 KB | 455.4 KB |
| Total stored snapshots | 140 | 350 | 700 |

All benchmark tests passed.

The results indicate that estimated retained memory increases with the number of stored snapshots, while execution time and total memory allocation remain relatively similar across the three limits.

This is expected because each scenario executes the same number of modifications and creates the same number of snapshots, regardless of how many snapshots are retained.

Although the benchmark maintained histories for 14 ConfigItems simultaneously, the current editor-local Undo/Redo scope generally requires history for only the ConfigItem being edited.

Therefore, the memory overhead of a bounded Item Snapshot history appears manageable for the current scope.

A default history limit of 25 steps remains a reasonable experimental configuration, while 50 steps should remain available for further evaluation.

However, these measurements were obtained from a single benchmark run, and the results should not be interpreted as definitive production memory requirements.

The benchmark also verified that cloning preserves the tested basic properties and creates separate instances of several major nested configuration objects. More detailed tests are still needed to verify the independence of all nested mutable data.

Remaining considerations:

- Define history capacity consistently in terms of undoable actions rather than only stored snapshots.
- Verify Undo/Redo correctness when the maximum history capacity is reached.
- Test more complex nested configuration modifications.
- Confirm the lifetime and default capacity of editor-local history.
- Evaluate separate history policies for future profile-level, project-level, and global Undo/Redo.

These findings support further consideration of Item Snapshot as a practical approach for ConfigItem editor Undo/Redo, subject to final architectural review.

### Conclusion and Future Considerations

The benchmark confirms that Command and Item Snapshot have significantly lower memory allocation costs than Full Snapshot for localized configuration changes.

However, the results do not establish a final production architecture.

For the current ConfigItem-level Undo/Redo scope, implementation simplicity, correctness, maintainability, and bounded history storage should take priority over large-scale performance optimization.

Future consideration: If MobiFlight introduces project-wide or global Undo/Redo, the larger-scale benchmark results may become more relevant.

Such an extension would require further evaluation of memory consumption, history lifetime, object reference identity, side effects, and synchronization between the project state and running configurations.

---

## 11. Remaining Questions for Production Integration

The main architectural spike is complete. The following topics still require product-level decisions in MobiFlight:

- Applying the demonstrated commit-boundary model to the real MobiFlight editors
- Production integration of native text-field Ctrl+Z versus global Project Undo
- Side effects outside ProjectState
- Production-level persistent ID resolver/index
- Reference-identity requirements across Snapshot restores
- History depth / memory limits
- Import / Merge transaction boundaries

These are integration questions rather than reasons to continue the current architecture microbenchmarks.
