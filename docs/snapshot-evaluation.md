# Snapshot-based Undo/Redo Evaluation

The Snapshot prototype stores complete copies of the current ProjectState instead of storing action-specific commands or low-level patch operations.

For every committed mutation, the current implementation stores the ProjectState that existed before the mutation.

Undo and Redo then restore previously captured states.

## Implemented Actions

- Toggle Active
- Delete Config Item
- Compound Edit
- Undo
- Redo

## Architecture

```
React UI
-> ASP.NET Core API
-> SnapshotHistory
-> ProjectState Clone / Restore
-> ProjectState
```

The backend owns both the committed ProjectState and the Undo/Redo history.

The frontend sends actions to the backend and receives the resulting state after each operation.

## History Representation

Each Undo history entry is a cloned ProjectState.

For example, after a Compound Edit with three ConfigItems, the history inspector displays:

```
ProjectState snapshot (3 ConfigItems)
```

The snapshot does not describe which properties or objects were changed.

It only represents the complete previous state of the configured snapshot scope.

## Toggle Active

For Toggle Active, SnapshotHistory performs the following steps:

1. clone the complete ProjectState
2. change the Active property
3. store the previous snapshot in the Undo stack

Undo restores the previous snapshot.

Redo stores the current state and restores the state from the Redo stack.

No Toggle-specific inverse operation is required.

## Delete Config Item

Delete also requires no Delete-specific Undo implementation.

Before the ConfigItem is removed, the complete ProjectState is cloned.

Undo restores the previous snapshot, including:

- the deleted ConfigItem
- its logical ID
- its original collection position

The existing unit tests verify that the deleted item is restored at the correct list index.

## Object Identity

The current Snapshot implementation restores state by cloning the ConfigItems contained in the snapshot:

```
target.ConfigItems = snapshot.ConfigItems
    .Select(item => item.Clone())
    .ToList();
```

Therefore logical identity such as ConfigItem ID is preserved.

However, object reference identity is not preserved.

After Undo or Redo, restored ConfigItems are new object instances rather than the same references that existed before the mutation.

This is different from the current Command and Patch implementations, which can retain and restore the original object reference.

## Ordering Restoration

Collection ordering is naturally captured by the snapshot.

For example, if a ConfigItem is deleted from the middle of the list, Undo restores the complete previous collection and therefore restores the original ordering without requiring the operation to explicitly store an index.

This is one of the main simplifications of the Snapshot approach.

## Compound Edit

The Compound Edit changes:

- Name
- Active
- collection position

The Snapshot implementation does not require separate reversal logic for any of these mutations.

Before the compound mutation is executed, one ProjectState snapshot is stored.

Undo restores the complete previous state.

Therefore one compound user action produces one Undo history entry:

```
ProjectState snapshot (3 ConfigItems)
```

The complexity of the Undo mechanism does not increase because the mutation changes several properties or collection positions.

This contrasts with Command and Patch, where the individual state changes must be represented explicitly.

## History Behavior

The Snapshot prototype supports the same general history behavior as the other implementations:

- Undo follows LIFO ordering
- Undo moves the previous state into the active ProjectState
- the state before Undo is sotred in the Redo stack
- Redo restores the next snapshot
- executing a new mutation after Undo clears the Redo stack

These behaviors are covered by the SnapshotHistory unit tests.

## Performance Observation

Median execution time for 100 Toggle operations:

| Approach | 100 Items | 1000 Items |
|---|---|---|
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |
| Hybrid | 0.070 ms | 0.042 ms |

These measurements are exploratory and should not be treated as formal performance benchmarks.

The important observation for Snapshot is the scaling behavior.

When ProjectState increased from 100 to 1000 ConfigItems, the median execution time for the same 100 Toggle workload increased from:

```
0.902 ms
to:
6.995 ms
```

This behavior is consistent with the current implementation cloning the complete ProjectState for every committed mutation.

