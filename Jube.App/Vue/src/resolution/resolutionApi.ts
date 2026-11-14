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

export interface EntityMatch {
  id: number
  nodeIdA: number
  keyA: string
  valueA: string
  nodeIdB: number
  keyB: string
  valueB: string
  sourceId: number
  posterior: number
  evidence: string | null
  decisionId: number
  decidedUser: string | null
  decidedDate: string | null
  decisionNote: string | null
  resolvedEntityId: number | null
  createdDate: string
}

export const matchDecision = {pending: 1, confirmed: 2, rejected: 3} as const
export const resolutionKey = ['service', 'GraphEntityMatch'] as const
export const maximumNote = 500

const base = '/api/GraphEntityMatch'

export const resolutionApi = {
  list: (decisionId: number, limit = 200) =>
    useApi<EntityMatch[]>(base, {query: {decisionId, limit}}),
  decide: (id: number, decisionId: number, note: string | null) =>
    useApi<EntityMatch>(`${base}/Decide`, {method: 'POST', body: {id, decisionId, note}}),
}

export function percent(posterior: number): string {
  return `${(posterior * 100).toFixed(1)}%`
}

export function canDecide(match: EntityMatch): boolean {
  return match.decisionId === matchDecision.pending
}
