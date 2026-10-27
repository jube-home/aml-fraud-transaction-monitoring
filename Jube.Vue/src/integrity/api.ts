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
  EngineState,
  IntegrityModel,
  IntegrityReport,
  ModelGraph,
  PendingRow,
  SyncNodeStatus,
} from './types'

export interface ApprovalDecision {
  kind: number
  entityId: number
  version: number
  note: string | null
}

export interface ApproveValuesResult {
  approved: number
  refusals: string[]
}

export interface SyncSchedule {
  scheduleDate: string | null
}

export const integrityApi = {
  models: () => useApi<IntegrityModel[]>('/api/EntityAnalysisModelIntegrity/Models'),
  report: (modelId: number) =>
    useApi<IntegrityReport>(`/api/EntityAnalysisModelIntegrity/${modelId}`),
  engine: (modelId: number) =>
    useApi<EngineState>(`/api/EntityAnalysisModelEngineState/${modelId}`),
  graph: (modelId: number) =>
    useApi<ModelGraph>(`/api/EntityAnalysisModelDependencyGraph/${modelId}`),
  graphSvgPath: (modelId: number) => `/api/EntityAnalysisModelDependencyGraph/${modelId}/Svg`,
  pending: (modelId: number) =>
    useApi<PendingRow[]>('/api/EntityApproval/Pending', { query: { modelId } }),
  decide: (action: 'Approve' | 'Reject', body: ApprovalDecision) =>
    useApi(`/api/EntityApproval/${action}`, { method: 'POST', body }),
  approveValues: (kind: number, entityId: number) =>
    useApi<ApproveValuesResult>('/api/EntityApproval/ApproveValues', {
      method: 'POST',
      body: { kind, entityId },
    }),
  schedule: (modelId?: number) =>
    useApi<SyncSchedule>('/api/EntityAnalysisModelSynchronisationSchedule/ByCurrent', {
      query: modelId === undefined ? undefined : { entityAnalysisModelId: modelId },
    }),
  setSchedule: (scheduleDate: string | null, modelId?: number) =>
    useApi<SyncSchedule>('/api/EntityAnalysisModelSynchronisationSchedule', {
      method: 'POST',
      body: {
        ...(scheduleDate ? { ScheduleDate: scheduleDate } : {}),
        ...(modelId === undefined ? {} : { EntityAnalysisModelId: modelId }),
      },
    }),
  nodeStatus: () =>
    useApi<SyncNodeStatus[]>('/api/GetEntityAnalysisModelSynchronisationNodeStatusEntries'),
}
