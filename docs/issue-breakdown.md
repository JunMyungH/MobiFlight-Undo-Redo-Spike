# Undo/Redo Issue Breakdown

The spike suggests bounded Item Snapshot history for **unsaved editor drafts** as a possible first increment. Undo/Redo for **committed ConfigItem list actions**, especially accidental Delete, is distinct and is explicitly requested by the original issue.

## Proposed issues

### 01-config-item-undo (MVP)

**Parent:** #2886  
**Delivery:** MVP  
**Depends on:** Minimal shared behavior from 03  
**Extension:** 06-config-item-redo

  - Goal
    - Provide Undo/Redo for meaningful editing actions within **one ConfigItem editor**, before applying changes to the project. Keep editing history local and temporary.

  - Proposed scope
    - Work on a separate draft of the selected ConfigItem; do not mutate committed project data while editing.
    - Support semantic Undo for changes to names, active state, and nested input/output configuration settings.
    - Evaluate bounded Item Snapshot history; test 10, 25, and 50 undoable steps. Final default is a product decision.
    - `Cancel`/`Escape` discard draft and history; `Apply` submits final draft and disposes local history after successful completion.
    - Route Ctrl+Z without overriding native Undo in focused text inputs.

  - Acceptance criteria
    - Undo restores all edited persisted settings, including nested objects, in the draft.
    - Original ConfigItem stays unchanged until successful Apply.
    - Cancel makes no committed change and re-opening starts with an empty local history.
    - Failed Apply retains the draft so editing can continue.
    - History overflow does not produce invalid Undo states.
    - Automated tests cover history branching, limits, Apply/Cancel, and complex nested configuration data.

  - Outside scope
    - Undo after the editor has been closed.
    - Delete/duplicate/reorder performed in the main ConfigItem list.
    - Profile, project, or global settings history.

### 02-profile-undo (MVP)

**Parent:** #2886  
**Delivery:** MVP  
**Depends on:** Minimal shared behavior from 03  
**Extension:** 07-profile-redo

- Goal

  - Undo accidental changes to **committed ConfigItems within profiles** from the main configuration list. In particular, restore a ConfigItem deleted unintentionally.
  - **Terminology:** Here "Profile Undo" means Undo of ConfigItem list operations in a profile. It does **not** mean unsaved wizard edits or changes to the profile tab's own metadata.

- Proposed scope

  - Establish one history entry per successfully committed semantic user action.
  - Candidate list operations: Add, Delete, Duplicate, Toggle Active, Reorder; determine MVP delivery sequence with Sebastian.
  - Support Input and Output ConfigItems in the shared React list even though Output editor itself remains WinForms.
  - Preserve original profile, index/order, persistent GUID and full relevant configuration data as appropriate.
  - Treat multi-selection as one user action where applicable (e.g. bulk delete, bulk toggle).
  - Assess and explicitly decide the behavior of moves between profiles/files, including both source and destination state.
  - Maintain the authoritative .NET `Project`/`ExecutionManager` state; propagate consistent frontend updates after Undo.
  - Reuse equivalent cache invalidation, configuration-changed events and notifications employed by the normal mutation path.
  - The first vertical slice should demonstrate end-to-end **single Delete Undo** before expanding to other actions.

- Acceptance criteria

  - Single Delete Undo restores the **entire** ConfigItem, its GUID and original list position in the correct profile.
  - The corresponding frontend list reflects the restored state without a manual refresh.
  - Add Undo removes only the item that was added, without damaging other items.
  - Duplicate Undo removes only the newly created duplicate while retaining the original item.
  - Toggle Undo restores the exact prior Active state of every affected item; do not simply invert an assumed common value.
  - Reorder Undo restores the prior order, including source and destination profiles if cross-profile moves are supported.
  - A bulk action creates one Undo step and restores all affected items atomically.
  - No-op, invalid or failed mutations do not leave invalid or partial history entries.
  - Relevant `OnInputConfigSettingsChanged`, config update messages and change notifications remain correct after reversing a mutation.
  - Tests cover identity, ordering, nested configuration values, failure behavior, cache invalidation and frontend/backend synchronization.

- Technical investigation

  - `ExecutionManager` currently handles `CommandAddConfigItem`, `CommandConfigContextMenu`, `CommandConfigBulkAction`, `CommandResortConfigItem` and `HandleCommandUpdateConfigItem`.
  - Deletion calls `OnInputConfigSettingsChanged`; item updates publish `ConfigValuePartialUpdate` and change events.
  - List history should capture *before* values and structural positions at the .NET commit boundary. A semantic action could use a targeted snapshot or a purpose-built reversible operation; the editor's preferred Item Snapshot does not automatically decide the list architecture.
  - Do not assume replacing an `IConfigItem` object is harmless to caches that retain object references.
  - Cross-profile operations may need a compound transaction affecting two `ConfigFiles`.

