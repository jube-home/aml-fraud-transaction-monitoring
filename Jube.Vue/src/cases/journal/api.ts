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
import type {
  JournalAnalytics,
  JournalRows,
  TagJob,
  TagJobPreview,
  TagOptions,
  AroundResult,
  PinList,
  SavedView,
  TrackResult,
  TagResult,
  TagSet,
  TransactionCard,
  TransactionPayload,
} from './types'

export interface JournalQuery {
  caseWorkflowGuid: string
  key: string
  value: string
  from: string
  lens?: string
  activationsOnly?: boolean
  responseElevation?: number
  limit?: number
  sections?: string
  caseId?: number
  merge?: { key: string; value: string }[]
  pinnedOnly?: boolean
  filter?: string
}

export type JournalChartKind =
  'Progression' | 'Distribution' | 'Velocity' | 'Gaps' | 'Counterparties' | 'Correlation' | 'Drift'

function queryOf(request: JournalQuery): Record<string, string | number | boolean | undefined> {
  return {
    caseWorkflowGuid: request.caseWorkflowGuid,
    key: request.key,
    value: request.value,
    from: request.from,
    lens: request.lens,
    activationsOnly: request.activationsOnly || undefined,
    responseElevation: request.responseElevation || undefined,
    limit: request.limit,
    sections: request.sections,
    caseId: request.caseId,
    merge: request.merge?.length ? JSON.stringify(request.merge) : undefined,
    pinnedOnly: request.pinnedOnly || undefined,
  }
}

export function chartUrl(
  kind: JournalChartKind,
  request: JournalQuery,
  field: string | undefined,
  dark: boolean,
): string {
  const query = new URLSearchParams()

  for (const [name, value] of Object.entries(queryOf(request))) {
    if (
      value !== undefined &&
      !['sections', 'lens', 'caseId', 'pinnedOnly', 'merge'].includes(name)
    )
      query.set(name, String(value))
  }

  if (field) query.set('field', field)
  query.set('dark', String(dark))

  return `/api/CaseKeyJournal/Chart/${kind}?${query.toString()}`
}

export function transactionChartUrl(
  kind: 'Contribution' | 'Calibration',
  workflow: string,
  id: number,
  adaptation: string,
  source: string,
  dark: boolean,
): string {
  const query = new URLSearchParams({
    caseWorkflowGuid: workflow,
    id: String(id),
    adaptation,
    source,
    dark: String(dark),
  })

  return `/api/CaseKeyJournal/Transaction/Chart/${kind}?${query.toString()}`
}

export interface SavedLayout {
  json: string | null
}

