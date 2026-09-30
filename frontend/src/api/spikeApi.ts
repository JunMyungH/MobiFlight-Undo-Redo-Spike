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

export function undo(approach: Approach): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/history/undo',
  )
}

export function redo(approach: Approach): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/history/redo',
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

export function compoundEdit(
  approach: Approach,
) {
  return request(
    approach,
    '/experiment/compound-edit',
    'POST',
  )
}

export async function runBulkToggle(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/bulk-toggle',
  )
}

export async function runBulkDelete(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/bulk-delete',
  )

}

async function requestExperiment(
  approach: Approach,
  path: string,
  method: HttpMethod = 'POST',
): Promise<ExperimentResponse> {
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

export function duplicateFirstItem(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/duplicate-first',
  )
}

export function moveFirstToLast(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/move-first-to-last',
  )
}

export function runBulkToggleReference(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/bulk-toggle-reference',
  )
}

export function runBulkToggleIndexed(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/bulk-toggle-indexed',
  )
}

export function runBulkDeleteIndexed(
  approach: Approach,
): Promise<ExperimentResponse> {
  return requestExperiment(
    approach,
    '/experiment/bulk-delete-indexed',
  )
}