# Undo/Redo Approach Evaluation

This document summarizes the findings from the current Undo/Redo spike implementations.

The evaluated approaches are:

- Command
- Snapshot
- Patch
- Hybrid

The current Hybrid prototype represents:

```
Semantic Action + PatchTransaction
```

It does not yet select between Patch and Snapshot representations depending on the mutation type.

## Comparison

| Criterion | Command | Snapshot | Patch | Hybrid |
|---|---|---|---|---|
| Stored history data | Action-specific command state such as target reference, previous/resulting values, deleted object and original index | Full cloned ProjectState for each history entry | PatchTransaction containing operation-specific before/after data | Semantic action name + PatchTransaction containing operation-specific data |
| History granularity | One semantic command per committed user action | One snapshot per committed mutation | One PatchTransaction per committed user action | One semantic HybridHistoryEntry per committed user action |
| Undo complexity | Each command must implement its own inverse logic | Generic state restoration; no action-specific inverse logic required | Each patch type implements its inverse; transactions compose multiple patches | Delegates reversal to PatchTransaction while retaining semantic action metadata |
| Redo complexity | Each command implements explicit Redo behavior | Generic snapshot restoration | Re-applies the stored PatchTransaction | Re-applies the stored PatchTransaction |
| Object identity | Can preserve object identity when the command stores/restores the original object | Current implementation restores cloned objects, so reference identity is not preserved | Can preserve identity when patches retain original object references | Inherits object-identity behavior from the underlying patches |
| Ordering restoration | Must explicitly store and restore collection position when required | Collection order is contained in the snapshot | Explicitly represented by operations such as move or remove with stored index | Inherits explicit ordering restoration from the underlying patches |
| Compound edits | Requires a dedicated command or additional command-specific state and logic | Naturally represented by one snapshot regardless of mutation complexity | Multiple reusable patches can be grouped into one PatchTransaction | Multiple patches are grouped into one semantic history entry |
| Broad / bulk edits | One command can represent the complete user action, but the command must explicitly store all required reversal data | One full-state snapshot represents the whole bulk mutation | One transaction can contain many fine-grained patches | One semantic entry can contain many fine-grained patches |
| Draft handling | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions |
| Side effects | Not evaluated; current prototype only mutates ProjectState | Not evaluated; current prototype only restores ProjectState | Not evaluated; current prototype only represents ProjectState mutations | Not evaluated; current prototype only represents ProjectState mutations |
| Memory implications | Primarily proportional to the reversal data required by each action | Proportional to snapshot scope × history depth; full-state prototype copies every ConfigItem for each entry | Primarily proportional to the number and size of stored patch operations | Similar to Patch plus semantic action metadata |
| Implementation effort | Simple for small actions, but each new action may require a new command and custom Execute/Undo/Redo logic | Undo/Redo mechanism is generic, but requires cloning/restoration infrastructure | Requires reusable patch types and transaction handling; existing patches can be reused across actions | Adds semantic history abstraction around Patch; new actions can reuse existing patch types when applicable |
| Extensibility | Strong semantic model, but action count can increase the number of command classes | New mutations generally require no new history type as long as the snapshot contains the affected state | New actions can compose existing operations, but new mutation kinds may require new patch types | New semantic actions can reuse existing patches; broader mutation strategies such as targeted Snapshot are not yet evaluated |

## Performance Observations

All timing results in this document are exploratory measurements from the spike and should not be interpreted as formal performance benchmarks.

The bulk scenarios below were measured five times per approach and operation. Because several runs show clear warm-up/runtime noise, especially in the first measurement, the median is used for comparison.

### Earlier Individual Toggle Baseline

The earlier experiment executed 100 individual Toggle actions while varying ProjectState size.

Median execution time:

| Approach | 100 Items | 1000 Items |
|---|---:|---:|
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

The relevant observation in this experiment was the effect of ProjectState size.

The full-state Snapshot prototype cloned the complete ProjectState once for every individual Toggle action. Its cost therefore increased substantially as the state grew from 100 to 1000 ConfigItems.

Command, Patch, and Hybrid did not show comparable state-size-dependent growth for this localized workload.

