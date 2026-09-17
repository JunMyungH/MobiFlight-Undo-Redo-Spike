# MobiFlight-Undo-Redo-Spike
Repository to test Undo/Redo function using C# and .Net Framework based prototype

## Goal
Compare Undo/Redo mechanisms for MobiFlight.

### Approaches
- Command-based
- Snapshot-based
- Hybrid

and additionally

- Delta / Patch
- Event Sourcing
- Persistent Immutable State
- Transaction Log
- Operational Transformation / CRDT

### Representative actions
- Toggle Active
- Delete Config Item
- later: Duplicate, Reorder

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
