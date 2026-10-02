import {
  NavLink,
  Outlet,
} from 'react-router-dom'

const navigationItems = [
  {
    to: '/command',
    label: 'Command Based',
    short: 'C',
    description: 'Semantic commands',
  },
  {
    to: '/snapshot',
    label: 'Snapshot Based',
    short: 'S',
    description: 'State copies',
  },
  {
    to: '/patch',
    label: 'Patch Based',
    short: 'P',
    description: 'Localized changes',
  },
  {
    to: '/hybrid',
    label: 'Hybrid',
    short: 'H',
    description: 'Patch + snapshot',
  },
]

function SpikeLayout() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-200 lg:grid lg:grid-cols-[280px_minmax(0,1fr)]">
      <aside className="hidden h-screen flex-col border-r border-white/10 bg-slate-950/95 p-5 lg:sticky lg:top-0 lg:flex">
        <div className="mb-8">
          <div className="mb-4 flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-cyan-400 text-lg font-bold text-white shadow-lg shadow-indigo-500/20">
            U/R
          </div>

          <h1 className="text-lg font-semibold tracking-tight text-white">
            Undo / Redo Spike
          </h1>

          <p className="mt-1 text-xs leading-relaxed text-slate-500">
            Architecture experiment
          </p>
        </div>

        <nav className="flex flex-col gap-1.5">
          {navigationItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                [
                  'group flex items-center gap-3 rounded-xl px-3 py-3 transition-all',
                  isActive
                    ? 'bg-indigo-500/10 text-white ring-1 ring-inset ring-indigo-400/20'
                    : 'text-slate-400 hover:bg-white/5 hover:text-slate-200',
                ].join(' ')
              }
            >
              {({ isActive }) => (
                <>
                  <div
                    className={[
                      'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-xs font-bold transition-colors',
                      isActive
                        ? 'bg-indigo-500 text-white shadow-md shadow-indigo-500/25'
                        : 'bg-slate-900 text-slate-500 group-hover:bg-slate-800 group-hover:text-slate-300',
                    ].join(' ')}
                  >
                    {item.short}
                  </div>

                  <div className="min-w-0">
                    <div className="text-sm font-medium">
                      {item.label}
                    </div>

                    <div className="mt-0.5 text-[11px] text-slate-500">
                      {item.description}
                    </div>
                  </div>
                </>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto border-t border-white/10 pt-5">
          <p className="text-[11px] leading-relaxed text-slate-600">
            MobiFlight
          </p>

          <p className="text-xs text-slate-500">
            Undo / Redo architecture spike
          </p>
        </div>
      </aside>

      <div className="min-w-0">
        <div className="border-b border-white/10 bg-slate-950/90 p-3 backdrop-blur lg:hidden">
          <div className="mb-3 flex items-center gap-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-indigo-500 to-cyan-400 text-xs font-bold text-white">
              U/R
            </div>

            <div>
              <div className="text-sm font-semibold text-white">
                Undo / Redo Spike
              </div>

              <div className="text-[11px] text-slate-500">
                Architecture experiment
              </div>
            </div>
          </div>

          <nav className="flex gap-2 overflow-x-auto">
            {navigationItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  [
                    'whitespace-nowrap rounded-lg px-3 py-2 text-xs font-medium transition-colors',
                    isActive
                      ? 'bg-indigo-500 text-white'
                      : 'bg-slate-900 text-slate-400',
                  ].join(' ')
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>
        </div>

        <div className="mx-auto w-full max-w-[1500px] p-5 md:p-8 xl:p-10">
          <Outlet />
        </div>
      </div>
    </div>
  )
}

export default SpikeLayout