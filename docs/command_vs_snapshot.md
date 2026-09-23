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

## Compound Edit Evaluation

A compound edit was added to evaluate how each approach handles one semantic user action that changes multiple parts of the project state.

The test action performs the following changes on one ConfigItem:

- changes the Name
- toggles the Active state
- moves the ConfigItem to another collection position

Although several state changes occur internally, the complete Apply operation must be represented as one Undo/Redo history entry.

### Command Based

The Command implementation uses a dedicated:

`CompoundEditCommand`

The command explicitly stores the information required to reverse and replay the complete operation:

- previous Name
- resulting Name
- previous Active value
- resulting Active value
- previous collection index
- resulting collection index
- target item identity

The History Inspector therefore contains one semantic entry:

`CompoundEditCommand`

Undo explicitly restores each stored property and the previous collection position.

Redo explicitly reapplies the resulting property values and position.

This keeps the history compact and semantically meaningful, but the amount of action-specific inverse logic increases as the operation becomes more complex.

### Snapshot Based

The Snapshot implementation performs the same compound mutation inside one SnapshotHistory operation.

Before the mutation, the complete ProjectState is cloned.

The mutation itself can then change multiple values without requiring any action-specific inverse implementation:

- Name
- Active
- collection position

The History Inspector contains one entry:

`ProjectState snapshot (N ConfigItems)`

Undo restores the previous ProjectState generically.

Redo restores the resulting ProjectState generically.

The Undo/Redo mechanism itself does not become more complex when more properties are included in the compound operation.

However, the complete configured snapshot scope is stored even though only one ConfigItem is modified.

### Comparison

| Criterion | Command | Snapshot |
| --- | --- | --- |
| History entries per compound Apply | 1 | 1 |
| Semantic history | `CompoundEditCommand` | Generic ProjectState snapshot |
| Property-specific inverse logic | Required | Not required |
| Collection position handling | Explicitly stored | Included in snapshot |
| Complexity when more fields change | Command implementation grows | Undo mechanism unchanged |
| Stored state | Data required by the action | Entire snapshot scope |
| Object reference restoration | Can preserve original object | Current clone implementation creates new objects |
| Dependence on project size | Low for this command | Snapshot size grows with project size |

### Finding

The compound-edit experiment exposes a trade-off that was not visible as clearly in the simple Toggle experiment.

For small localized changes, the Command approach can store only the minimal information required to reverse the operation.

For compound changes, however, the Command implementation must explicitly model every part of the inverse operation.

The Snapshot approach has a higher state-copying cost, but the Undo mechanism remains unchanged regardless of how many properties or collection changes are included in the transaction.

This suggests that the suitability of an Undo/Redo representation may depend on the mutation pattern rather than only on whether the approach can technically support Undo and Redo.