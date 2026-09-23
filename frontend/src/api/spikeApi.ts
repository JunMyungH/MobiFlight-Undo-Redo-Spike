import type { ExperimentResponse, SpikeState } from '../types'

const API_URL = 'http://localhost:5094'

export type Approach =
  | 'command'
  | 'snapshot'
  | 'patch'
  | 'hybrid'

type HttpMethod =
  | 'GET'
  | 'POST'
  | 'DELETE'

async function request(
  approach: Approach,
  path: string,
  method: HttpMethod = 'GET',
): Promise<SpikeState> {
  const response = await fetch(
    `${API_URL}/api/${approach}${path}`,
    {
      method,
    },
  )

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`)
  }

  return response.json()
}

export function getState(approach: Approach) {
  return request(approach, '/state')
}

export function toggleItem(
  approach: Approach,
  id: string,
) {
  return request(
    approach,
    `/config-items/${id}/toggle`,
    'POST',
  )
}

export function deleteItem(
  approach: Approach,
  id: string,
) {
  return request(
    approach,
    `/config-items/${id}`,
    'DELETE',
  )
}

export function undo(approach: Approach) {
  return request(
    approach,
    '/history/undo',
    'POST',
  )
}

export function redo(approach: Approach) {
  return request(
    approach,
    '/history/redo',
    'POST',
  )
}

export function resetExperiment(
  approach: Approach,
  itemCount: number,
) {
  return request(
    approach,
    `/experiment/reset/${itemCount}`,
    'POST',
  )
}

export async function runToggleExperiment(
  approach: Approach,
  count: number,
): Promise<ExperimentResponse> {
  const response = await fetch(
    `${API_URL}/api/${approach}/experiment/toggles/${count}`,
    {
      method: 'POST',
    },
  )

  if (!response.ok) {
    throw new Error(`HTTP ${response.status}`)
  }

  return response.json()
}