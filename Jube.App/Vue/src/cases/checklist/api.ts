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
import type {ChecklistHistoryEntry, ChecklistOverride, ChecklistView} from './types'

export const checklistKeys = {
  all: ['service', 'CaseChecklist'] as const,
  byCase: (caseId: number) => ['service', 'CaseChecklist', 'byCase', caseId] as const,
  history: (caseId: number) => ['service', 'CaseChecklist', 'history', caseId] as const,
}

export const checklistApi = {
  get: (caseId: number) => useApi<ChecklistView>(`/api/CaseChecklist/${caseId}`),
  history: (caseId: number) =>
    useApi<ChecklistHistoryEntry[]>(`/api/CaseChecklist/${caseId}/History`),
  check: (caseId: number, itemId: number) =>
    useApi<ChecklistView>(`/api/CaseChecklist/${caseId}/Check/${itemId}`, {method: 'POST'}),
  uncheck: (caseId: number, itemId: number) =>
    useApi<ChecklistView>(`/api/CaseChecklist/${caseId}/Uncheck/${itemId}`, {method: 'POST'}),
  override: (caseId: number, reason: string) =>
    useApi<ChecklistOverride>(`/api/CaseChecklist/${caseId}/Override`, {
      method: 'POST',
      body: {reason},
    }),
}