### Bulk Toggle

Bulk Toggle changes the Active state of every ConfigItem but records the complete user action as one history entry.

#### 100 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0150 ms | 0.0065 ms | 0.0068 ms |
| Snapshot | 0.0220 ms | 0.0300 ms | 0.0343 ms |
| Patch | 0.0400 ms | 0.0412 ms | 0.0390 ms |
| Hybrid | 0.0610 ms | 0.0433 ms | 0.0404 ms |

#### 1000 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0628 ms | 0.0333 ms | 0.0199 ms |
| Snapshot | 0.0913 ms | 0.2571 ms | 0.2677 ms |
| Patch | 1.7906 ms | 4.6173 ms | 4.3340 ms |
| Hybrid | 1.8936 ms | 2.8551 ms | 1.8171 ms |

The current Command implementation scales well for this scenario because BulkToggleCommand stores direct ConfigItem references and before/after values, then restores them with direct assignments.

Snapshot behaves differently from the earlier individual-toggle experiment. A Bulk Toggle is one committed action, so the current implementation creates one complete ProjectState snapshot rather than one snapshot per item. This makes the full-state Snapshot approach comparatively competitive for this broad single action, even though it still copies the whole state.

The current Patch and Hybrid implementations create one ReplaceActivePatch per ConfigItem. ReplaceActivePatch locates its target using a linear search through ProjectState.ConfigItems. As a result, a broad mutation can perform many repeated list searches. The observed scaling therefore reflects both the fine-grained patch representation and the current prototype's lookup strategy.

This result should not be interpreted as evidence that Patch-based Undo/Redo is inherently slow. The current implementation has an important algorithmic limitation that should be isolated in a later experiment.

### Bulk Delete

Bulk Delete removes all ConfigItems whose Active property is true. With the generated data used in the experiment, approximately half of the items are removed. The complete deletion is recorded as one history entry.

#### 100 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.0687 ms | 0.0340 ms | 0.0527 ms |
| Snapshot | 0.0654 ms | 0.0438 ms | 0.0281 ms |
| Patch | 0.2810 ms | 0.0139 ms | 0.0298 ms |
| Hybrid | 0.0810 ms | 0.0141 ms | 0.0374 ms |

#### 1000 Items - Median

| Approach | Execute | Undo | Redo |
|---|---:|---:|---:|
| Command | 0.1632 ms | 0.0775 ms | 2.4196 ms |
| Snapshot | 0.1484 ms | 0.1405 ms | 0.1409 ms |
| Patch | 1.3968 ms | 0.0806 ms | 1.9467 ms |
| Hybrid | 1.3031 ms | 0.0459 ms | 1.3286 ms |

Bulk Delete exposes an important asymmetry between Undo and Redo in the current Command, Patch, and Hybrid implementations.

For Patch and Hybrid, RemoveConfigItemPatch.Undo restores the stored ConfigItem directly at its stored index. This does not require locating the item first.

By contrast, RemoveConfigItemPatch.Apply finds the item by ID before removing it. Bulk Delete Redo therefore repeats ID lookup and removal for many items. The Command implementation has a similar behavior in BulkDeleteCommand.Redo, where each deleted item is located again before removal.

The Snapshot implementation restores cloned ProjectState snapshots for both Undo and Redo. Its work is therefore comparatively uniform across the two directions in this scenario.

The current results show that performance is influenced not only by the history representation itself, but also by how individual operations locate and modify their targets.

## Raw Bulk Measurements

Each row contains five measurements in milliseconds in execution order.

### Bulk Toggle - Execute

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0260, 0.0130, 0.0150, 0.0160, 0.0110 |
| 100 | Snapshot | 0.0250, 0.0220, 0.0180, 0.0180, 0.0230 |
| 100 | Patch | 0.0400, 0.0890, 0.0390, 0.0390, 0.0910 |
| 100 | Hybrid | 0.0600, 0.0700, 0.2230, 0.0320, 0.0610 |
| 1000 | Command | 1.0774, 0.0729, 0.0585, 0.0470, 0.0628 |
| 1000 | Snapshot | 0.5891, 0.0913, 0.1021, 0.0907, 0.0861 |
| 1000 | Patch | 3.0586, 3.3539, 1.7906, 1.7740, 1.7773 |
| 1000 | Hybrid | 1.8936, 1.7366, 2.2803, 2.2559, 1.7823 |

