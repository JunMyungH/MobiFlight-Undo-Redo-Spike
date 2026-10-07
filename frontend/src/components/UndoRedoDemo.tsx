import {
  useEffect,
  useState,
  useCallback,
} from 'react'

import {
  compoundEdit,
  deleteItem,
  duplicateFirstItem,
  editHybridConfigItem,
  getState,
  moveFirstToLast,
  redo,
  redoTo,
  resetExperiment,
  runBulkDelete,
  runBulkDeleteIndexed,
  runBulkDeleteSnapshot,
  runBulkToggle,
  runBulkToggleIndexed,
  runBulkToggleReference,
  toggleItem,
  undo,
  undoTo,
} from '../api/spikeApi'

import type {
  Approach,
} from '../api/spikeApi'

import type {
  ConfigItem,
  ExperimentResponse,
  SpikeState,
} from '../types'

type Props = {
  approach: Approach
  title: string
}

const buttonBase =
  'inline-flex items-center justify-center rounded-lg border px-3.5 py-2 text-sm font-medium transition-all duration-150 disabled:cursor-not-allowed disabled:border-white/5 disabled:bg-slate-900 disabled:text-slate-600 disabled:shadow-none'

const buttonNeutral =
  `${buttonBase} border-white/10 bg-slate-900 text-slate-300 hover:border-white/20 hover:bg-slate-800 hover:text-white`

const buttonAccent =
  `${buttonBase} border-indigo-400/20 bg-indigo-500/10 text-indigo-300 hover:border-indigo-400/40 hover:bg-indigo-500/20 hover:text-indigo-200`

const buttonDanger =
  `${buttonBase} border-rose-400/20 bg-rose-500/10 text-rose-300 hover:border-rose-400/40 hover:bg-rose-500/20 hover:text-rose-200`

const approachDescriptions:
  Record<Approach, string> = {
    command:
      'Each transaction is represented by a semantic command with explicit execute, undo and redo behavior.',

    snapshot:
      'History stores complete state snapshots so an earlier project state can be restored directly.',

    patch:
      'History stores only the affected values or objects instead of copying the complete project state.',

    hybrid:
      'A single semantic history can use patches for localized mutations and snapshots for broad structural changes.',
  }

