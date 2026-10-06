# MobiFlight-Undo-Redo-Spike
Repository for prototyping and comparing Undo/Redo approaches.

This prototype should be a full-stack prototype, using React + Typescript and .NET, which would be used for evaluating Undo/Redo architectures for MobiFlight Connector.

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
- Simplified domain model and Undo/Redo implementations
- Command, Snapshot, Patch, and Hybrid approaches

`src/UndoRedoSpike.Api`
- .NET API exposing the spike operations to the frontend
- Undo, Redo, bulk experiments, and history navigation endpoints

`frontend`
- React + TypeScript frontend
- Interactive comparison of all four approaches
- Keyboard Undo/Redo shortcuts
- History Inspector with multi-step history navigation

`tests/UndoRedoSpike.Tests`
- MSTest coverage for all approaches
- State restoration, failure rollback, compound actions, History Jump, nested History Jump, and Hybrid Patch/Snapshot mixing

`docs/spike-plan.md`
- Scope and evaluation plan

`docs/evaluation.md`
- Comparison results and current architectural recommendation

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

### Current Result

The architecture comparison currently favors a Hybrid approach:

- Patch-backed history entries for localized and identity-sensitive changes
- Snapshot-backed entries for selected broad or structurally complex changes
- One semantic history entry per committed user action
- Persistent IDs rather than direct object references for Patch target resolution
- History navigation implemented independently from the underlying history representation