# MobiFlight-Undo-Redo-Spike
Repository for prototyping and comparing Undo/Redo approaches using C# and .NET.

## Goal
Compare Undo/Redo mechanisms for MobiFlight.

### Approaches
- Command-based
- Snapshot-based
- Patch-based
- Hybrid

and additionally

- Event Sourcing
- Persistent Immutable State
- Transaction Log

These may inform the comparison but are not separate implementation phases.

### Initial representative actions
- Toggle Active - Update
- Delete Config Item - Delete

Other Action Catalog patterns such as Create, Move/Reorder, Compound Edit, and Import/Merge are considered during evaluation.

## Structure

`src/UndoRedoSpike`
- Simplified domain model and Undo/Redo prototypes

`tests/UndoRedoSpike.Tests`
- MSTest tests for each approach

`docs/spike-plan.md`
- Scope and evaluation plan

`docs/evaluation.md`
- Comparison results

### Questions
- Where should history live?
- What state must each entry store?
- How are frontend/backend states synchronized?
- How are side effects reproduced?
- How difficult is Redo?
- How easy is the approach to test and extend?

### Evaluation
- Architecture fit
- Implementation complexity
- Maintainability
- Testability
- Memory/state cost
- MVP feasibility
