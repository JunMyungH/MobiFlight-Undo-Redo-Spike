## Patch-Based Evaluation

The Patch prototype represents state changes using reusable,
low-level operations rather than action-specific commands or
complete state snapshots.

Examples:

- Toggle Active -> replace Active
- Delete Config Item -> remove ConfigItem
- Compound Edit -> replace Name + replace Active + move ConfigItem

### Performance Observation

Median execution time for 100 Toggle operations:

| Approach | 100 Items | 1000 Items |
| --- | ---: | ---: |
| Command | 0.006 ms | 0.006 ms |
| Snapshot | 0.902 ms | 6.995 ms |
| Patch | 0.038 ms | 0.038 ms |

These values are exploratory measurements and not formal
performance benchmarks.

The important observation is the scaling behavior.

The full-state Snapshot implementation became substantially more
expensive as project size increased.

The Command and Patch prototypes did not show the same
state-size-dependent growth for this workload.

### Compound Edit

All three approaches represented the compound Apply operation as
one history entry.

Command:

    CompoundEditCommand

Snapshot:

    ProjectState snapshot (3 ConfigItems)

Patch:

    replace Name + replace Active + move ConfigItem

The Command representation preserves the semantic user action, but
requires action-specific inverse state and Undo/Redo implementation.

The Snapshot representation requires no operation-specific inverse
logic, but stores the complete snapshot scope and does not retain
the semantic meaning of the user action.

The Patch representation reuses generic operations and can compose
multiple state mutations into one transaction without requiring a
dedicated CompoundEdit command.

### Patch Trade-offs

Advantages:

- avoids full-state snapshots
- reusable primitive operations
- compound actions can be composed from existing operations
- history exposes concrete state changes
- current implementation can preserve object identity

Disadvantages / Open Questions:

- every new kind of state mutation may require another patch type
- patches are coupled to the shape of the state model
- semantic user intent is less explicit than with Commands
- transaction rollback is required if one patch in a compound
  transaction fails
- side effects outside ProjectState are not yet represented