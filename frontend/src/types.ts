export type ConfigItem = {
  id: string
  name: string
  active: boolean
}

export type ProjectState = {
  configItems: ConfigItem[]
}

export type SpikeState = {
  projectState: ProjectState
  canUndo: boolean
  canRedo: boolean
}