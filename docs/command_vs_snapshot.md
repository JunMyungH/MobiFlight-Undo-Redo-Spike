# Command vs Snapshot Evaluation

## Functional Behavior

Both approaches successfully support the currently implemented representative actions:

- Toggle Active
- Delete Config Item
- Undo
- Redo
- multiple sequential history entries

Both approaches can restore the observable project state for the tested actions.

## History Representation

### Command Based

History contains semantic operations such as:

- ToggleActiveCommand
- DeleteConfigItemCommand

Undo information is stored specifically for each action.

For example:

ToggleActiveCommand:
- target object
- previous Active value
- resulting Active value

DeleteConfigItemCommand:
- deleted item
- item identity
- original collection index

This makes the history semantically understandable but requires action-specific Undo/Redo logic.

### Snapshot Based

History contains complete ProjectState snapshots.

No action-specific inverse operation is necessary. The previous project state is restored generically.

However, the history does not directly describe which semantic user action caused the state transition.

## Storage Scaling

With 100 ConfigItems and 100 Toggle operations:

- Command: 100 command entries
- Snapshot: 100 snapshots
- Snapshot stored ConfigItem copies: 10,000

With 1,000 ConfigItems and 100 Toggle operations:

- Command: 100 command entries
- Snapshot: 100 snapshots
- Snapshot stored ConfigItem copies: 100,000

The current full-state snapshot implementation therefore scales approximately with:

Project State Size × History Depth

even when an individual operation changes only one property.

Command history storage is instead determined mainly by the data required to reverse each individual operation.

## Exploratory Execution-Time Observation

100 ConfigItems / 100 Toggle operations:

- Command: 0.146 ms
- Snapshot: 1.203 ms

1,000 ConfigItems / 100 Toggle operations:

- Command: 0.006 ms
- Snapshot: 9.234 ms

These are exploratory Stopwatch measurements and must not be treated as formal performance benchmarks.

The Command measurements are too short and variable for meaningful numerical comparison.

The Snapshot measurements nevertheless show a clear increase with project size, consistent with full ProjectState cloning.

## Object Identity

The current Command Delete implementation restores the previously removed ConfigItem instance.

The current Snapshot implementation reconstructs ConfigItems using Clone().

Therefore:

- logical identity (Guid): preserved
- object reference identity: not preserved by Snapshot restore

This may be relevant if other MobiFlight components keep references to project objects.

## Collection Ordering

Command Delete requires the original collection index to be stored explicitly.

Snapshot restore preserves collection order as part of the stored state and does not require action-specific index handling.

## Preliminary Trade-offs

Command Based:
- compact for small/local mutations
- semantic history
- explicit control over Undo/Redo
- requires action-specific inverse logic
- complexity may grow for compound operations

Snapshot Based:
- generic Undo/Redo implementation
- simple support for complex multi-property state changes
- automatically restores collection contents/order
- storage cost grows with snapshot scope and history depth
- cloning cost grows with state size
- current implementation loses object reference identity

## Open Questions

The current evaluation is not sufficient to select a final architecture.

Further evaluation is required for:

- Create
- Move / Reorder
- Compound Edit
- Import / Merge
- draft vs committed state
- side effects
- frontend/backend synchronization
- appropriate snapshot scope