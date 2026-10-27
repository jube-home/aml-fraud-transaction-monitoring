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
import type {
  CaseAnalyticsSummary,
  CaseCount,
  CaseMatrix,
  CaseQaSample,
  CasePage,
  CaseSearchRequest,
  CaseSkim,
  CaseSummary,
  CaseWorkflowChoice,
} from './types'

function queryOf(
  request: CaseSearchRequest,
): Record<string, string | number | boolean | undefined> {
  return { ...request }
}

export const casesApi = {
  workflows: () => useApi<CaseWorkflowChoice[]>('/api/CaseSearch/Workflows'),
  counts: (caseWorkflowGuid: string) =>
    useApi<CaseCount[]>(`/api/CaseSearch/Counts/${caseWorkflowGuid}`),
  page: (request: CaseSearchRequest) =>
    useApi<CasePage>('/api/CaseSearch', { query: queryOf(request) }),
  matrix: (request: CaseSearchRequest) =>
    useApi<CaseMatrix>('/api/CaseSearch/RiskMatrix', { query: queryOf(request) }),
  skim: (request: CaseSearchRequest) =>
    useApi<CaseSkim>('/api/CaseSearch/Skim', { method: 'POST', body: request }),
  summary: (caseId: number) => useApi<CaseSummary>(`/api/CaseSummary/${caseId}`),
  qaSample: (caseWorkflowGuid: string, from: string) =>
    useApi<{ qaSample: CaseQaSample }>('/api/CaseAnalytics', { query: { caseWorkflowGuid, from } }),
  analyticsSince: (caseWorkflowGuid: string, from: string) =>
    useApi<CaseAnalyticsSummary>('/api/CaseAnalytics', { query: { caseWorkflowGuid, from } }),
  analytics: (caseWorkflowGuid: string) =>
    useApi<CaseAnalyticsSummary>('/api/CaseAnalytics', { query: { caseWorkflowGuid } }),
}
