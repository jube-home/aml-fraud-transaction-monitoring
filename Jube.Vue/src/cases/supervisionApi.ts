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

import { useApi } from '../api/useApi'

export interface AnalystWorkload {
  user: string
  holding: number
  oldestHoldMinutes: number | null
  taken: number
  peeks: number
  closed: number
  medianResolutionMinutes: number | null
}

export interface Supervision {
  caseWorkflowGuid: string
  from: string
  to: string
  unassigned: number
  held: number
  analysts: AnalystWorkload[]
}

export interface StuckLock {
  caseId: number
  caseKey: string | null
  caseKeyValue: string | null
  statusName: string | null
  lockedUser: string | null
  lockedDate: string | null
  lastActivityDate: string
  idleMinutes: number
  priority: number
  lane: number
}

export const idleChoices = [60, 120, 240, 480, 1440] as const

export const defaultIdle = 120

export function idleOf(value: unknown): number {
  const single = Array.isArray(value) ? value[0] : value
  const parsed = Number(single)

  return (idleChoices as readonly number[]).includes(parsed) ? parsed : defaultIdle
}

export const supervisionApi = {
  workload: (caseWorkflowGuid: string, from: string) =>
    useApi<Supervision>('/api/CaseSupervision', { query: { caseWorkflowGuid, from } }),
  stuck: (caseWorkflowGuid: string, idleMinutes: number) =>
    useApi<StuckLock[]>('/api/CaseSupervision/StuckLocks', {
      query: { caseWorkflowGuid, idleMinutes },
    }),
  breakLock: (caseId: number, reason: string) =>
    useApi('/api/CaseSupervision/BreakLock', {
      method: 'POST',
      body: { caseId, reason: reason.trim() === '' ? null : reason.trim() },
    }),
  reassign: (caseId: number, toUser: string) =>
    useApi('/api/CaseSupervision/Reassign', { method: 'POST', body: { caseId, toUser } }),
}
