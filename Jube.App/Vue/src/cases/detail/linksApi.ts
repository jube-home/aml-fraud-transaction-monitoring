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

export interface CaseLink {
  otherCaseId: number
  caseKey: string | null
  caseKeyValue: string | null
  closedStatusId: number
  reason: string | null
  createdUser: string
  createdDate: string
}

export const linksKey = (caseId: number) => ['service', 'CaseLink', 'byCase', caseId] as const

export const maximumReason = 500

export const linksApi = {
  list: (caseId: number) => useApi<CaseLink[]>(`/api/CaseLink/ByCaseId/${caseId}`),
  link: (caseId: number, otherCaseId: number, reason: string | null) =>
    useApi('/api/CaseLink/Link', {method: 'POST', body: {caseId, otherCaseId, reason}}),
  unlink: (caseId: number, otherCaseId: number) =>
    useApi('/api/CaseLink/Unlink', {method: 'POST', body: {caseId, otherCaseId, reason: null}}),
}

export function parseCaseNumber(text: string): number | null {
  const cleaned = text.trim().replace(/^(#|c-)/i, '')

  if (!/^\d{1,9}$/.test(cleaned)) return null

  const value = Number(cleaned)

  return value > 0 ? value : null
}