Command, Patch, and Hybrid did not show comparable state-size-dependent growth in the same experiment.

## History Storage Observation

The current implementation exposes the number of ConfigItem copies stored in Snapshot history.

For 100 Toggle operations:

```
100 ConfigItems
x 100 history entries
= 10,000 stored ConfigItem copies
```

For 1000 ConfigItems:

```
1000 ConfigItems
x 100 history entries
= 100,000 stored ConfigItem copies
```

This value is a simple proxy for snapshot storage and is not a measurement of actual memory usage in bytes.

The experiment nevertheless demonstrates that full-state history storage grows approximately with:

```
snapshot scope size
x
history depth
```

## Frontend / Backend Synchronization

The React frontend does not maintain its own committed Undo history.

A frontend action is sent to the backend API.

The backend:

1. creates the snapshot
2. performs the mutation
3. stores the previous state in SnapshotHistory
4. returns the resulting ProjectState

This keeps the committed state and global Undo/Redo history in the same backend context.

## Initial Advantages

- generic Undo and Redo mechanism
- no action-specific inverse logic required
- compound mutations are naturally captured
- collection ordering is restored automatically
- adding a new mutation normally does not require a new history-entry type
- simple mental model: restore a previous state

## Initial Disadvantages

- current implementation clones the complete ProjectState for every committed mutation
- execution cost increases with snapshot scope size
- history storage increases with snapshot scope size and history depth
- object reference identity is not preserved by the current Clone / Restore implementation
- history entries do note retain semantic user-action information
- even very small mutations currently pay the cost of cloning the full state

## Comparison with Command

Command stores only the information required to reverse a specific semantic action.

Snapshot instead stores the previous state without knowing what kind of action caused the change.

For a simple Toggle operation, Command requires very little history data, while Snapshot still clones the complete ProjectState.

For a complex compound mutation, Snapshot remains structurally simple, while Command requires more action-specific state and inverse logic.

## Comparison with Patch

Patch records the concrete state changes required to reverse an action.

Snapshot records the complete previous state instead.

Patch therefore avoids copying unrelated state for small local mutations.

Snapshot, however, does not require a mutation to be decomposed into individual replace, remove, or move operations.

This may become relevant when one action modifies a large or difficult to predict portion of the state.

## Comparison with Hybrid

The current Hybrid prototype uses semantic metadata together with PatchTransaction.

For the currently tested local mutations, Hybrid avoids full-state cloning and retains semantic history information.

Snapshot does not provide either of these properties in the current prototype.

However, Snapshot can represent broad or complex state transitions without requiring a large set of explicit Patch operations.

This makes snapshot scope an important candidate for later Hybrid experiments.

## Open Questions

- What is the appropriate snapshot scope for MobiFlight?

Possible scopes include:

```
ConfigItem
Profile
Project subset
complete ProjectState
```

- Can targeted snapshots avoid the scaling behavior of the current full-state implementation?

- When does a targeted snapshot become simpler than a larg collection of Patch operations?

- Does restoring cloned objects cause problems for other parts of the MobiFlight object model that rely on object references?

- How should external side effects be restored?

- How should draft UI state be separated from committed snapshots?

- Should semantic metadata be stored alongside snapshots?

## Current Finding

The Snapshot prototype provides the most generic state-reversal mechanism evaluated so far.

Its Undo/Redo implementation remains simple even when one action modifies multiple properties and collection positions.

The cost of this simplicity in the current prototype is that every action stores a complete ProjectState snapshot.

The experiments show clear state-size-dependent execution and storage growth for this full-state approach.

Therefore the current results do not suggest that complete ProjectState snapshots should automatically be used for every action.

However, they also do not rule out snapshots as a useful mechanism.

A narrower targeted snapshot may be appropriate for actions that modify a broad or complex section of the state and would otherwise require many individual Patch operations.

This should be evaluated with a broader mutation such as a Bulk Action.