# Undo/Redo Spike Plan

## Goal

Evaluate possible Undo/Redo architectures before integrating an Undo/Redo mechanism into MobiFlight Connector.

The spike is based on the interaction and mutation patterns identified in the GUI-based Action Catalog.

The spike does **not** define the final MVP scope.

Its purpose is to determine how different Undo/Redo approaches handle representative MobiFlight state changes and interaction boundaries.

## Representative Action Patterns

The GUI Action Catalog identified several recurring mutation patterns:

```
Update
Create
Delete
Move / Reorder
Compound Edit
Import / Merge
```

For the initial prototype implementations, use two small representative cases:

```
Toggle Active
→ Update

Delete Config Item
→ Delete
```

These cases are intentionally small so that the architectural differences are easy to compare.

The remaining patterns should be considered during evaluation even if they are not fully implemented in every prototype.

Examples:

```
Duplicate / Add Config Item
→ Create

Drag and Drop
→ Move / Reorder

Input Config → Apply Changes
→ Compound Edit

Merge Profiles
→ Import / Compound
```

The legacy Output Config WinForms flow is intentionally excluded from the current spike and should be reconsidered after the new Output Config UI is available.

## Phase 1 - Command Based

Implement:

- Toggle Active
- Delete Config Item

Represent each committed user action as a semantic reversible command.

Verify:

- Undo restores the exact previous state
- Redo restores the exact resulting state
- Multiple actions follow LIFO ordering
- A new action after Undo clears the Redo stack
- Required object identity and collection position can be restored
- One semantic user action produces one history entry

Evaluate how additional catalog patterns would map to commands:

- Create
- Move / Reorder
- Compound Edit
- Import / Merge

## Phase 2 - Snapshot Based

Implement the same representative actions using before/after state snapshots.

Compare the implementation with the command-based approach.

Verify:

- exact previous state can be restored
- exact resulting state can be restored
- object identity and collection ordering are preserved where required
- snapshots can represent compound state changes without requiring an explicit inverse operation

Evaluate the appropriate snapshot scope:

```
Property
Config Item
Profile
Project subset
Complete Project
```

## Phase 3 - Patch Based

Represent state changes as generic operations such as:

- replace
- add
- remove
- move

Map the operations to Action Catalog patterns.

Examples:

```
Toggle Active
→ replace

Delete Config Item
→ remove

Duplicate / Add Config Item
→ add

Drag and Drop
→ move
```

Evaluate whether patches can also represent compound actions such as:

```
Input Config Apply Changes
Merge Profiles
```

without becoming difficult to understand or maintain.

## Phase 4 - Hybrid

Use the findings from the previous approaches to evaluate combinations such as:

```
Semantic command + targeted snapshot

Semantic command + generic patch

Transaction-level snapshot + semantic action metadata
```

Evaluate whether different mutation patterns benefit from different representations rather than forcing every action into one model.

## Interaction Boundary Evaluation

The GUI Action Catalog identified that not every visible interaction is an immediately committed Project mutation.

Examples include:

```
Inline Rename
→ typing
→ Enter / Blur

Input Config
→ multiple draft edits
→ Apply Changes

Drag and Drop
→ temporary drag state
→ successful Drop
```

The spike should evaluate where Undo history entries should be created.

In particular, compare two possible models for form-based editing.

### Draft-Level Undo

```
Open editor
→ change A
→ change B
→ Ctrl+Z
→ return to state after change A
```

### Transaction-Level Undo

```
Project state A
→ open editor
→ perform several draft changes
→ Apply
→ Project state B
→ Ctrl+Z
→ restore Project state A
```

Cancellation must also be distinguished from Undo:

```
Cancel / Escape
→ discard unfinished interaction

Undo
→ reverse a state transition represented in history
```

The appropriate behavior should be determined from the spike findings rather than assumed in advance.

## Local and Global Undo Context

The Action Catalog also identified interactions that may have their own local Undo behavior.

Examples:

```
Search fields
Inline text editors
Form text fields
Filter searches
```

The spike should consider how a global Project Undo mechanism can coexist with native text editing.

For example:

```
Focused text input + Ctrl+Z
→ local/native text Undo

Committed Project action + Ctrl+Z
→ Project history Undo
```

This does not require implementing all UI shortcut routing in the spike, but the architecture should make the distinction possible.

## Evaluation

For every approach document:

- stored history data
- history entry granularity
- Undo complexity
- Redo complexity
- object identity handling
- collection index / ordering restoration
- compound-action handling
- draft vs committed-state handling
- cancellation behavior
- side-effect handling
- frontend/backend synchronization implications
- memory implications
- implementation effort
- extensibility to other Action Catalog patterns

Also record any cases where an approach works well for one mutation pattern but becomes difficult for another.

## Spike Output

At the end of the spike, summarize:

- strengths and weaknesses of each approach
- which Action Catalog patterns each approach represents naturally
- problems discovered during implementation
- unresolved questions
- implications for MobiFlight's frontend/backend architecture

The spike findings will then provide input for the later MVP scope prioritization.