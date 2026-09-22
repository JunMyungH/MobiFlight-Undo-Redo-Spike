import { useState } from 'react'
import './App.css'

import { useEffect } from 'react'
import type { ProjectState } from './types'

function App() {
  const [projectState, setProjectState] = useState<ProjectState | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('http://localhost:5094/api/state')
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`)
        }

        return response.json()
      })
      .then((data: ProjectState) => {
        setProjectState(data)
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Unknown error')
      })
  }, [])

  if (error) {
    return <div>Failed to load state: {error}</div>
  }

  if (!projectState) {
    return <div>Loading...</div>
  }

  return (
    <main>
      <h1>MobiFlight Undo/Redo Spike</h1>

      <h2>Config Items</h2>

      <ul>
        {projectState.configItems.map((item) => (
          <li key={item.id}>
            <input type="checkbox" checked={item.active} readOnly />
            {' '}
            {item.name}
          </li>
        ))}
      </ul>
    </main>
  )
}

export default App