function UndoRedoDemo({
  approach,
  title,
}: Props) {
  const [state, setState] =
    useState<SpikeState | null>(null)

  const [error, setError] =
    useState<string | null>(null)

  const [pending, setPending] =
    useState(false)

  const [executeMs, setExecuteMs] =
    useState<number | null>(null)

  const [undoMs, setUndoMs] =
    useState<number | null>(null)

  const [redoMs, setRedoMs] =
    useState<number | null>(null)

  const [undoOperations, setUndoOperations] =
    useState<number | null>(null)

  const [redoOperations, setRedoOperations] =
    useState<number | null>(null)

  const [editingItemId, setEditingItemId] =
    useState<string | null>(null)

  const [draftName, setDraftName] =
    useState('')

  const [draftActive, setDraftActive] =
    useState(false)

  const clearBenchmarks = useCallback(() => {
    setExecuteMs(null)
    setUndoMs(null)
    setRedoMs(null)

    setUndoOperations(null)
    setRedoOperations(null)
  }, [])

  const openEditor =
  useCallback(
    (item: ConfigItem) => {
      setEditingItemId(
        item.id,
      )

      setDraftName(
        item.name,
      )

      setDraftActive(
        item.active,
      )

      setError(null)
    },
    [],
  )

  const cancelEditor =
  useCallback(() => {
    setEditingItemId(null)
    setDraftName('')
    setDraftActive(false)
  }, [])

  useEffect(() => {
    let cancelled = false

    const loadState = async () => {
      try {
        const data =
          await getState(approach)

        if (!cancelled) {
          setState(data)
          setError(null)
        }
      } catch (err: unknown) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : 'Unknown error',
          )
        }
      }
    }

    void loadState()

    return () => {
      cancelled = true
    }
  }, [approach])

  const runAction = useCallback(
    async (
      action: () => Promise<SpikeState>,
    ) => {
      try {
        setPending(true)
        setError(null)

        const data = await action()

        setState(data)

        clearBenchmarks()
      } catch (err: unknown) {
        setError(
          err instanceof Error
            ? err.message
            : 'Unknown error',
        )
      } finally {
        setPending(false)
      }
    },
    [clearBenchmarks],
  )

  const runHistoryAction = useCallback(
    async (
      action:
        () => Promise<ExperimentResponse>,
      operation: 'undo' | 'redo',
    ) => {
      try {
        setPending(true)
        setError(null)

        const result = await action()

        setState(result.state)

        if (operation === 'undo') {
          setUndoMs(
            result.benchmark
              .elapsedMilliseconds,
          )

          setUndoOperations(
            result.benchmark.operations,
          )
        } else {
          setRedoMs(
            result.benchmark
              .elapsedMilliseconds,
          )

          setRedoOperations(
            result.benchmark.operations,
          )
        }
      } catch (err: unknown) {
        setError(
          err instanceof Error
            ? err.message
            : 'Unknown error',
        )
      } finally {
        setPending(false)
      }
    },
    [],
  )

  const runExperimentAction = useCallback(
    async (
      action:
        () => Promise<ExperimentResponse>,
    ) => {
      try {
        setPending(true)
        setError(null)

        const result = await action()

        setState(result.state)

        setExecuteMs(
          result.benchmark
            .elapsedMilliseconds,
        )

        setUndoMs(null)
        setRedoMs(null)
        setUndoOperations(null)
        setRedoOperations(null)
      } catch (err: unknown) {
        setError(
          err instanceof Error
            ? err.message
            : 'Unknown error',
        )
      } finally {
        setPending(false)
      }
    },
    [],
  )

  const applyEditor =
  useCallback(
    async () => {
      if (
        editingItemId === null
      ) {
        return
      }

      try {
        setPending(true)
        setError(null)

        const data =
          await editHybridConfigItem(
            editingItemId,
            {
              name: draftName,
              active: draftActive,
            },
          )

        setState(data)

        clearBenchmarks()

        cancelEditor()
      } catch (err: unknown) {
        setError(
          err instanceof Error
            ? err.message
            : 'Unknown error',
        )
      } finally {
        setPending(false)
      }
    },
    [
      editingItemId,
      draftName,
      draftActive,
      clearBenchmarks,
      cancelEditor,
    ],
  )

  useEffect(() => {
    if (!state) {
      return
    }

    const {
      canUndo,
      canRedo,
    } = state

    const handleKeyDown = (
      event: KeyboardEvent,
    ) => {
      const target = event.target

      if (
        target instanceof HTMLTextAreaElement ||
        (
          target instanceof HTMLInputElement &&
          ![
            "checkbox",
            "radio",
            "button",
            "submit",
            "reset",
          ].includes(target.type)
        ) ||
        (
          target instanceof HTMLElement &&
          target.isContentEditable
        )
      ) {
        return
      }

      if (
        editingItemId !== null
      ) {
        return
      }

      if (pending) {
        return
      }

      const key =
        event.key.toLowerCase()

      const modifierPressed =
        event.ctrlKey ||
        event.metaKey

      if (
        modifierPressed &&
        key === "z" &&
        !event.shiftKey &&
        canUndo
      ) {
        event.preventDefault()

        void runHistoryAction(
          () => undo(approach),
          "undo",
        )

        return
      }

      if (
        modifierPressed &&
        (
          key === "y" ||
          (
            key === "z" &&
            event.shiftKey
          )
        ) &&
        canRedo
      ) {
        event.preventDefault()

        void runHistoryAction(
          () => redo(approach),
          "redo",
        )

        return
      }

      if (
        event.key === "Delete" &&
        !modifierPressed
      ) {
        const selectedItems =
          state.projectState.configItems
            .filter(
              (item) =>
                item.active,
            )

        if (
          selectedItems.length === 0
        ) {
          return
        }

        event.preventDefault()

        if (
          selectedItems.length === 1
        ) {
          void runAction(() =>
            deleteItem(
              approach,
              selectedItems[0].id,
            ),
          )

          return
        }

        void runExperimentAction(
          () =>
            runBulkDelete(
              approach,
            ),
        )
      }
    }

    window.addEventListener(
      "keydown",
      handleKeyDown,
    )

    return () => {
      window.removeEventListener(
        "keydown",
        handleKeyDown,
      )
    }
  }, [
    approach,
    pending,
    state,
    runAction,
    runHistoryAction,
    runExperimentAction,
    editingItemId,
  ])

  useEffect(() => {
    if (
      editingItemId === null
    ) {
      return
    }

    const handleEscape = (
      event: KeyboardEvent,
    ) => {
      if (
        event.key !== 'Escape' ||
        pending
      ) {
        return
      }

      event.preventDefault()

      cancelEditor()
    }

    window.addEventListener(
      'keydown',
      handleEscape,
    )

    return () => {
      window.removeEventListener(
        'keydown',
        handleEscape,
      )
    }
  }, [
    editingItemId,
    pending,
    cancelEditor,
  ])
  
  if (error && !state) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="w-full max-w-lg rounded-2xl border border-rose-400/20 bg-rose-500/5 p-6">
          <p className="text-sm font-semibold text-rose-300">
            Failed to load state
          </p>

          <p className="mt-2 text-sm text-slate-400">
            {error}
          </p>
        </div>
      </div>
    )
  }
  
  if (!state) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="flex items-center gap-3 text-sm text-slate-400">
          <div className="h-4 w-4 animate-spin rounded-full border-2 border-slate-700 border-t-indigo-400" />

          Loading spike state...
        </div>
      </div>
    )
  }
  
  const itemCount =
  state.projectState.configItems.length
  
  const activeItemCount =
  state.projectState.configItems
  .filter((item) => item.active)
  .length
  
  return (
    <main className="space-y-6">
      <header className="flex flex-col gap-5 xl:flex-row xl:items-start xl:justify-between">
        <div className="max-w-3xl">
          <div className="mb-3 flex items-center gap-2">
            <span className="rounded-full border border-indigo-400/20 bg-indigo-500/10 px-2.5 py-1 text-[11px] font-semibold tracking-[0.12em] text-indigo-300 uppercase">
              {approach}
            </span>

            {pending && (
              <span className="flex items-center gap-1.5 text-xs text-amber-300">
                <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-400" />
                Processing
              </span>
            )}
          </div>

          <h1 className="text-3xl font-semibold tracking-tight text-white md:text-4xl">
            {title}
          </h1>

          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-400">
            {
              approachDescriptions[
                approach
              ]
            }
          </p>
        </div>

        <div className="flex shrink-0 gap-2 rounded-xl border border-white/10 bg-slate-900/70 p-2 shadow-xl shadow-black/10">
          <button
            type="button"
            disabled={
              pending ||
              !state.canUndo
            }
            onClick={() =>
              runHistoryAction(
                () => undo(approach),
                "undo",
              )
            }
            className={buttonAccent}
            title="Undo (Ctrl+Z)"
          >
            <span className="mr-1 text-base">
              ↶
            </span>

            Undo

            <kbd className="ml-2 rounded border border-indigo-400/20 bg-black/20 px-1.5 py-0.5 font-mono text-[10px] text-indigo-300">
              Ctrl Z
            </kbd>

            <span className="rounded bg-black/20 px-1.5 py-0.5 font-mono text-[10px] text-indigo-400">
              {state.diagnostics.undoEntries}
            </span>
          </button>

          <button
            type="button"
            disabled={
              pending ||
              !state.canRedo
            }
            onClick={() =>
              runHistoryAction(
                () => redo(approach),
                "redo",
              )
            }
            className={buttonNeutral}
            title="Redo (Ctrl+Y)"
          >
            <span className="mr-1 text-base">
              ↷
            </span>

            Redo

            <kbd className="ml-2 rounded border border-white/10 bg-black/20 px-1.5 py-0.5 font-mono text-[10px] text-slate-400">
              Ctrl Y
            </kbd>

            <span className="rounded bg-black/20 px-1.5 py-0.5 font-mono text-[10px] text-slate-500">
              {state.diagnostics.redoEntries}
            </span>
          </button>
        </div>
      </header>

      <section className="overflow-hidden rounded-2xl border border-white/10 bg-slate-900/50 shadow-2xl shadow-black/10">
        <div className="border-b border-white/10 px-5 py-4">
          <h2 className="text-sm font-semibold text-white">
            Experiment Controls
          </h2>

          <p className="mt-1 text-xs text-slate-500">
            Prepare datasets and execute the mutations used to compare each history strategy.
          </p>
        </div>

        <div className="grid gap-px bg-white/5 xl:grid-cols-3">
          <div className="bg-slate-950/40 p-5">
            <div className="mb-4">
              <p className="text-xs font-semibold tracking-wider text-slate-300 uppercase">
                Dataset
              </p>

              <p className="mt-1 text-xs text-slate-600">
                Reset the project state.
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                disabled={pending}
                onClick={() =>
                  runAction(() =>
                    resetExperiment(
                      approach,
                      3,
                    ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                Reset 3 Items
              </button>

              <button
                disabled={pending}
                onClick={() =>
                  runAction(() =>
                    resetExperiment(
                      approach,
                      100,
                    ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                100 Items
              </button>

              <button
                disabled={pending}
                onClick={() =>
                  runAction(() =>
                    resetExperiment(
                      approach,
                      1000,
                    ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                1000 Items
              </button>
            </div>
          </div>

          <div className="bg-slate-950/40 p-5">
            <div className="mb-4">
              <p className="text-xs font-semibold tracking-wider text-slate-300 uppercase">
                Mutations
              </p>

              <p className="mt-1 text-xs text-slate-600">
                Semantic single-operation cases.
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                disabled={
                  pending ||
                  itemCount === 0
                }
                onClick={() =>
                  runAction(() =>
                    compoundEdit(
                      approach,
                    ),
                  )
                }
                className={
                  buttonAccent
                }
              >
                Compound Edit
              </button>

              <button
                disabled={
                  pending ||
                  itemCount === 0
                }
                onClick={() =>
                  runExperimentAction(
                    () =>
                      duplicateFirstItem(
                        approach,
                      ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                Duplicate First
              </button>

              <button
                disabled={
                  pending ||
                  itemCount < 2
                }
                onClick={() =>
                  runExperimentAction(
                    () =>
                      moveFirstToLast(
                        approach,
                      ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                Move First → Last
              </button>
            </div>
          </div>

          <div className="bg-slate-950/40 p-5">
            <div className="mb-4">
              <p className="text-xs font-semibold tracking-wider text-slate-300 uppercase">
                Bulk Benchmark
              </p>

              <p className="mt-1 text-xs text-slate-600">
                Stress the history implementation.
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                disabled={
                  pending ||
                  itemCount === 0
                }
                onClick={() =>
                  runExperimentAction(
                    () =>
                      runBulkToggle(
                        approach,
                      ),
                  )
                }
                className={
                  buttonAccent
                }
              >
                Bulk Toggle
              </button>

              {(
                approach === 'patch' ||
                approach === 'hybrid'
              ) && (
                <button
                  disabled={
                    pending ||
                    itemCount === 0
                  }
                  onClick={() =>
                    runExperimentAction(
                      () =>
                        runBulkToggleReference(
                          approach,
                        ),
                    )
                  }
                  className={
                    buttonNeutral
                  }
                >
                  Toggle Direct Ref
                </button>
              )}

              {(
                approach === 'patch' ||
                approach === 'hybrid'
              ) && (
                <button
                  disabled={
                    pending ||
                    itemCount === 0
                  }
                  onClick={() =>
                    runExperimentAction(
                      () =>
                        runBulkToggleIndexed(
                          approach,
                        ),
                    )
                  }
                  className={
                    buttonNeutral
                  }
                >
                  Toggle Indexed
                </button>
              )}

              <button
                disabled={
                  pending ||
                  itemCount === 0
                }
                onClick={() =>
                  runExperimentAction(
                    () =>
                      runBulkDelete(
                        approach,
                      ),
                  )
                }
                className={
                  buttonDanger
                }
              >
                Bulk Delete
              </button>

              {approach ===
                'hybrid' && (
                <button
                  disabled={
                    pending ||
                    itemCount === 0
                  }
                  onClick={() =>
                    runExperimentAction(
                      () =>
                        runBulkDeleteSnapshot(
                          approach,
                        ),
                    )
                  }
                  className={
                    buttonNeutral
                  }
                >
                  Delete Snapshot
                </button>
              )}

              <button
                disabled={
                  pending ||
                  itemCount === 0
                }
                onClick={() =>
                  runExperimentAction(
                    () =>
                      runBulkDeleteIndexed(
                        approach,
                      ),
                  )
                }
                className={
                  buttonNeutral
                }
              >
                Delete Stored Index
              </button>
            </div>
          </div>
        </div>
      </section>

      {(
        executeMs !== null ||
        undoMs !== null ||
        redoMs !== null
      ) && (
        <section className="grid gap-3 md:grid-cols-3">
          <BenchmarkCard
            label="Execute"
            value={executeMs}
          />

          <BenchmarkCard
            label="Undo"
            value={undoMs}
            operations={undoOperations}
          />

          <BenchmarkCard
            label="Redo"
            value={redoMs}
            operations={redoOperations}
          />
        </section>
      )}

      <section className="overflow-hidden rounded-2xl border border-white/10 bg-slate-900/50">
        <div className="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 px-5 py-4">
          <div>
            <h2 className="text-sm font-semibold text-white">
              Config Items
            </h2>

            <p className="mt-1 text-xs text-slate-500">
              Current project state after the latest transaction.
            </p>
          </div>

          <div className="flex gap-2">
            <StatusPill
              label="Items"
              value={itemCount}
            />

            <StatusPill
              label="Active"
              value={activeItemCount}
            />
          </div>
        </div>

        {itemCount === 0 ? (
          <div className="px-5 py-12 text-center">
            <p className="text-sm text-slate-500">
              No config items in the current project state.
            </p>
          </div>
        ) : (
          <div className="max-h-105 divide-y divide-white/5 overflow-y-auto">
            {state.projectState.configItems.map(
              (item, index) => (
                <div
                  key={item.id}
                  className={[
                    "group flex cursor-pointer items-center gap-4 px-5 py-3 transition-colors",
                    item.active
                      ? "bg-indigo-500/2.5 hover:bg-indigo-500/[0.07]"
                      : "hover:bg-white/[0.035]",
                  ].join(" ")}
                  onClick={() => {
                    if (pending) {
                      return
                    }

                    void runAction(() =>
                      toggleItem(
                        approach,
                        item.id,
                      ),
                    )
                  }}
                >
                  <span className="w-8 shrink-0 font-mono text-[11px] text-slate-600">
                    #
                    {String(
                      index + 1,
                    ).padStart(
                      2,
                      '0',
                    )}
                  </span>

                  <input
                    type="checkbox"
                    checked={item.active}
                    disabled={pending}
                    onClick={(event) => {
                      event.stopPropagation()
                    }}
                    onChange={() =>
                      runAction(() =>
                        toggleItem(
                          approach,
                          item.id,
                        ),
                      )
                    }
                    className="h-4 w-4 cursor-pointer rounded border-slate-700 bg-slate-900 accent-indigo-500 disabled:cursor-not-allowed"
                  />

                  <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-medium text-slate-300">
                      {item.name}
                    </div>

                    <div className="mt-0.5 truncate font-mono text-[10px] text-slate-600">
                      {item.id}
                    </div>
                  </div>

                  <span
                    className={[
                      'rounded-full px-2 py-0.5 text-[10px] font-medium',
                      item.active
                        ? 'bg-emerald-500/10 text-emerald-400'
                        : 'bg-slate-800 text-slate-500',
                    ].join(' ')}
                  >
                    {item.active
                      ? 'Active'
                      : 'Inactive'}
                  </span>

                  {approach ===
                    'hybrid' && (
                    <button
                      type="button"
                      disabled={pending}
                      onClick={(event) => {
                        event.stopPropagation()

                        openEditor(item)
                      }}
                      className="rounded-lg px-2.5 py-1.5 text-xs font-medium text-slate-500 transition-colors hover:bg-indigo-500/10 hover:text-indigo-300 disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Edit
                    </button>
                  )}

                  <button
                    type="button"
                    disabled={pending}
                    onClick={(event) => {
                      event.stopPropagation()

                      void runAction(() =>
                        deleteItem(
                          approach,
                          item.id,
                        ),
                      )
                    }}
                    className="rounded-lg px-2.5 py-1.5 text-xs font-medium text-slate-500 transition-colors hover:bg-rose-500/10 hover:text-rose-300 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Delete
                  </button>
                </div>
              ),
            )}
          </div>
        )}
      </section>

      <section className="overflow-hidden rounded-2xl border border-white/10 bg-slate-900/50">
        <div className="border-b border-white/10 px-5 py-4">
          <div className="flex items-center gap-2">
            <div className="h-2 w-2 rounded-full bg-cyan-400 shadow-[0_0_12px_rgba(34,211,238,0.7)]" />

            <h2 className="text-sm font-semibold text-white">
              History Inspector
            </h2>
          </div>

          <p className="mt-1 text-xs text-slate-500">
            Inspect how the selected implementation stores undo and redo state.
            Double-click a history entry to jump through that point as one grouped action.
          </p>
        </div>

        <dl className="grid gap-px bg-white/5 sm:grid-cols-2 xl:grid-cols-5">
          <HistoryMetric
            label="Representation"
            value={
              state.diagnostics
                .representation
            }
          />

          <HistoryMetric
            label="Undo Entries"
            value={
              state.diagnostics
                .undoEntries
            }
          />

          <HistoryMetric
            label="Redo Entries"
            value={
              state.diagnostics
                .redoEntries
            }
          />

          <HistoryMetric
            label="Config Items"
            value={itemCount}
          />

          {state.diagnostics
            .storedConfigItemCopies !==
            null && (
            <HistoryMetric
              label="Stored Copies"
              value={
                state.diagnostics
                  .storedConfigItemCopies
              }
            />
          )}
        </dl>

        <div className="grid gap-px bg-white/5 lg:grid-cols-2">
          <HistoryStack
            title="Undo Stack"
            entries={
              state.diagnostics
                .undoEntryDetails
            }
            accent="indigo"
            disabled={pending}
            onEntryDoubleClick={
              (index) => {
                if (index === 0) {
                  void runHistoryAction(
                    () =>
                      undo(approach),
                    "undo",
                  )

                  return
                }

                const steps =
                  index + 1

                void runHistoryAction(
                  () =>
                    undoTo(
                      approach,
                      steps,
                    ),
                  "undo",
                )
              }
            }
          />

          <HistoryStack
            title="Redo Stack"
            entries={
              state.diagnostics
                .redoEntryDetails
            }
            accent="cyan"
            disabled={pending}
            onEntryDoubleClick={
              (index) => {
                if (index === 0) {
                  void runHistoryAction(
                    () =>
                      redo(approach),
                    "redo",
                  )

                  return
                }

                const steps =
                  index + 1

                void runHistoryAction(
                  () =>
                    redoTo(
                      approach,
                      steps,
                    ),
                  "redo",
                )
              }
            }
          />
        </div>
      </section>

      {approach === 'hybrid' &&
      editingItemId !== null && (
         <>
          <div
            className="fixed inset-0 z-40 bg-transparent"
            aria-hidden="true"
          />

          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="edit-config-item-title"
            className="fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col border-l border-white/10 bg-slate-900 shadow-2xl shadow-black/50"
          >
            <div className="border-b border-white/10 px-5 py-4">
              <div className="flex items-center justify-between gap-4">
                <div>
                  <h2
                    id="edit-config-item-title"
                    className="text-sm font-semibold text-white"
                  >
                    Edit Config Item
                  </h2>

                  <p className="mt-1 text-xs text-slate-500">
                    Changes remain local
                    until Apply.
                  </p>
                </div>

                <span className="rounded-full border border-amber-400/20 bg-amber-500/10 px-2.5 py-1 text-[10px] font-semibold tracking-wider text-amber-300 uppercase">
                  Draft
                </span>
              </div>
            </div>

            <form
              onSubmit={(event) => {
                event.preventDefault()

                void applyEditor()
              }}
              className="flex flex-1 flex-col gap-5 overflow-y-auto p-5"
            >
              <div>
                <label
                  htmlFor="draft-config-name"
                  className="mb-2 block text-xs font-medium text-slate-400"
                >
                  Name
                </label>

                <input
                  id="draft-config-name"
                  type="text"
                  autoFocus
                  value={draftName}
                  disabled={pending}
                  onChange={(event) =>
                    setDraftName(
                      event.target.value,
                    )
                  }
                  className="w-full rounded-lg border border-white/10 bg-slate-950 px-3 py-2.5 text-sm text-slate-200 outline-none transition-colors placeholder:text-slate-700 focus:border-indigo-400/40 focus:ring-2 focus:ring-indigo-500/10 disabled:opacity-50"
                />

                <p className="mt-2 text-[11px] leading-5 text-slate-600">
                  Ctrl+Z while this field is
                  focused uses native text
                  editing Undo and does not
                  touch Project history.
                </p>
              </div>

              <label className="flex cursor-pointer items-center justify-between rounded-xl border border-white/10 bg-slate-950/60 px-4 py-3">
                <div>
                  <div className="text-sm font-medium text-slate-300">
                    Active
                  </div>

                  <div className="mt-0.5 text-xs text-slate-600">
                    Draft value only until
                    Apply.
                  </div>
                </div>

                <input
                  type="checkbox"
                  checked={draftActive}
                  disabled={pending}
                  onChange={(event) =>
                    setDraftActive(
                      event.target.checked,
                    )
                  }
                  className="h-4 w-4 cursor-pointer rounded border-slate-700 bg-slate-900 accent-indigo-500"
                />
              </label>

              <div className="rounded-xl border border-cyan-400/10 bg-cyan-500/5 px-4 py-3 text-xs leading-5 text-cyan-200/70">
                ProjectState and global
                history are unchanged while
                this editor is open.
              </div>

              <div className="mt-auto flex justify-end gap-2 border-t border-white/10 pt-4">
                <button
                  type="button"
                  disabled={pending}
                  onClick={
                    cancelEditor
                  }
                  className={
                    buttonNeutral
                  }
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  disabled={pending}
                  className={
                    buttonAccent
                  }
                >
                  {pending
                    ? 'Applying...'
                    : 'Apply'}
                </button>
              </div>

              <p className="text-right text-[10px] text-slate-600">
                Esc to cancel
              </p>
            </form>
          </div>
        </>
      )}

      {error && (
        <div className="rounded-xl border border-rose-400/20 bg-rose-500/5 px-4 py-3 text-sm text-rose-300">
          Request failed: {error}
        </div>
      )}
    </main>
  )
}

type BenchmarkCardProps = {
  label: string
  value: number | null
  operations?: number | null
}

function BenchmarkCard({
  label,
  value,
  operations = null,
}: BenchmarkCardProps) {
  return (
    <div className="rounded-2xl border border-white/10 bg-linear-to-br from-slate-900 to-slate-950 p-5">
      <div className="flex items-center justify-between">
        <p className="text-xs font-medium tracking-wide text-slate-500 uppercase">
          {label}
        </p>

        {operations !== null && (
          <span className="rounded-md border border-white/10 bg-slate-950 px-2 py-1 font-mono text-[10px] text-slate-500">
            {operations}{' '}
            {operations === 1
              ? 'entry'
              : 'entries'}
          </span>
        )}
      </div>

      <div className="mt-3 flex items-end gap-2">
        <span className="font-mono text-2xl font-semibold tracking-tight text-white">
          {value !== null
            ? value.toFixed(4)
            : '—'}
        </span>

        <span className="pb-1 text-xs text-slate-600">
          ms
        </span>
      </div>
    </div>
  )
}

type StatusPillProps = {
  label: string
  value: number
}

function StatusPill({
  label,
  value,
}: StatusPillProps) {
  return (
    <div className="rounded-lg border border-white/10 bg-slate-950 px-2.5 py-1.5 text-xs">
      <span className="text-slate-500">
        {label}
      </span>

      <span className="ml-2 font-mono font-semibold text-slate-300">
        {value}
      </span>
    </div>
  )
}

type HistoryMetricProps = {
  label: string
  value:
    | string
    | number
}

function HistoryMetric({
  label,
  value,
}: HistoryMetricProps) {
  return (
    <div className="bg-slate-950/50 p-4">
      <dt className="text-[10px] font-semibold tracking-wider text-slate-600 uppercase">
        {label}
      </dt>

      <dd className="mt-2 wrap-break-word font-mono text-sm font-medium text-slate-300">
        {value}
      </dd>
    </div>
  )
}

type HistoryStackProps = {
  title: string
  entries: string[]
  accent: 'indigo' | 'cyan'
  disabled?: boolean
  onEntryDoubleClick?: (
    index: number,
  ) => void
}

function HistoryStack({
  title,
  entries,
  accent,
  disabled = false,
  onEntryDoubleClick,
}: HistoryStackProps) {
  const accentClass =
    accent === 'indigo'
      ? 'bg-indigo-400'
      : 'bg-cyan-400'

  return (
    <div className="min-w-0 bg-slate-950/40 p-5">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span
            className={`h-1.5 w-1.5 rounded-full ${accentClass}`}
          />

          <h3 className="text-xs font-semibold text-slate-300">
            {title}
          </h3>
        </div>

        <span className="text-[10px] text-slate-600">
          Newest first
        </span>
      </div>

      {entries.length === 0 ? (
        <div className="rounded-xl border border-dashed border-white/10 px-4 py-8 text-center text-xs text-slate-600">
          Empty
        </div>
      ) : (
        <ol className="space-y-2">
          {entries.map(
            (
              entry,
              index,
            ) => (
              <li
                key={`${entry}-${index}`}
                onDoubleClick={() => {
                  if (
                    disabled ||
                    !onEntryDoubleClick
                  ) {
                    return
                  }

                  onEntryDoubleClick(
                    index,
                  )
                }}
                title={
                  onEntryDoubleClick
                    ? "Double-click to jump through this history entry"
                    : undefined
                }
                className={[
                  "flex gap-3 rounded-lg border border-white/5 bg-slate-900/60 px-3 py-2.5 transition-all",
                  onEntryDoubleClick &&
                    !disabled
                    ? "cursor-pointer select-none hover:border-indigo-400/25 hover:bg-indigo-500/5"
                    : "",
                ].join(" ")}
              >
                <span className="shrink-0 font-mono text-[10px] text-slate-600">
                  {index + 1}
                </span>

                <code className="min-w-0 wrap-break-word font-mono text-xs leading-5 text-slate-400">
                  {entry}
                </code>
              </li>
            ),
          )}
        </ol>
      )}
    </div>
  )
}

export default UndoRedoDemo