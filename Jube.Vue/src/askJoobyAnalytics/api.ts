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
import { valueGridApi } from '../valueGrid/api'
import type { ValueStatus } from '../valueGrid/model'
import type {
  AnalyticsInstance,
  AnalyticsNarrative,
  ModelName,
  ModelVersion,
  PromotionEvidence,
  TrainedModel,
  VersionComparison,
} from './types'

const instances = '/api/AskJoobyAnalyticsInstance'
const models = '/api/AskJoobyAnalyticsModel'
const charts = '/api/Chart/AnalyticsModelVersion'
const modelKind = 1

export const analyticsApi = {
  instances: () => useApi<AnalyticsInstance[]>(instances, { query: { includeDeleted: true } }),
  models: (instanceId: number) =>
    useApi<TrainedModel[]>(models, { query: { askJoobyAnalyticsInstanceId: instanceId } }),
  model: (id: number) => useApi<TrainedModel>(`${models}/${id}`),
  versions: (modelId: number) => useApi<ModelVersion[]>(`${models}/${modelId}/versions`),
  version: (versionId: number) => useApi<ModelVersion>(`${models}/Version/${versionId}`),
  promotion: (versionId: number) =>
    useApi<PromotionEvidence>(`${models}/Version/${versionId}/PromotionEvidence`),
  compare: (versionA: number, versionB: number) =>
    useApi<VersionComparison>(`${models}/Compare/${versionA}/${versionB}`),
  narrative: (versionId: number) => useApi<AnalyticsNarrative>(`${charts}/Narrative/${versionId}`),
  chartPath: (name: 'ModelDiagram' | 'Calibration' | 'Roc', versionId: number) =>
    `${charts}/${name}/${versionId}`,
  modelNames: () => useApi<ModelName[]>('/api/EntityAnalysisModel'),
  statuses: (modelId: number) =>
    useApi<ValueStatus[]>('/api/EntityApproval/ValueStatus', {
      method: 'POST',
      body: { kind: modelKind, entityId: modelId },
    }),
  setActive: (id: number, active: boolean) =>
    useApi(`${instances}/${id}/active`, { method: 'PUT', body: { active } }),
  remove: (id: number) => useApi(`${instances}/${id}`, { method: 'DELETE' }),
  act: (id: number, action: 'Resubmit' | 'Retract', note: string | null) =>
    useApi(`${instances}/${id}/${action}`, { method: 'POST', body: { note } }),
  decide: valueGridApi.decide,
  approveAll: (modelId: number) =>
    useApi<{ approved: number; refusals: string[] }>('/api/EntityApproval/ApproveValues', {
      method: 'POST',
      body: { kind: modelKind, entityId: modelId },
    }),
}
