import { useEffect, useState } from 'react'

import {
  compoundEdit,
  deleteItem,
  getState,
  redo,
  resetExperiment,
  runBulkToggle,
  runBulkToggleReference,
  runBulkToggleIndexed,
  runBulkDeleteIndexed,
  runBulkDeleteSnapshot,
  runBulkDelete,
  toggleItem,
  undo,
  duplicateFirstItem,
  moveFirstToLast,
} from '../api/spikeApi'

import type { Approach } from '../api/spikeApi'
import type { ExperimentResponse, SpikeState } from '../types'

type Props = {
  approach: Approach
  title: string
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

  const clearBenchmarks = () => {
    setExecuteMs(null)
    setUndoMs(null)
    setRedoMs(null)
  }

  useEffect(() => {
    let cancelled = false

    const loadState = async () => {
      try {
        const data = await getState(approach)

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

  const runAction = async (
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
  }

  const runHistoryAction = async (
  action: () => Promise<ExperimentResponse>,
  operation: 'undo' | 'redo',
) => {
  try {
    setPending(true)
    setError(null)

    const result = await action()

    setState(result.state)

    if (operation === 'undo') {
      setUndoMs(
        result.benchmark.elapsedMilliseconds,
      )
    } else {
      setRedoMs(
        result.benchmark.elapsedMilliseconds,
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
}
  
  const runExperimentAction = async (
  action: () => Promise<ExperimentResponse>,
) => {
  try {
    setPending(true)
    setError(null)

    const result = await action()

    setState(result.state)

    setExecuteMs(
      result.benchmark.elapsedMilliseconds,
    )

    setUndoMs(null)
    setRedoMs(null)
  } catch (err: unknown) {
    setError(
      err instanceof Error
        ? err.message
        : 'Unknown error',
    )
  } finally {
    setPending(false)
  }
}

  if (error && !state) {
    return (
      <div>
        <h1>{title}</h1>
        <p>Failed to load state: {error}</p>
      </div>
    )
  }

  if (!state) {
    return (
      <div>
        <h1>{title}</h1>
        <p>Loading...</p>
      </div>
    )
  }

  return (
    <main>
      <h1>{title}</h1>

      <p>
        Active approach: <strong>{approach}</strong>
      </p>

      <div>
        <button
          type="button"
          disabled={pending || !state.canUndo}
          onClick={() =>
            runHistoryAction(() => undo(approach),
          'undo')
          }
        >
          Undo
        </button>

        {' '}

        <button
          type="button"
          disabled={pending || !state.canRedo}
          onClick={() =>
            runHistoryAction(() => redo(approach),
          'redo')
          }
        >
          Redo
        </button>
      </div>

      <section className="experiment-controls">
        <h2>Experiment</h2>

        <button
          disabled={pending}
          onClick={() =>
            runAction(() =>
              resetExperiment(approach, 3),
            )
          }
        >
          Reset 3 Items
        </button>

        {' '}

        <button
          disabled={
            pending ||
            state.projectState.configItems.length === 0
          }
          onClick={() =>
            runAction(() =>
              compoundEdit(approach),
            )
          }
        >
          Apply Compound Edit
        </button>

        {' '}

        <button
          disabled={pending}
          onClick={() =>
            runAction(() =>
              resetExperiment(approach, 100),
            )
          }
        >
          Generate 100 Items
        </button>

        {' '}

        <button
          disabled={pending}
          onClick={() =>
            runAction(() =>
              resetExperiment(approach, 1000),
            )
          }
        >
          Generate 1000 Items
        </button>

        {' '}

        <button
          disabled={
            pending ||
            state.projectState.configItems.length === 0
          }
          onClick={() =>
            runExperimentAction(
              () => runBulkToggle(approach),
            )
          }
        >
          Run Bulk Toggle
        </button>

        {(
          approach === 'patch' ||
          approach === 'hybrid'
        ) && (
          <>
            {' '}

            <button
              disabled={
                pending ||
                state.projectState.configItems.length === 0
              }
              onClick={() =>
                runExperimentAction(
                  () =>
                    runBulkToggleReference(
                      approach,
                    ),
                )
              }
            >
              Run Bulk Toggle (Direct Ref)
            </button>
          </>
        )}

        {' '}

        <button
          disabled={
            pending ||
            state.projectState.configItems.length === 0
          }
          onClick={() =>
            runExperimentAction(
              () => runBulkDelete(approach),
            )
          }
        >
          Run Bulk Delete
        </button>
        
        {approach === 'hybrid' && (
          <>
            {' '}

            <button
              disabled={
                pending ||
                state.projectState.configItems.length === 0
              }
              onClick={() =>
                runExperimentAction(
                  () =>
                    runBulkDeleteSnapshot(
                      approach,
                    ),
                )
              }
            >
              Run Bulk Delete (Snapshot)
            </button>
          </>
        )}

        {' '}
        
        <button
          disabled={
            pending ||
            state.projectState.configItems.length === 0
          }
          onClick={() =>
            runExperimentAction(
              () =>
                runBulkDeleteIndexed(
                  approach,
                ),
            )
          }
        >
          Run Bulk Delete (Stored Index)
        </button>

        {(
          approach === 'patch' ||
          approach === 'hybrid'
        ) && (
          <>
            {' '}

            <button
              disabled={
                pending ||
                state.projectState.configItems.length === 0
              }
              onClick={() =>
                runExperimentAction(
                  () =>
                    runBulkToggleIndexed(
                      approach,
                    ),
                )
              }
            >
              Run Bulk Toggle (Indexed)
            </button>
          </>
        )}

        {' '}

        <button
          disabled={
            pending ||
            state.projectState.configItems.length === 0
          }
          onClick={() =>
            runExperimentAction(
              () => duplicateFirstItem(approach),
            )
          }
        >
          Duplicate First Item
        </button>

        {' '}

        <button
          disabled={
            pending ||
            state.projectState.configItems.length < 2
          }
          onClick={() =>
            runExperimentAction(
              () => moveFirstToLast(approach),
            )
          }
        >
          Move First → Last
        </button>

        {(
          executeMs !== null ||
          undoMs !== null ||
          redoMs !== null
        ) && (
          <div>
            <h3>Benchmark</h3>

            {executeMs !== null && (
              <p>
                Execute: {executeMs.toFixed(4)} ms
              </p>
            )}

            {undoMs !== null && (
              <p>
                Undo: {undoMs.toFixed(4)} ms
              </p>
            )}

            {redoMs !== null && (
              <p>
                Redo: {redoMs.toFixed(4)} ms
              </p>
            )}
          </div>
        )}
      </section>

      <h2>Config Items</h2>

      <ul>
        {state.projectState.configItems.map(
          (item) => (
            <li key={item.id}>
              <input
                type="checkbox"
                checked={item.active}
                disabled={pending}
                onChange={() =>
                  runAction(() =>
                    toggleItem(
                      approach,
                      item.id,
                    ),
                  )
                }
              />

              {' '}
              {item.name}
              {' '}

              <button
                type="button"
                disabled={pending}
                onClick={() =>
                  runAction(() =>
                    deleteItem(
                      approach,
                      item.id,
                    ),
                  )
                }
              >
                Delete
              </button>
            </li>
          ),
        )}
      </ul>

      <section className="history-inspector">
        <h2>History Inspector</h2>

        <dl className="history-summary">
          <div>
            <dt>Representation</dt>
            <dd>{state.diagnostics.representation}</dd>
          </div>

          <div>
            <dt>Undo entries</dt>
            <dd>{state.diagnostics.undoEntries}</dd>
          </div>

          <div>
            <dt>Redo entries</dt>
            <dd>{state.diagnostics.redoEntries}</dd>
          </div>

          <div>
            <dt>Current ConfigItems</dt>
            <dd>
              {state.projectState.configItems.length}
            </dd>
          </div>

          {state.diagnostics.storedConfigItemCopies !== null && (
            <div>
              <dt>Stored ConfigItem copies</dt>
              <dd>
                {state.diagnostics.storedConfigItemCopies}
              </dd>
            </div>
          )}
        </dl>

        <div className="history-stacks">
          <div>
            <h3>Undo stack</h3>
            <p className="history-order">
              Newest first
            </p>

            {state.diagnostics.undoEntryDetails.length === 0 ? (
              <p>Empty</p>
            ) : (
              <ol>
                {state.diagnostics.undoEntryDetails.map(
                  (entry, index) => (
                    <li key={`${entry}-${index}`}>
                      {entry}
                    </li>
                  ),
                )}
              </ol>
            )}
          </div>

          <div>
            <h3>Redo stack</h3>
            <p className="history-order">
              Newest first
            </p>

            {state.diagnostics.redoEntryDetails.length === 0 ? (
              <p>Empty</p>
            ) : (
              <ol>
                {state.diagnostics.redoEntryDetails.map(
                  (entry, index) => (
                    <li key={`${entry}-${index}`}>
                      {entry}
                    </li>
                  ),
                )}
              </ol>
            )}
          </div>
        </div>
      </section>

      {error && (
        <p>
          Request failed: {error}
        </p>
      )}
    </main>
  )
}

export default UndoRedoDemo