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
| Draft handling | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions | Not evaluated; current prototype records committed backend actions |
| Side effects | Not evaluated; current prototype only mutates ProjectState | Not evaluated; current prototype only restores ProjectState | Not evaluated; current prototype only represents ProjectState mutations | Not evaluated; current prototype only represents ProjectState mutations |
| Memory implications | Primarily proportional to the reversal data required by each action | Proportional to snapshot scope × history depth; full-state prototype copies every ConfigItem for each entry | Primarily proportional to the number and size of stored patch operations | Similar to Patch plus semantic action metadata |
| Implementation effort | Simple for small actions, but each new action may require a new command and custom Execute/Undo/Redo logic | Undo/Redo mechanism is generic, but requires cloning/restoration infrastructure | Requires reusable patch types and transaction handling; existing patches can be reused across actions | Adds semantic history abstraction around Patch; new actions can reuse existing patch types when applicable |
| Extensibility | Strong semantic model, but action count can increase the number of command classes | New mutations generally require no new history type as long as the snapshot contains the affected state | New actions can compose existing operations, but new mutation kinds may require new patch types | New semantic actions can reuse existing patches; broader mutation strategies such as targeted Snapshot are not yet evaluated |

## Performance Observation

Median execution time for 100 Toggle operations:

| Approach | 100 Items | 1000 Items |
|---|---:|---:|
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

These measurements are exploratory and should not be interpreted as formal performance benchmarks.

The absolute differences between Command, Patch, and Hybrid are very small at this scale and can be affected by runtime and measurement noise.

The more relevant observation is the effect of ProjectState size.

The full-state Snapshot prototype showed a clear increase in execution cost when the state increased from 100 to 1000 ConfigItems.

Command, Patch, and Hybrid did not show comparable state-size-dependent growth for the same workload.

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

## State Size and History Storage

The current Snapshot implementation clones the complete ProjectState before every committed action.

For 100 Toggle operations:

```
100 ConfigItems
-> 10,000 stored ConfigItem copies

1000 ConfigItems
-> 100,000 stored ConfigItem copies
```

The Command, Patch, and current Hybrid implementations do not store a complete copy of ProjectState for each Toggle operation.

Their history data is instead primarily related to the state required to reverse the individual mutation.

## Current Findings

### Command

Command gives each history entry a clear domain meaning.

This works naturally for small semantic actions such as Toggle Active and Delete Config Item.

The main cost is implementation complexity: each new action may require its own command class and its own Execute, Undo, and Redo logic.

CompoundEditCommand demonstrated that this logic grows as one action modifies more properties or collection positions.

### Snapshot

Snapshot provides the most generic reversal mechanism in the current spike.

A mutation can change multiple properties and collection positions without requiring operation-specific inverse logic.

The main disadvantages observed in the current full-state prototype are:

- storage grows with ProjectState size and history depth
- execution cost grows as the cloned state becomes larger
- restoring cloned state does not preserve object reference identity
- history does not describe the semantic action that created the state

Snapshot scope may therefore be an important design decision.

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

For the currently tested local mutations, Hybrid avoids the need for a dedicated command class while retaining readable history entries.

However, the current Hybrid implementation still uses PatchTransaction for every tested action.

It has therefore not yet demonstrated whether different mutation patterns should use different history representations.

## Not Yet Evaluated

The following areas remain open:

- Draft-level Undo versus committed transaction Undo
- Cancel / Escape behavior relative to Undo history
- Native text-field Undo versus global Project Undo
- Side effects outside ProjectState
- Create operations
- Larger Move / Reorder scenarios
- Bulk Actions
- Import / Merge-like mutations
- Targeted Snapshot scope
- Mixed Patch / Snapshot Hybrid history
- Formal memory measurements
- Formal performance benchmarking

## Next Evaluation

The next experiment should use a broader mutation represented as a Bulk Action.

This can be used to determine whether a large number of Patch operations remains practical or whether a targeted Snapshot becomes a more natural history representation.

That experiment can then provide evidence for evaluating a mixed strategy such as:

```
Local / small mutation
-> Semantic Action + PatchTransaction

Broad / complex mutation
-> Semantic Action + Targeted Snapshot
```