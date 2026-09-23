import { NavLink, Outlet } from 'react-router-dom'

function SpikeLayout() {
  return (
    <div className="spike-layout">
      <aside className="sidebar">
        <h1 className="sidebar-title">
          Undo/Redo Spike
        </h1>

        <nav className="sidebar-nav">
          <NavLink to="/command">
            Command Based
          </NavLink>

          <NavLink to="/snapshot">
            Snapshot Based
          </NavLink>

          <NavLink to="/patch">
            Patch Based
          </NavLink>

          <NavLink to="/hybrid">
            Hybrid
          </NavLink>
        </nav>
      </aside>

      <div className="spike-content">
        <Outlet />
      </div>
    </div>
  )
}

export default SpikeLayout