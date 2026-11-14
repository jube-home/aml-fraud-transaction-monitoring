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
import type {
  CaseDetail,
  CaseEventEntry,
  CaseJournal,
  CaseKeyCase,
  CaseResearchEntry,
  CaseNoteEntry,
  CaseStatusChoice,
  CaseTag,
  CaseWorkflowSettings,
  JournalFilter,
} from './types'

export const caseDetailApi = {
  byId: (caseId: number) => useApi<CaseDetail>(`/api/GetCaseByIdQuery/${caseId}`),
  tags: (caseId: number) => useApi<CaseTag[]>(`/api/CaseTag/ByCaseId/${caseId}`),
  tagChoices: (caseId: number) => useApi<string[]>(`/api/CaseTag/Choices/${caseId}`),
  mergeTags: (caseId: number, tags: string[]) =>
    useApi<CaseTag[]>('/api/CaseTag/Merge', {method: 'POST', body: {caseId, tags}}),
  statuses: (caseWorkflowGuid: string) =>
    useApi<CaseStatusChoice[]>(
      `/api/CaseWorkflowStatus/ByCaseWorkflowGuidActiveOnly/${caseWorkflowGuid}`,
    ),
  workflowSettings: (entityAnalysisModelId: number) =>
    useApi<CaseWorkflowSettings[]>('/api/CaseWorkflow/ByEntityAnalysisModelIdActiveOnly', {
      query: {id: entityAnalysisModelId},
    }),
  byKey: (key: string, value: string) =>
    useApi<CaseKeyCase[]>('/api/GetCaseByCaseKeyValueQuery', {query: {key, value}}),
  notes: (key: string, value: string) =>
    useApi<CaseNoteEntry[]>('/api/GetCaseNoteByCaseKeyValueQuery', {query: {key, value}}),
  research: (key: string, value: string) =>
    useApi<CaseResearchEntry[]>('/api/CaseResearch/ByCaseKeyValue', {query: {key, value}}),
  events: (key: string, value: string) =>
    useApi<CaseEventEntry[]>('/api/GetCaseEventByCaseKeyValueQuery', {query: {key, value}}),
  journal: (
    caseWorkflowGuid: string,
    drillName: string,
    drillValue: string,
    filter: JournalFilter,
  ) =>
    useApi<CaseJournal>('/api/GetCaseJournalQuery', {
      query: {
        caseWorkflowGuid,
        drillName,
        drillValue,
        limit: filter.limit,
        activationsOnly: filter.activationsOnly,
        responseElevation: filter.responseElevation,
      },
    }),
}