export const journalApi = {
  layout: async (workflow: string): Promise<SavedLayout | null> =>
    (await useApi<SavedLayout | undefined>(
      `/api/SessionCaseJournal/ByCaseWorkflowGuid/${workflow}`,
    )) ?? null,
  saveLayout: (workflow: string, json: string) =>
    useApi<SavedLayout>('/api/SessionCaseJournal', {
      method: 'POST',
      body: { caseWorkflowGuid: workflow, json },
    }),
  rows: (request: JournalQuery) =>
    useApi<JournalRows>('/api/CaseKeyJournal', {
      query: { ...queryOf(request), filter: request.filter },
    }),
  analytics: (request: JournalQuery) =>
    useApi<JournalAnalytics>('/api/CaseKeyJournal/Analytics', { query: queryOf(request) }),
  transaction: (workflow: string, id: number) =>
    useApi<TransactionCard>('/api/CaseKeyJournal/Transaction', {
      query: { caseWorkflowGuid: workflow, id },
    }),
  payload: (workflow: string, id: number) =>
    useApi<TransactionPayload>('/api/CaseKeyJournal/Transaction/Payload', {
      query: { caseWorkflowGuid: workflow, id },
    }),
  around: (workflow: string, id: number) =>
    useApi<AroundResult>('/api/CaseKeyJournal/Around', {
      query: { caseWorkflowGuid: workflow, id },
    }),
  pins: (caseId: number) => useApi<PinList>('/api/CaseKeyJournal/Pins', { query: { caseId } }),
  setPins: (caseId: number, transactions: string[], pinned: boolean) =>
    useApi<PinList>('/api/CaseKeyJournal/Pins', {
      method: 'PUT',
      body: { caseId, transactions, pinned },
    }),
  track: (
    request: JournalQuery & {
      limitKmh?: number
      minimumKilometres?: number
      includeUnmatched?: boolean
    },
  ) =>
    useApi<TrackResult>('/api/CaseKeyJournal/Track', {
      query: {
        caseWorkflowGuid: request.caseWorkflowGuid,
        key: request.key,
        value: request.value,
        from: request.from,
        merge: request.merge?.length ? JSON.stringify(request.merge) : undefined,
        filter: request.filter,
        caseId: request.caseId,
        limitKmh: request.limitKmh,
        minimumKilometres: request.minimumKilometres,
        includeUnmatched: request.includeUnmatched || undefined,
      },
    }),
  replays: (caseId: number) =>
    useApi<{ replays: SavedView[] }>('/api/CaseKeyJournal/Replays', { query: { caseId } }),
  saveReplay: (body: {
    caseId: number
    name: string
    days: number
    merge?: string
    filter?: string
    limitKmh?: number
    minimumKilometres?: number
    impossibleOnly?: boolean
    showAll?: boolean
    lens?: string
  }) => useApi<SavedView>('/api/CaseKeyJournal/Replays', { method: 'POST', body }),
  deleteReplay: (guid: string) =>
    useApi<void>(`/api/CaseKeyJournal/Replays/${guid}`, { method: 'DELETE' }),
  tagJobPreview: (request: JournalQuery) =>
    useApi<TagJobPreview>('/api/CaseKeyJournal/TagJobs/Preview', {
      query: {
        caseWorkflowGuid: request.caseWorkflowGuid,
        key: request.key,
        value: request.value,
        from: request.from,
        activationsOnly: request.activationsOnly || undefined,
        merge: request.merge?.length ? JSON.stringify(request.merge) : undefined,
        filter: request.filter,
      },
    }),
  tagJobSubmit: (request: JournalQuery, add: string[], remove: string[]) =>
    useApi<TagJob>('/api/CaseKeyJournal/TagJobs', {
      method: 'POST',
      body: {
        caseWorkflowGuid: request.caseWorkflowGuid,
        key: request.key,
        value: request.value,
        from: request.from,
        activationsOnly: request.activationsOnly || undefined,
        merge: request.merge?.length ? JSON.stringify(request.merge) : undefined,
        filter: request.filter,
        add,
        remove,
      },
    }),
  tagJobs: (workflow: string) =>
    useApi<TagJob[]>('/api/CaseKeyJournal/TagJobs', { query: { caseWorkflowGuid: workflow } }),
  tagJob: (guid: string) => useApi<TagJob>(`/api/CaseKeyJournal/TagJobs/${guid}`),
  tagJobCancel: (guid: string) =>
    useApi<TagJob>(`/api/CaseKeyJournal/TagJobs/${guid}/Cancel`, { method: 'POST' }),
  tagJobUndo: (guid: string) =>
    useApi<TagJob>(`/api/CaseKeyJournal/TagJobs/${guid}/Undo`, { method: 'POST' }),
  tagOptions: (workflow: string) =>
    useApi<TagOptions>('/api/CaseKeyJournal/Tags', { query: { caseWorkflowGuid: workflow } }),
  tag: (workflow: string, transactions: string[], add: string[], remove: string[]) =>
    useApi<TagResult>('/api/CaseKeyJournal/Tags', {
      method: 'PUT',
      body: { caseWorkflowGuid: workflow, transactions, add, remove },
    }),
  restore: (workflow: string, sets: TagSet[]) =>
    useApi<TagResult>('/api/CaseKeyJournal/Tags/Restore', {
      method: 'PUT',
      body: { caseWorkflowGuid: workflow, sets },
    }),
}
