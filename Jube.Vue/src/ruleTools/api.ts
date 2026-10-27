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
  BacktestExplanation,
  BacktestFields,
  BacktestInstance,
  BacktestRequest,
  BacktestResult,
  BacktestSubmit,
  BacktestSubmitResult,
  DependencyImpact,
  DependencyList,
} from './types'

const backtest = '/api/EntityAnalysisModelBacktest'

export const ruleToolsApi = {
  fields: (modelId: number) => useApi<BacktestFields>(`${backtest}/Fields/${modelId}`),
  instances: (modelId: number, ruleType: string, ruleId: number | null) =>
    useApi<BacktestInstance[]>(`${backtest}/ByEntityAnalysisModelId/${modelId}`, {
      query: { ruleType, ruleId: ruleId ?? undefined },
    }),
  submit: (body: BacktestSubmit) =>
    useApi<BacktestSubmitResult>(backtest, { method: 'POST', body }),
  stop: (id: number) => useApi(`${backtest}/${id}/Stop`, { method: 'POST', body: {} }),
  result: (id: number) => useApi<BacktestResult>(`${backtest}/${id}/Result`),
  explain: (request: BacktestRequest, guid: string) =>
    useApi<BacktestExplanation>(`${backtest}/Explain`, {
      method: 'POST',
      body: { request, entityAnalysisModelInstanceEntryGuid: guid },
    }),
  dependencies: (modelId: number, ruleType: string, ruleId: number) =>
    useApi<DependencyList>(
      `/api/EntityAnalysisModelDependency/Dependencies/${modelId}/${ruleType}/${ruleId}`,
    ),
  dependents: (modelId: number, ruleType: string, ruleId: number) =>
    useApi<DependencyImpact>(
      `/api/EntityAnalysisModelDependency/Dependents/${modelId}/${ruleType}/${ruleId}`,
    ),
}