- Outside scope

  - Redo (issue 07).
  - Local unsaved editor draft history (issue 01).
  - Project name/simulator type and profile name/order metadata (provisionally issue 04).
  - Global settings Undo (issue 05).

### 03-shared-history-policy (MVP)

**Parent:** #2886  
**Delivery:** MVP-enabling infrastructure, only as needed by 01/02  
**Extensions:** Supports later issues without requiring their implementation now

- Goal

  - Define a small, testable set of common Undo semantics and capacity/ownership rules so editor-local and committed-profile history do not behave inconsistently.

- Proposed scope

  - Define **one semantic user action = one Undo step** and no entry for no-op/failed actions.
  - Distinguish history ownership and lifetime: editor-local Draft history versus authoritative .NET committed-history for ConfigItem list changes.
  - Define maximum **undoable steps** (not number of snapshots). The experimental values 10/25/50 are not final product defaults.
  - Bound memory use by removing oldest reversible entries without breaking the remaining valid Undo path.
  - Define `CanUndo` or equivalent UX state, action descriptions/labels where useful, and rules for disposing/resetting history.
  - Ensure history entries become visible only after a successful committed action (or successful local draft edit).
  - Document shortcut focus routing: native text Ctrl+Z > relevant local editor Undo > committed Profile Undo; do not route shortcut to an unrelated context.
  - Permit future Redo extension, but **do not require Redo stacks, Redo UI or Redo tests for MVP delivery**.
  - Prefer small reusable interfaces/contracts over an oversized universal history service.

- Acceptance criteria

  - The scope and lifetime of editor history and committed history are documented and independently testable.
  - History caps never cause invalid Undo targets; the current state is always retained.
  - No-op and failed mutations do not consume a history step.
  - Undo cannot mutate stored immutable snapshots/history entries.
  - Closing an editor clears its local history without clearing committed Profile Undo history.
  - Tests verify history-cap overflow, no-op behavior, session reset and failure boundaries.
  - Undo action availability is correctly disabled/ignored when no prior valid step exists.

- Technical investigation

  - Decide whether a shared conceptual policy is enough or whether a small shared TypeScript/.NET contract is useful. The stores cannot literally share C# objects with React.
  - Clarify semantics: a history with N undoable actions may require N+1 stored snapshot states in a cursor-based design.
  - For a backend authoritative history, define how UI learns `CanUndo` and receives updated configuration data.
  - Avoid enforcing identical memory budgets for one-item editor drafts and multi-item profile transactions.

- Outside scope

  - Implementing the actual mutations of 01/02 (owned by those issues).
  - Redo delivery (06–09).
  - Persisting history across application restarts.
  - Project-wide/global history implementation (04/05).

### 04-project-undo

**Parent:** #2886  
**Delivery:** Post-MVP Undo feature  
**Extension:** 08-project-redo

- Goal

  - Undo committed changes to the project's own settings and structure, beyond ConfigItem list actions covered by issue 02.

- Proposed scope

  - Project metadata changes such as project name and simulator type.
  - **Provisional allocation:** profile tab metadata and structure, including profile rename and profile order, because #2886 explicitly mentions "profile settings, like name and order". Confirm with Sebastian whether these belong here or merit their own sub-issue.
  - Record each committed user operation as one semantic Undo step; distinguish it from draft/editor-only activity.
  - Restore metadata and any dependent project state consistently, including necessary UI notifications.
  - Investigate effects of changing simulator type, selected/active profile and profile structure before promising exact reversal.

- Acceptance criteria

  - Undo restores supported project settings exactly without changing unrelated ConfigItems.
  - If included, profile rename/order Undo restores the correct profile identities and ordering.
  - No-op or failed mutations produce no valid history entry.
  - Relevant project/UI change notifications remain consistent after Undo.
  - Tests cover project metadata, selected profile behavior and edge cases around profile order.

- Outside scope

  - Redo (issue 08).
  - ConfigItem list Add/Delete/Duplicate/Toggle/Reorder (issue 02).
  - Unsaved ConfigItem editor history (issue 01).
  - Global settings (issue 05).

- Review questions

  - Do profile name/order belong to this Project Undo issue, or should they become a distinct issue?
  - Which project settings have side effects or migration implications that make Undo unsafe?
  - Should Project Undo share history with committed Profile Undo, or be separate but coordinated?

### 05-global-settings-undo

**Parent:** #2886  
**Delivery:** Post-MVP Undo feature  
**Extension:** 09-global-settings-redo

- Goal

  - Support Undo of selected committed **application-wide settings** changes, independently from project and ConfigItem editing history.

