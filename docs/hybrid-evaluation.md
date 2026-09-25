# Hybrid Undo/Redo Evaluation

The current Hybrid prototype combines semantic user-action metadata with reusable low-level Patch transactions.

Each history entry stores:

- a semantic action name
- a PatchTransaction containing the concrete state changes

Examples:

- Toggle Active
  -> Toggle Active [replace Active]

- Delete Config Item
  -> Delete Config Item [remove ConfigItem]

- Compound Edit
  -> Compound Edit [replace Name + replace Active + move ConfigItem]

## Architecture

React UI
-> ASP.NET Core API
-> HybridHistory
-> HybridHistoryEntry
-> PatchTransaction
-> IPatchOperation
-> ProjectState

The backend owns both the committed ProjectState and the Undo/Redo history.

HybridHistoryEntry adds semantic information around the same PatchTransaction implementation evaluated in the Patch prototype.

## History Representation

The main difference from the Patch prototype is that each history entry retains both the user-level action and the underlying mutations.

For example:

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

This makes the history readable at both levels:

- what the user did
- what state changes are required to undo or redo it

## Compound Edit

The Compound Edit changes:

- Name
- Active
- collection position

The Hybrid implementation represents these changes using three existing Patch operations:

```
replace Name
replace Active
move ConfigItem
```

These operations are grouped into one PatchTransaction and wrapped inside one semantic HybridHistoryEntry.

Therefore one user action still creates exactly one Undo history entry.

Unlike the Command prototype, no dedicated CompoundEditCommand with custom Execute, Undo, and Redo logic is required.

## Failure Handling

Hybrid reuses PatchTransaction for applying and reversing state changes.

The PatchTransaction implementation provides rollback when a multi-operation Apply or Undo fails part way through.

HybridHistory also keeps entries on their original history stack until the corresponding Undo or Redo operation succeeds.

Therefore a failed Undo or Redo does not remove the history entry before the operation has completed successfully.

This behavior was verified by unit tests.

## Performance Observation

Median execution time for 100 Toggle operations:

| Approach | 100 Items | 1000 Items |
| --- | ---: | ---: |
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

These values are exploratory measurements and should not be treated as formal performance benchmarks.

The absolute differences between Command, Patch, and Hybrid are very small and are likely affected by measurement noise at this scale.

The more relevant observation is the scaling behavior.

The Snapshot prototype became substantially more expensive when the ProjectState increased from 100 to 1000 ConfigItems.

Command, Patch, and Hybrid did not show comparable state-size-dependent growth for this workload.

Hybrid therefore currently has the same important scaling property as Patch: history storage and execution are primarily related to the changed state rather than the complete ProjectState size.

## Stored History Data

A Hybrid history entry stores:

- semantic action name
- PatchTransaction
- one or more Patch operations
- operation-specific before/after information

For example, Toggle Active stores information equivalent to:

```
"Toggle Active"
ConfigItem ID
previous Active value
resulting Active value
```

It does not clone the complete ProjectState.

This avoids the state-size-dependent snapshot storage observed in the Snapshot prototype.

## Object Identity and Ordering

Hybrid inherits these characteristics from the underlying Patch operations.

RemoveConfigItemPatch stores the removed ConfigItem reference and its original list index.

Undo can therefore restore:

- the same ConfigItem object
- the original collection position

MoveConfigItemPatch explicitly stores and restores collection positions.

Unlike the current full-state Snapshot implementation, Hybrid does not replace ConfigItem objects with cloned instances during normal Undo/Redo operations.

## Frontend / Backend Synchronization

The frontend continues to send user actions to the ASP.NET Core API.

The backend:

1. creates the semantic HybridHistoryEntry
2. performs the associated PatchTransaction
3. stores the entry in HybridHistory
4. returns the resulting ProjectState

The React frontend does not independently maintain committed Undo history.

This keeps the committed state and global Undo/Redo history in the same backend context.

## Initial Advantages

- preserves semantic user-action information
- exposes the concrete state mutations behind each action
- reuses generic Patch operations
- compound actions can reuse existing Patch primitives
- avoids full ProjectState snapshots for local mutations
- preserves the Patch prototype's transaction rollback behavior
- can preserve object identity and collection positions
- new semantic actions may be composed without requiring a dedicated Command class when the required Patch operations already exist

## Initial Disadvantages

- adds another abstraction layer around PatchTransaction
- still depends on Patch types for every supported kind of state mutation
- remains coupled to the structure of ProjectState
- semantic action names must be assigned explicitly by the caller
- history entries contain more metadata than the plain Patch approach
- operations outside ProjectState are still not represented
- the current prototype is primarily "semantic metadata + patches" rather than a Hybrid that dynamically selects different storage strategies

## Comparison with Command

Command represents the user action directly through a dedicated class such as:

```
CompoundEditCommand
```

This provides strong semantic meaning but requires the command itself to implement the necessary Execute, Undo, and Redo behavior.

Hybrid separates these concerns:

```
Semantic action
    +
reusable state-change operations
```

For compound actions that can be expressed using existing Patch operations, this reduces the need for new action-specific Undo/Redo implementations.

However, Command may still be useful when an operation contains domain behavior that cannot naturally be represented as generic state changes.

## Comparison with Patch

Patch already provides reusable state mutations and transaction composition.

Hybrid does not fundamentally change the reversal mechanism.

Instead, it adds the semantic context that the Patch history lacks.

For example:

```
Patch:

replace Name + replace Active + move ConfigItem

Hybrid:

Compound Edit [replace Name + replace Active + move ConfigItem]
```

The experiment therefore shows that semantic action information can be added without replacing the Patch-based Undo/Redo mechanism.

## Comparison with Snapshot

Hybrid avoids complete ProjectState cloning for the currently tested local actions.

This avoids the state-size-dependent execution and history storage behavior observed with the full-state Snapshot prototype.

However, Snapshot still has an important property that Hybrid has not replaced: a complex state transition can be captured without constructing a large collection of explicit inverse operations.

This remains relevant for broad mutations such as imports or bulk operations.

## Open Questions

- At what point does composing many Patch operations become more complex than storing a targeted Snapshot?

- Should semantic action names be simple strings or structured metadata?

- Should some actions use Patch transactions while larger actions use targeted Snapshots?

- How should side effects outside ProjectState participate in the same transaction?

- How should draft UI state interact with the committed Hybrid history?

- How should native text-field Undo coexist with global Project Undo?

## Current Finding

The current Hybrid experiment demonstrates that semantic user-action information can be combined with reusable Patch transactions.

For the tested local and compound mutations, this preserves the main advantages of Patch while making the history more meaningful from the user's perspective.

However, the current Hybrid implementation does not yet demonstrate that different mutation patterns should use different history representations.

At this stage it is more accurately described as:

```
Semantic Action + PatchTransaction
```

rather than:

```
Patch or Snapshot selected by mutation type
```

A broader action should therefore be evaluated separately before deciding whether a mixed Patch/Snapshot Hybrid provides additional value.