### Bulk Toggle - Undo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0065, 0.0068, 0.0063, 0.0062, 0.0083 |
| 100 | Snapshot | 0.3270, 0.0293, 0.0300, 0.0292, 0.0302 |
| 100 | Patch | 0.4753, 0.0372, 0.0412, 0.0366, 0.1356 |
| 100 | Hybrid | 0.1849, 0.0891, 0.0430, 0.0325, 0.0433 |
| 1000 | Command | 0.3435, 0.0550, 0.0269, 0.0245, 0.0333 |
| 1000 | Snapshot | 0.8881, 0.2187, 0.1721, 0.2571, 0.3651 |
| 1000 | Patch | 9.0421, 4.7426, 1.8236, 1.8708, 4.6173 |
| 1000 | Hybrid | 2.5705, 2.8551, 1.8231, 7.3479, 5.1991 |

### Bulk Toggle - Redo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.2448, 0.0551, 0.0067, 0.0059, 0.0068 |
| 100 | Snapshot | 0.1258, 0.0339, 0.0343, 0.0263, 0.0457 |
| 100 | Patch | 0.1200, 0.0308, 0.0390, 0.0412, 0.0377 |
| 100 | Hybrid | 0.1234, 0.0367, 0.0404, 0.1258, 0.0350 |
| 1000 | Command | 0.2713, 0.0543, 0.0199, 0.0194, 0.0184 |
| 1000 | Snapshot | 0.2677, 0.1747, 0.4008, 0.1682, 0.4234 |
| 1000 | Patch | 5.1339, 1.8468, 9.2158, 2.2535, 4.3340 |
| 1000 | Hybrid | 1.8171, 1.8323, 1.6972, 4.0323, 1.7351 |

### Bulk Delete - Execute

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 2.6013, 0.0961, 0.0687, 0.0337, 0.0347 |
| 100 | Snapshot | 1.0944, 0.0692, 0.0654, 0.0408, 0.0487 |
| 100 | Patch | 1.2381, 0.5029, 0.1355, 0.2810, 0.0682 |
| 100 | Hybrid | 0.2325, 0.0734, 0.2028, 0.0792, 0.0810 |
| 1000 | Command | 0.1876, 0.1758, 0.1520, 0.1632, 0.1440 |
| 1000 | Snapshot | 0.1420, 0.1783, 0.1484, 0.1698, 0.1410 |
| 1000 | Patch | 1.3968, 1.3716, 1.3326, 1.4874, 1.4519 |
| 1000 | Hybrid | 1.2501, 1.3072, 1.2353, 1.3031, 1.5433 |

### Bulk Delete - Undo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0311, 0.0340, 0.0265, 0.0403, 0.1723 |
| 100 | Snapshot | 0.0695, 0.0317, 0.0301, 0.0475, 0.0438 |
| 100 | Patch | 0.0139, 0.0234, 0.0128, 0.0139, 0.0098 |
| 100 | Hybrid | 0.0114, 0.0218, 0.0110, 0.0712, 0.0141 |
| 1000 | Command | 0.0588, 0.0994, 0.0775, 0.0511, 0.0844 |
| 1000 | Snapshot | 0.1305, 0.2882, 0.1342, 0.1405, 0.2907 |
| 1000 | Patch | 0.0918, 0.0806, 0.0886, 0.0707, 0.0494 |
| 1000 | Hybrid | 0.0459, 0.0579, 0.0395, 0.0459, 0.0823 |

### Bulk Delete - Redo

