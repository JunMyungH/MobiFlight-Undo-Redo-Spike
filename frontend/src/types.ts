export type ConfigItem = {
  id: string
  name: string
  active: boolean
}

export type ProjectState = {
  configItems: ConfigItem[]
}

export type HistoryDiagnostics = {
  approach: string
  representation: string

  undoEntries: number
  redoEntries: number

  undoEntryDetails: string[]
  redoEntryDetails: string[]

  storedConfigItemCopies: number | null
}

export type SpikeState = {
  projectState: ProjectState
  canUndo: boolean
  canRedo: boolean

  diagnostics: HistoryDiagnostics
}