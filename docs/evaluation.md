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

## 7. Overall Spike Conclusion

The spike does not support selecting one universal Undo/Redo representation for every mutation.

The strongest architecture demonstrated by the prototype is **Hybrid v2 with Patch as the default reversal mechanism and Snapshot available for selected broad or structurally complex operations**.

Recommended direction:

```
User Action
    |
    v
Semantic HybridHistoryEntry
    |
    +-- localized / identity-sensitive action
    |       -> Patch-backed operation
    |
    +-- broad / structurally complex action
            -> Snapshot-backed operation
```

Patch should be the normal path for updates, create/duplicate, delete, move/reorder, and compound actions because it stores only relevant reversal data and composes well.

Snapshot should remain an optional strategy rather than the default. It is useful when representing the inverse mutation as many individual operations becomes more complex than restoring state, but its object-identity and full-state-copying behavior must be considered.

The performance experiments also show an important implementation lesson:

> The measured performance of an Undo/Redo architecture can be dominated by target lookup and collection algorithms rather than by the history representation itself.

Therefore, production Patch operations should use an efficient ID resolver/index where appropriate instead of repeatedly scanning the complete collection.

The spike also supports defining history at the **committed user-action level**. A transaction containing several low-level changes should normally appear as one Undo step.

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

## 10. Remaining Questions for Production Integration

The main architectural spike is complete. The following topics still require product-level decisions in MobiFlight:

- Applying the demonstrated commit-boundary model to the real MobiFlight editors
- Production integration of native text-field Ctrl+Z versus global Project Undo
- Side effects outside ProjectState
- Production-level persistent ID resolver/index
- Reference-identity requirements across Snapshot restores
- History depth / memory limits
- Import / Merge transaction boundaries

These are integration questions rather than reasons to continue the current architecture microbenchmarks.
