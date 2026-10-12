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

The spike has implemented and evaluated Command, Snapshot, Patch, and Hybrid Undo/Redo approaches.

Although the initial evaluation favored Hybrid with Patch-backed history entries, subsequent architecture review and production-oriented experiments led to a reassessment.

For the current scope of Undo/Redo within an individual ConfigItem editor, bounded Item **Snapshot history is the preferred candidate for further integration.**

The prototype demonstrates:

- Semantic Snapshot history.
- Local Undo/Redo for temporary ConfigItem drafts.
- Apply/Cancel boundaries.
- Configurable experimental history limits (10, 25, and 50 steps).
- Automated local history tests.
- Performance and memory measurements using actual MobiFlight ConfigItems.

Command remains an alternative, particularly for committed actions where compact history representation or object identity preservation is important.

The initial production scope, final history limit, and implementation issue breakdown remain subject to review.

**See docs/evaluation.md** for detailed findings and remaining integration questions.