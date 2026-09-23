import { useEffect, useState } from 'react'

import {
  deleteItem,
  getState,
  redo,
  toggleItem,
  undo,
} from '../api/spikeApi'

import type { Approach } from '../api/spikeApi'
import type { SpikeState } from '../types'

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
            runAction(() => undo(approach))
          }
        >
          Undo
        </button>

        {' '}

        <button
          type="button"
          disabled={pending || !state.canRedo}
          onClick={() =>
            runAction(() => redo(approach))
          }
        >
          Redo
        </button>
      </div>

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

      {error && (
        <p>
          Request failed: {error}
        </p>
      )}
    </main>
  )
}

export default UndoRedoDemo