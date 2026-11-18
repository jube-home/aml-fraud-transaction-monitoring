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

export interface BulkItem {
  caseId: number
  done: boolean
  skipReasonId: number
}

export interface BulkResult {
  items: BulkItem[]
  done: number
  skipped: number
}

export const bulkLimit = 500
export const bulkReassignPermission = 18

export const bulkApi = {
  close: (caseIds: number[], closedReasonId: number, closedReasonNote: string | null) =>
    useApi<BulkResult>('/api/CaseBulk/Close', {
      method: 'POST',
      body: {caseIds, closedReasonId, closedReasonNote},
    }),
  reassign: (caseIds: number[], toUser: string) =>
    useApi<BulkResult>('/api/CaseBulk/Reassign', {method: 'POST', body: {caseIds, toUser}}),
}

export function skipSummary(result: BulkResult): Map<number, number> {
  const counts = new Map<number, number>()

  for (const item of result.items) {
    if (!item.done) counts.set(item.skipReasonId, (counts.get(item.skipReasonId) ?? 0) + 1)
  }

  return counts
}
