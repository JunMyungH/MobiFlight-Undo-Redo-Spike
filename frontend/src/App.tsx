import { useEffect, useState } from 'react'
import './App.css'

import type { SpikeState } from './types'

const API_URL = 'http://localhost:5094'

type HttpMethod = 'GET' | 'POST' | 'DELETE'

async function fetchSpikeState(
  path: string,
  method: HttpMethod = 'GET',
): Promise<SpikeState> {
  const response = await fetch(`${API_URL}${path}`, {
    method,
  })

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`)
  }

  return response.json()
}


function App() {
  const [state, setState] = useState<SpikeState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const request = async (
    path: string,
    method: HttpMethod = 'GET',
  ) => {
    try {
      setError(null)
      const response = await fetch(`${API_URL}${path}`, {
        method,
      })

      if(!response.ok){
        throw new Error(`HTTP ${response.status}`)
      }

      const data: SpikeState = await response.json()
      setState(data)
    } catch (err: unknown){
      setError(
        err instanceof Error
          ? err.message
          : 'Unknown error',
      )
    }
  }

  useEffect(() => {
    let cancelled = false

    const loadInitialState = async () => {
      try {
        const data = await fetchSpikeState('/api/state')

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

    void loadInitialState()

    return () => {
      cancelled = true
    }
  }, [])

  if (error) {
    return <div>Request failed: {error}</div>
  }

  if (!state) {
    return <div>Loading...</div>
  }

  return (
    <main>
      <h1>MobiFlight Undo/Redo Spike</h1>

      <div>
        <button
          disabled={!state.canUndo}
          onClick={() => 
            request('/api/history/undo', 'POST')
          }
        >
          Undo
        </button>

        <button
          disabled={!state.canRedo}
          onClick={() => 
            request('/api/history/redo', 'POST')
          }
        >
          Redo
        </button>
      </div>

      <h2>
        Config Items
      </h2>

      <ul>
        {state.projectState.configItems.map((item) => (
          <li key={item.id}>
            <input
              type="checkbox"
              checked={item.active}
              onChange={() =>
                request(
                  `/api/config-items/${item.id}/toggle`,
                  'POST',
                )
              }
            />
            {' '}
            {item.name}
            {' '}
            <button
              onClick={() =>
                request(
                  `/api/config-items/${item.id}`,
                  'DELETE',
                )
              }
            >
              Delete
            </button>

          </li>
        ))}
      </ul>
    </main>
  )
}

export default App