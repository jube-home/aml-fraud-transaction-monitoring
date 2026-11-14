/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

import {useApi} from '../api/useApi'

export interface QueueDepth {
  pending: number
  processing: number
  dead: number
}

export interface DeadRow {
  id: number
  entityAnalysisModelGuid: string
  attempts: number
  lastError: string | null
  enqueuedBy: string
  createdDate: string
}

export const queueKey = ['service', 'InboundQueue'] as const

const base = '/api/InboundQueue'

export const queueApi = {
  depth: () => useApi<QueueDepth>(`${base}/Depth`),
  dead: () => useApi<DeadRow[]>(`${base}/Dead`),
  requeue: (id: number) => useApi<unknown>(`${base}/Dead/${id}/Requeue`, {method: 'POST'}),
  remove: (id: number) => useApi<unknown>(`${base}/Dead/${id}`, {method: 'DELETE'}),
}

export function backlog(depth: QueueDepth | undefined): number {
  return depth ? depth.pending + depth.processing : 0
}