| Items | Approach | Runs |
|---|---|---|
| 100 | Command | 0.0527, 0.1058, 0.0404, 0.0395, 0.1043 |
| 100 | Snapshot | 0.0518, 0.0235, 0.0242, 0.0281, 0.0558 |
| 100 | Patch | 0.0298, 0.0375, 0.0365, 0.0220, 0.0246 |
| 100 | Hybrid | 0.0270, 0.0374, 0.0438, 0.0693, 0.0231 |
| 1000 | Command | 3.0641, 2.2170, 2.4535, 1.4715, 2.4196 |
| 1000 | Snapshot | 0.2364, 0.1409, 0.2692, 0.1361, 0.1350 |
| 1000 | Patch | 4.9678, 2.3710, 1.2086, 1.9467, 1.2381 |
| 1000 | Hybrid | 1.3286, 3.7622, 1.2076, 1.2010, 3.3895 |

## History Representation

For the Compound Edit experiment, all approaches represented one committed user action as one Undo history entry.

Command:

```
CompoundEditCommand
```

Snapshot:

```
ProjectState snapshot (3 ConfigItems)
```

Patch:

```
replace Name + replace Active + move ConfigItem
```

Hybrid:

```
Compound Edit [replace Name + replace Active + move ConfigItem]
```

Command provides the clearest domain-level representation but requires action-specific reversal logic.

Snapshot stores the previous state without requiring knowledge of the individual mutations, but does not preserve the semantic meaning of the action.

Patch exposes the concrete mutations and allows existing operations to be composed into transactions.

Hybrid retains both the semantic user action and the concrete Patch operations used to reverse it.

For Bulk Actions, the same one-user-action / one-history-entry rule is retained.

A Bulk Toggle of many items is represented as:

Command:

```
BulkToggleCommand
```

Snapshot:

```
ProjectState snapshot
```

Patch:

```
replace Active + replace Active + ...
```

Hybrid:

```
Bulk Toggle [replace Active + replace Active + ...]
```

This exposes a representation trade-off that was less visible in the localized experiments: a single broad semantic action can correspond to a large number of low-level Patch operations.

## State Size and History Storage

The current Snapshot implementation clones the complete ProjectState before every committed action.

For the earlier 100-individual-Toggle experiment:

```
100 ConfigItems
-> 10,000 stored ConfigItem copies

1000 ConfigItems
-> 100,000 stored ConfigItem copies
```

This storage growth came from both state size and history depth: 100 committed actions produced 100 snapshots.

For one Bulk Toggle, however, the history depth increases by only one entry.

Therefore:

```
100 ConfigItems
-> one snapshot containing 100 ConfigItems

1000 ConfigItems
-> one snapshot containing 1000 ConfigItems
```

This distinction is important. Snapshot cost depends on both snapshot scope and the number of committed history entries.

The Command, Patch, and current Hybrid implementations do not store a complete copy of ProjectState for each action.

Their history data is instead primarily related to the state required to reverse the mutation. For broad Patch/Hybrid actions, however, the number of stored low-level operations can grow with the number of affected items.

## Current Findings

### Command

Command gives each history entry a clear domain meaning.

This works naturally for small semantic actions such as Toggle Active and Delete Config Item.

The same model also represents Bulk Toggle and Bulk Delete as one semantic history entry.

For Bulk Toggle, the current BulkToggleCommand stores direct ConfigItem references and before/after Active values. Execute, Undo, and Redo can therefore update those objects directly without searching ProjectState for every item.

The main cost remains implementation complexity: each new action may require its own command class and its own Execute, Undo, and Redo logic.

CompoundEditCommand demonstrated that this logic grows as one action modifies more properties or collection positions.

BulkDeleteCommand also demonstrates that reversal implementation details matter. Undo can restore stored objects at stored indices, while Redo currently searches the collection for each deleted item before removing it again.

### Snapshot

Snapshot provides the most generic reversal mechanism in the current spike.

A mutation can change multiple properties and collection positions without requiring operation-specific inverse logic.

The main disadvantages observed in the current full-state prototype are:

- storage grows with snapshot scope and history depth
- restoring cloned state does not preserve object reference identity
- history does not describe the semantic action that created the state

The earlier individual-toggle benchmark showed significant cost when the whole ProjectState was cloned for every small action.

The Bulk Action experiment adds an important counter-observation: when a broad mutation is represented as one committed action, only one full snapshot is required. For Bulk Toggle and Bulk Delete, this made the current Snapshot implementation comparatively competitive in execution time.

