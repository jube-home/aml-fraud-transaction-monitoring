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

import { useApi } from '../../api/useApi'

export interface CaseRecord {
  id: number
  caseWorkflowStatusGuid: string
  locked: boolean
  lockedUser: string | null
  closedStatusId: number
  rating: number
  diary: boolean
  diaryDate: string | null
  [field: string]: unknown
}

export interface Choice {
  id: number
  name: string | null
  description?: string | null
}

export interface MacroChoice extends Choice {
  imageLocation?: string | null
}

export interface NoteRequest {
  note: string
  actionId: number
  priorityId: number
  caseKey: string
  caseKeyValue: string
  caseId: number
}

export interface FormField {
  name: string
  kind: string
  options: string[]
}

export interface FormRender {
  html: string
  visibleFields: string[]
  fields: FormField[]
}

export interface FormEntryView {
  id: number
  caseId: number
  formName: string
  createdUser: string
  createdDate: string | null
  html: string
  otherValues: { name: string; value: string | null }[]
  error: string | null
}

export interface DispatchView {
  id: number
  caseId: number
  sourceKindId: number
  sourceName: string
  channelId: number
  modeId: number
  destination: string | null
  subject: string | null
  body: string | null
  outcomeId: number
  detail: string | null
  responseStatus: number | null
  correlationId: string
  rows: string | null
  createdUser: string
  createdDate: string | null
}

export interface HoldLeg {
  kindId: number
  outcomeId: number
  date: string
  destination: string | null
  httpStatus: number | null
  detail: string | null
}

export interface HoldDecision {
  actor: string | null
  date: string
  reason: string | null
  releaseHookId: number | null
  releaseHookName: string | null
  httpStatus: number | null
}

export interface HoldView {
  entityAnalysisModelInstanceEntryGuid: string
  entryKeyValue: string | null
  referenceDate: string | null
  stateId: number
  rangeName: string | null
  rangeMinimum: number
  rangeMaximum: number
  elevation: number
  stampedDate: string
  deadlineDate: string | null
  decision: HoldDecision | null
  legs: HoldLeg[]
  releaseHookName: string | null
  refuseCallsHook: boolean
}

export interface HoldResult {
  stateId: number
  releaseHookName: string | null
  httpStatus: number | null
  version: number
}

export const holdState = { held: 1, declined: 2, released: 3, refused: 4 } as const

export interface FormSubmission {
  caseWorkflowFormId: number
  caseId: number
  caseKey: string
  caseKeyValue: string
  payload: Record<string, string>
}

export const closedStatus = { open: 0, suspended: 1, reopened: 2, closed: 3 } as const

export const operationsApi = {
  change: async (caseId: number, patch: Partial<CaseRecord>) => {
    const current = await useApi<CaseRecord>(`/api/Case/${caseId}`)

    return useApi<CaseRecord>('/api/Case', { method: 'PUT', body: { ...current, ...patch } })
  },
  actions: (workflow: string) =>
    useApi<Choice[]>(`/api/CaseWorkflowAction/ByCaseWorkflowGuidActiveOnly/${workflow}`),
  priorities: (workflow: string) =>
    useApi<Choice[]>(`/api/CaseWorkflowPriority/ByCaseWorkflowGuidActiveOnly/${workflow}`),
  macros: (workflow: string) =>
    useApi<MacroChoice[]>(`/api/CaseWorkflowMacro/ByCaseWorkflowGuidActiveOnly/${workflow}`),
  runMacro: (caseId: number, caseWorkflowMacroId: number) =>
    useApi<unknown>('/api/CaseWorkflowMacroExecution', {
      method: 'POST',
      body: { caseId, caseWorkflowMacroId },
    }),
  displays: (workflow: string) =>
    useApi<Choice[]>(`/api/CaseWorkflowDisplay/ByCaseWorkflowGuidActiveOnly/${workflow}`),
  runDisplay: (caseId: number, caseWorkflowDisplayId: number) =>
    useApi<string>('/api/CaseWorkflowDisplayExecution', {
      method: 'POST',
      body: { caseId, caseWorkflowDisplayId },
    }),
  forms: (workflow: string) =>
    useApi<Choice[]>(`/api/CaseWorkflowForm/ByCaseWorkflowGuidActiveOnly/${workflow}`),
  renderForm: (
    caseId: number,
    caseWorkflowFormId: number,
    values: Record<string, string>,
    signal?: AbortSignal,
  ) =>
    useApi<FormRender>('/api/CaseWorkflowDisplayExecution/Form', {
      method: 'POST',
      body: { caseId, caseWorkflowFormId, values, readOnly: false },
      signal,
    }),
  formEntries: (caseId: number) =>
    useApi<FormEntryView[]>('/api/CaseWorkflowDisplayExecution/FormEntries', {
      method: 'POST',
      body: { caseId },
    }),
  dispatches: (caseId: number) =>
    useApi<DispatchView[]>('/api/CaseWorkflowDisplayExecution/Dispatches', {
      method: 'POST',
      body: { caseId },
    }),
  holds: (caseId: number) => useApi<HoldView[]>(`/api/CaseHold/ByCaseId/${caseId}`),
  releaseHold: (caseId: number, entityAnalysisModelInstanceEntryGuid: string, reason: string) =>
    useApi<HoldResult>('/api/CaseHold/Release', {
      method: 'POST',
      body: { caseId, entityAnalysisModelInstanceEntryGuid, reason },
    }),
  refuseHold: (caseId: number, entityAnalysisModelInstanceEntryGuid: string, reason: string) =>
    useApi<HoldResult>('/api/CaseHold/Refuse', {
      method: 'POST',
      body: { caseId, entityAnalysisModelInstanceEntryGuid, reason },
    }),
  submitForm: (request: FormSubmission) =>
    useApi<unknown>('/api/CaseWorkflowFormEntry', { method: 'POST', body: request }),
  addNote: (request: NoteRequest) =>
    useApi<unknown>('/api/CaseNote', { method: 'POST', body: request }),
}
