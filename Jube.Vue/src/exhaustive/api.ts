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
  BatchResult,
  ExhaustiveInstance,
  NarrativeResult,
  PrescriptionRow,
  PromotedModel,
  RecallResult,
  SliderVariable,
  TrainingVariable,
  Trial,
  TrialDetail,
  VarianceRow,
} from './types'

const instances = '/api/ExhaustiveSearchInstance'

export const exhaustiveApi = {
  instance: (id: number) => useApi<ExhaustiveInstance>(`${instances}/${id}`),
  stop: (guid: string) => useApi(`${instances}/stop/${guid}`, { method: 'PUT', body: {} }),
  narrative: (id: number) => useApi<NarrativeResult>(`/api/Chart/Exhaustive/Narrative/${id}`),
  variables: (id: number) =>
    useApi<TrainingVariable[]>(`/api/GetExhaustiveSearchInstanceVariableQuery/${id}`),
  variance: (trialId: number) =>
    useApi<VarianceRow[]>(
      `/api/GetExhaustiveSearchInstanceTrialInstanceVariableVarianceQuery/${trialId}`,
    ),
  promoted: (id: number) =>
    useApi<PromotedModel[]>(`/api/GetExhaustiveSearchInstancePromotedTrialInstanceQuery/${id}`),
  setPromotedActive: (id: number, active: boolean) =>
    useApi('/api/ExhaustiveSearchInstancePromotedTrialInstance', {
      method: 'PUT',
      body: { id, active },
    }),
  prescriptions: (promotedId: number) =>
    useApi<PrescriptionRow[]>(
      `/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionQuery/${promotedId}`,
    ),
  sliders: (id: number) =>
    useApi<SliderVariable[]>(
      `/api/GetExhaustiveSearchInstancePromotedTrialInstanceVariableQuery/${id}`,
    ),
  recall: (guid: string, params: Record<string, string | number>) =>
    useApi<RecallResult>(`/api/Invoke/ExhaustiveSearchInstance/${guid}`, {
      method: 'POST',
      body: params,
    }),
  batch: (guid: string, file: File) => {
    const formData = new FormData()

    formData.append('file', file)

    return useApi<BatchResult>(`/api/Invoke/ExhaustiveSearchInstance/${guid}/Batch`, {
      method: 'POST',
      formData,
    })
  },
  trials: (id: number) =>
    useApi<Trial[]>(`/api/GetExhaustiveSearchInstanceTrialInstanceListQuery/${id}`),
  trialDetail: (trialId: number) =>
    useApi<TrialDetail>(`/api/GetExhaustiveSearchInstanceTrialInstanceDetail/${trialId}`),
}