- Proposed scope

  - Inventory global settings and classify which are safely reversible.
  - Start with a simple reversible setting such as log level (explicitly mentioned in #2886).
  - Capture the previous value before changing the setting, and record one semantic Undo entry after a successful change.
  - Apply undo through normal settings pathways, including any necessary observer notifications and persistence updates.
  - Define behavior across application restart and external settings changes before extending scope.

- Acceptance criteria

  - Undo restores the previous value for each explicitly supported global setting.
  - The UI and persistent settings stay synchronized after Undo.
  - No-op and failed settings changes produce no history entry.
  - Tests cover supported settings and interactions with application-level notifications.

- Outside scope

  - Redo (issue 09).
  - Undo of all settings automatically; unsupported/irreversible side effects require explicit exclusions.
  - Project metadata and ConfigItem operations (issues 01, 02 and 04).

### 06-config-item-redo

**Parent:** #2886  
**Delivery:** Extension (not MVP)  
**Depends on:** 01-config-item-undo and relevant policy in 03

- Goal

  - Allow users to reapply previously undone **editor-local** ConfigItem draft changes while the React editor remains open.

- Proposed scope

  - Redo previously undone semantic local draft steps, in correct order.
  - New meaningful edit after Undo invalidates the incompatible future Redo branch.
  - Support Ctrl+Y and/or Ctrl+Shift+Z without overriding native redo behavior in a focused text input.
  - Expose a correct `CanRedo` state and optional Redo control.
  - Keep local Redo separate from committed Profile history.

- Acceptance criteria

  - Undo followed by Redo recreates the exact relevant draft configuration, including nested settings.
  - A new edit after Undo clears previously available Redo steps.
  - No-op changes do not incorrectly clear history branches.
  - Apply/Cancel/close discards local Redo history together with local Undo history.
  - Tests cover branching, capacity overflow, nested object independence and shortcut focus priority.

- Outside scope

  - Committed ConfigItem list Redo (issue 07).
  - Legacy WinForms Output Config editor, unless separately approved.
  - Project/global settings Redo.

### 07-profile-redo

**Parent:** #2886  
**Delivery:** Extension (not MVP)  
**Depends on:** 02-profile-undo and relevant policy in 03

- Goal

  - Reapply previously undone committed ConfigItem list operations, with authoritative .NET state and UI consistency.

- Proposed scope

  - Redo supported Add, Delete, Duplicate, Toggle Active, and Reorder actions after their Undo implementations have been delivered.
  - Preserve the intended GUID, original/created identities, destination profile and item ordering.
  - Redo the recorded semantic action's resulting state rather than recomputing a Toggle from potentially changed UI state.
  - Clear the Redo branch after a new committed edit.
  - Preserve bulk-action atomicity and cache/notification behavior.

- Acceptance criteria

  - Undo then Redo restores the exact intended committed configuration state.
  - Duplicate Redo does not allocate an unexpected new logical identity.
  - Bulk and cross-profile Redo restore all affected items or fail without partial project mutations.
  - Normal change events and frontend updates are emitted consistently.
  - Tests cover redo ordering, branch invalidation, item identity, cache refresh and error handling.

- Outside scope

  - Editor-local draft Redo (issue 06).
  - Project settings/profile metadata Redo (issue 08).
  - Global settings Redo (issue 09).

### 08-project-redo

**Parent:** #2886  
**Delivery:** Extension (not MVP)  
**Depends on:** 04-project-undo

- Goal

  - Reapply previously undone project-level metadata or project-structural changes supported by Project Undo.

- Proposed scope

  - Support Redo for project name and selected approved project settings.
  - If issue 04 includes profile rename/order, support Redo of those changes as well.
  - Maintain correct project state, profile identity, selection and UI updates.
  - Define Redo branch invalidation when a new committed project change occurs.

- Acceptance criteria

  - Undo followed by Redo restores the exact intended project settings and profile structure.
  - No unrelated ConfigItems are changed.
  - External side effects and UI notifications are applied through approved project pathways.
  - Tests cover ordering, branch invalidation and failure rollback.

- Outside scope

  - ConfigItem editor/list Redo (issues 06 and 07).
  - Global settings Redo (issue 09).

### 09-global-settings-redo

**Parent:** #2886  
**Delivery:** Extension (not MVP)  
**Depends on:** 05-global-settings-undo

- Goal

  - Reapply previously undone supported application-wide settings changes safely.

- Proposed scope

  - Add Redo only for settings already approved as safely reversible under issue 05.
  - Preserve the exact intended value and apply required persistence/UI notifications.
  - Invalidate prior Redo entries when a new relevant global setting change is committed.
  - Avoid blindly re-running non-idempotent external side effects.

- Acceptance criteria

  - Undo followed by Redo restores the expected setting value and observable application state.
  - Unsupported or irreversible settings remain unavailable for Redo.
  - History branching and failure boundaries are correct.
  - Tests verify persistence, notifications and representative settings such as log level.

- Outside scope

  - ConfigItem and project Redo (issues 06–08).
  - Automatic support for settings with unverified side effects.

## Proposed decision order

1. Confirm whether Editor Undo/Redo or List Undo/Redo ships first.
2. Approve or adjust the initial architecture and the history lifetime/limit policy.
3. Confirm the minimal dependencies for the first implementation.
4. Defer profile/project/global work unless it is explicitly part of the initial milestone.