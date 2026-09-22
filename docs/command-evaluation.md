# Command-based Undo/Redo Evaluation

## Implemented actions

- Toggle Active
- Delete Config Item
- Undo
- Redo

## Architecture

React UI
→ ASP.NET Core API
→ CommandHistory
→ IUndoableCommand
→ ProjectState

The backend owns the committed project state and command history.

## Observations

### Toggle Active

Stored information:

- Target ConfigItem
- Previous Active value
- New Active value

Undo and Redo are straightforward.

### Delete Config Item

Stored information:

- Project reference
- ConfigItem ID
- Deleted ConfigItem
- Original list index

Undo requires restoring both the deleted object and its original position.

### History behavior

- Commands are undone in LIFO order.
- Undo moves commands to the redo stack.
- Redo moves them back to the undo stack.
- Executing a new command after Undo clears the redo stack.

### Frontend / Backend synchronization

The React frontend does not modify the committed project state directly.

Actions are sent to the backend API and the backend returns the resulting state.

This keeps the Undo/Redo history and committed state in one place.

## Initial advantages

- Small amount of history data for simple actions.
- Explicit semantic representation of user actions.
- Undo/Redo behavior is easy to understand for simple mutations.

## Initial disadvantages

- Every action requires specific Execute/Undo/Redo logic.
- Delete-like operations need additional contextual information.
- Compound actions may require significantly more implementation logic.

## Questions for later comparison

- How complex does Command become for compound edits?
- How should draft-level Undo interact with committed Command history?
- How much memory does Command use compared with Snapshot?
- How should side effects be handled?