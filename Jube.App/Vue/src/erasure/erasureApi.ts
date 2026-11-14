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

export interface ErasureRequest {
  guid: string
  keyName: string
  keyValue: string
  reason: string
  stateId: number
  refusalReasonId: number | null
  archiveRowsFound: number | null
  archiveRowsRemoved: number | null
  graphNodesRemoved: number | null
  graphEdgesRemoved: number | null
  casePayloadsNulled: number | null
  requestedUser: string
  requestedDate: string
  decidedUser: string | null
  decidedDate: string | null
  decisionNote: string | null
  executedDate: string | null
}

export interface ErasurePreview {
  archiveRows: number
  refusalReasonId: number | null
}

export interface SubjectExport {
  keyName: string
  keyValue: string
  generatedDate: string
  generatedUser: string
  truncated: boolean
  transactions: unknown[]
}

export const erasureState = {
  pending: 1,
  approved: 2,
  executed: 3,
  rejected: 4,
  retracted: 5,
  refused: 6,
} as const

export const erasurePermission = {request: 78, approve: 79} as const

export const erasureKey = ['service', 'Erasure', 'list'] as const

export const maximumReason = 1000
export const maximumKeyName = 200
export const maximumKeyValue = 500

const base = '/api/Erasure'

function post<T>(path: string, body?: unknown) {
  return useApi<T>(`${base}/${path}`, {method: 'POST', body})
}

export const erasureApi = {
  list: () => useApi<ErasureRequest[]>(base),
  preview: (keyName: string, keyValue: string) =>
    post<ErasurePreview>('Preview', {keyName, keyValue}),
  request: (keyName: string, keyValue: string, reason: string) =>
    post<ErasureRequest>('Request', {keyName, keyValue, reason}),
  approve: (guid: string, note: string | null) => post<ErasureRequest>('Approve', {guid, note}),
  reject: (guid: string, note: string) => post<ErasureRequest>('Reject', {guid, note}),
  retract: (guid: string) => post<ErasureRequest>(`Retract/${guid}`),
  export: (keyName: string, keyValue: string) =>
    post<SubjectExport>('Export', {keyName, keyValue}),
}

export function canDecide(request: ErasureRequest, user: string | null): boolean {
  return request.stateId === erasureState.pending && request.requestedUser !== user
}

export function canRetract(request: ErasureRequest, user: string | null): boolean {
  return request.stateId === erasureState.pending && request.requestedUser === user
}

export function subjectReady(keyName: string, keyValue: string): boolean {
  return (
    keyName.trim().length > 0 &&
    keyName.trim().length <= maximumKeyName &&
    keyValue.trim().length > 0 &&
    keyValue.trim().length <= maximumKeyValue
  )
}
