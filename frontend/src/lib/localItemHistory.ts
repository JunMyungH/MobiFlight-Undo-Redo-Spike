
export type LocalItemHistory<T> = {
  readonly snapshots: readonly T[]
  readonly actions: readonly string[]
  readonly cursor: number
  readonly maxEntries: number
}

function validateLimit(maxEntries: number): void {
  if (!Number.isInteger(maxEntries) || maxEntries < 1) {
    throw new RangeError(
      'maxEntries must be a positive integer',
    )
  }
}

export function createLocalItemHistory<T>(
  initial: T,
  maxEntries = 25,
): LocalItemHistory<T> {
  validateLimit(maxEntries)

  return {
    snapshots: [structuredClone(initial)],
    actions: [],
    cursor: 0,
    maxEntries,
  }
}

export function currentLocalItem<T>(
  history: LocalItemHistory<T>,
): T {
  return history.snapshots[history.cursor]
}

export function recordLocalItemChange<T>(
  history: LocalItemHistory<T>,
  next: T,
  actionName: string,
  sameState: (a: T, b: T) => boolean,
): LocalItemHistory<T> {
  if (!actionName.trim()) {
    throw new Error('Action name cannot be empty')
  }

  if (sameState(currentLocalItem(history), next)) {
    return history
  }

  // Remove any Redo branch after a new edit.
  let snapshots = [
    ...history.snapshots.slice(0, history.cursor + 1),
    structuredClone(next),
  ]

  let actions = [
    ...history.actions.slice(0, history.cursor),
    actionName,
  ]

  // N Undo steps require N + 1 snapshots.
  const overflow = Math.max(
    0,
    actions.length - history.maxEntries,
  )

  if (overflow > 0) {
    snapshots = snapshots.slice(overflow)
    actions = actions.slice(overflow)
  }

  return {
    ...history,
    snapshots,
    actions,
    cursor: snapshots.length - 1,
  }
}

export function undoLocalItem<T>(
  history: LocalItemHistory<T>,
): LocalItemHistory<T> {
  return history.cursor > 0
    ? { ...history, cursor: history.cursor - 1 }
    : history
}

export function redoLocalItem<T>(
  history: LocalItemHistory<T>,
): LocalItemHistory<T> {
  return history.cursor < history.actions.length
    ? { ...history, cursor: history.cursor + 1 }
    : history
}

export function resizeLocalItemHistory<T>(
  history: LocalItemHistory<T>,
  maxEntries: number,
): LocalItemHistory<T> {
  validateLimit(maxEntries)

  // Preserve the current state and the most recent
  // Undo states whenever possible.
  const overflow = Math.max(
    0,
    history.actions.length - maxEntries,
  )

  const trimBefore = Math.min(
    overflow,
    history.cursor,
  )

  let snapshots =
    history.snapshots.slice(trimBefore)

  let actions =
    history.actions.slice(trimBefore)

  const cursor = history.cursor - trimBefore

  // Trim distant Redo states if necessary.
  const trimAfter = Math.max(
    0,
    actions.length - maxEntries,
  )

  if (trimAfter > 0) {
    snapshots = snapshots.slice(
      0,
      snapshots.length - trimAfter,
    )

    actions = actions.slice(
      0,
      actions.length - trimAfter,
    )
  }

  return {
    snapshots,
    actions,
    cursor,
    maxEntries,
  }
}
