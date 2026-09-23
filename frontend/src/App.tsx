import './App.css'
import {
  Navigate,
  Route,
  Routes,
} from 'react-router-dom'

import SpikeLayout from './layout/SpikeLayout'
import CommandPage from './pages/CommandPage'
import SnapshotPage from './pages/SnapshotPage'
import PatchPage from './pages/PatchPage'
import HybridPage from './pages/HybridPage'

function App() {
  return (
    <Routes>
      <Route element={<SpikeLayout />}>
        <Route
          path="/"
          element={<Navigate to="/command" replace />}
        />

        <Route
          path="/command"
          element={<CommandPage />}
        />

        <Route
          path="/snapshot"
          element={<SnapshotPage />}
        />

        <Route
          path="/patch"
          element={<PatchPage />}
        />

        <Route
          path="/hybrid"
          element={<HybridPage />}
        />  
      </Route>
    </Routes>
  )
}

export default App