Snapshot scope therefore remains an important design decision.

The current experiment only evaluates complete ProjectState snapshots.

### Patch

Patch separates Undo/Redo from domain-specific command classes by representing mutations as reusable low-level operations.

Existing operations can be composed into a PatchTransaction.

For example:

```
replace Name
replace Active
move ConfigItem
```

can form one Compound Edit transaction.

The implementation also demonstrated that transaction failure handling requires explicit rollback logic so that partially applied or partially undone transactions do not leave ProjectState inconsistent.

The Bulk Action experiment shows another trade-off: one semantic action may expand into many low-level patch operations.

For Bulk Toggle, the current implementation creates one ReplaceActivePatch for every ConfigItem. ReplaceActivePatch currently performs a linear search by ID when it applies or undoes the change. This repeated lookup contributes strongly to the observed cost at 1000 items.

For Bulk Delete, RemoveConfigItemPatch.Undo can restore the stored object directly at its stored index, while Apply locates the item before removal. This creates a visible difference between Undo and Redo cost.

These results describe the current prototype implementation. They do not establish that Patch-based history is inherently slower than the other approaches.

Patch history describes concrete mutations well, but does not by itself retain the semantic user intent behind those mutations.

### Hybrid

The current Hybrid prototype combines:

```
Semantic Action
    +
PatchTransaction
```

For example:

```
Compound Edit
[replace Name + replace Active + move ConfigItem]
```

This retains user-level semantic information while reusing the Patch-based reversal mechanism.

For Bulk Actions, Hybrid retains one semantic entry such as `Bulk Toggle` or `Bulk Delete`, while the underlying transaction may contain many patch operations.

This preserves readable history semantics, but the current Hybrid implementation also inherits the broad-operation behavior of the Patch prototype. In particular, Bulk Toggle still creates one ReplaceActivePatch per item and therefore inherits the repeated target-lookup cost.

The current Hybrid implementation still uses PatchTransaction for every tested action.

It has therefore not yet demonstrated whether different mutation patterns should use different history representations.

## Not Yet Evaluated

The following areas remain open:

- Draft-level Undo versus committed transaction Undo
- Cancel / Escape behavior relative to Undo history
- Native text-field Undo versus global Project Undo
- Side effects outside ProjectState
- Create operations
- Larger Move / Reorder scenarios
- Import / Merge-like mutations
- Targeted Snapshot scope
- Mixed Patch / Snapshot Hybrid history
- Formal memory measurements
- Formal performance benchmarking with warm-up and a larger number of measured runs
- Patch/Hybrid performance with a more efficient target lookup strategy

## Next Evaluation

The Bulk Action experiment provides evidence that mutation shape matters.

It also exposes two separate questions that should not be conflated:

1. How much of the current Patch/Hybrid bulk cost comes from the history representation itself?
2. How much comes from the prototype's repeated linear target lookup?

A useful next step is therefore to isolate the lookup effect, for example by providing a more efficient way to resolve ConfigItems by ID, and then repeat the broad-mutation measurements.

After that, the spike can evaluate a mixed Hybrid strategy such as:

```
Local / small mutation
-> Semantic Action + PatchTransaction

Broad / complex mutation
-> Semantic Action + Targeted Snapshot
```

This would allow the targeted Snapshot hypothesis to be compared against a less lookup-bound Patch implementation.

Create and larger Move / Reorder scenarios should also be added so that the comparison is not based only on property updates and deletions.

## Cross-Approach Observation

The experiments increasingly indicate that the suitability of an Undo/Redo representation depends on the mutation pattern and the implementation strategy, rather than only on whether an approach can technically support an operation.

Localized mutations favor compact reversal data.

Broad single actions change the trade-off:

- Command can remain compact and efficient when it stores exactly the required references and values.
- Snapshot pays for state capture, but only once per committed bulk action.
- Patch can reuse generic operations, but a broad action may create many operations.
- Hybrid preserves semantic meaning while currently inheriting the operation cost of Patch.

The current results therefore support continued evaluation of a mixed representation strategy, while also showing that algorithmic details such as target lookup must be controlled before drawing performance conclusions.