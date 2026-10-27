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

export const diaryApi = {
  set: (caseId: number, diaryTypeId: number, dueDate: string) =>
    useApi('/api/CaseDiary/Set', { method: 'POST', body: { caseId, diaryTypeId, dueDate } }),
  clear: (caseId: number) => useApi('/api/CaseDiary/Clear', { method: 'POST', body: { caseId } }),
}

export function localInputToIso(value: string): string | null {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) return null

  const parsed = new Date(`${value}:00Z`)

  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString()
}

export function isoToLocalInput(value: string | null): string {
  if (!value) return ''

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime()) ? '' : parsed.toISOString().slice(0, 16)
}
