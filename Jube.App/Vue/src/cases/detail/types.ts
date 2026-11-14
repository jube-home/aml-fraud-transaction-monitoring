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

export interface CaseFieldEntry {
  name: string | null
  value: string | null
  cellFormatForeColor: string | null
  cellFormatForeRow: boolean
  cellFormatBackColor: string | null
  cellFormatBackRow: boolean
  existsMatch: boolean
  conditionalRegularExpressionFormatting: boolean
}

export interface CaseDetail {
  id: number
  entityAnalysisModelInstanceEntryGuid: string
  diaryDate: string
  caseWorkflowGuid: string
  caseWorkflowStatusGuid: string
  createdDate: string
  locked: boolean
  lockedUser: string | null
  lockedDate: string
  closedStatusId: number
  closedDate: string
  closedUser: string | null
  caseKey: string | null
  diary: boolean
  diaryUser: string | null
  rating: number
  caseKeyValue: string | null
  foreColor: string | null
  backColor: string | null
  formattedPayload: CaseFieldEntry[] | null
  activation: { name: string | null }[] | null
  enableVisualisation: boolean
  visualisationRegistryGuid: string | null
  entityAnalysisModelId: number
}

export interface CaseWorkflowSettings {
  guid: string
  enableGraph: boolean
  graphRegistryGuid: string | null
}

export interface CaseStatusChoice {
  guid: string
  name: string | null
  priority?: number
  escalationTargetCaseWorkflowStatusGuid?: string | null
}

export interface CaseKeyCase {
  id: number
  caseWorkflowStatusName: string | null
  createdDate: string
  locked: boolean
  lockedUser: string | null
  closedStatusId: number
  closedDate: string
  closedUser: string | null
  caseKey: string | null
  caseKeyValue: string | null
}

export interface CaseNoteEntry {
  id: number
  caseId: number
  createdDate: string
  createdUser: string | null
  note: string | null
  action: string | null
  priority: string | null
  sourceId?: number
  kindId?: number
  isFinding?: boolean
  version?: number
  supersedesNoteId?: number | null
  files?: CaseFileEntry[]
  askJoobyConversationId?: number | null
}

export interface CaseFileEntry {
  id: number
  caseNoteId: number | null
  caseResearchId: number | null
  caseId: number
  createdDate: string | null
  createdUser: string | null
  name: string | null
  extension: string | null
  contentType: string | null
  size: number
}

export interface CaseEventEntry {
  id: number
  caseId: number
  caseEventTypeId: number
  caseEventType: string | null
  createdDate: string
  createdUser: string | null
  before: string | null
  after: string | null
}

export interface CaseJournal {
  schema: Record<string, string> | null
  rows: Record<string, unknown>[] | null
}

export interface JournalFilter {
  limit: number
  responseElevation: number
  activationsOnly: boolean
}

export interface CaseResearchSource {
  id: number
  title: string | null
  url: string
  retrievedDate: string | null
}

export interface CaseResearchEntry {
  id: number
  caseId: number
  topic: string
  findings: string
  sourceId: number
  sources: CaseResearchSource[]
  files: CaseFileEntry[]
  createdUser: string | null
  createdDate: string | null
}

export interface CaseTag {
  id: number
  caseId: number
  name: string | null
  createdUser: string | null
  createdDate: string | null
}
