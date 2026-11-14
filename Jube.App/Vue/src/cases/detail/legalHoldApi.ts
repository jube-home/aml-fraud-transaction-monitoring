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

import {useApi} from '../../api/useApi'

export interface LegalHoldState {
  hold: boolean
  reason: string | null
  user: string | null
  date: string | null
}

export const legalHoldPermission = 18
export const maximumHoldReason = 500

export const legalHoldKey = (caseId: number) =>
  ['service', 'CaseSupervision', 'legalHold', caseId] as const

const base = '/api/CaseSupervision/LegalHold'

export const legalHoldApi = {
  get: (caseId: number) => useApi<LegalHoldState>(`${base}/${caseId}`),
  set: (caseId: number, hold: boolean, reason: string | null) =>
    useApi<void>(base, {method: 'POST', body: {caseId, hold, reason}}),
}

export function holdReady(hold: boolean, reason: string): boolean {
  return !hold || (reason.trim().length > 0 && reason.trim().length <= maximumHoldReason)
}
