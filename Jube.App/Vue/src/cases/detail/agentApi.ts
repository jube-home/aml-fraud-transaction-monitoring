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

export interface CaseAgentRun {
  guid: string
  recommendedOutcomeId: number
  rationale: string
  evidence: string[]
  stateId: number
  overrideReason: string | null
  decidedUser: string | null
  decidedDate: string | null
  createdDate: string
  modelName: string | null
}

export const agentKey = (caseId: number) => ['service', 'CaseAgentRun', 'byCase', caseId] as const

export const maximumOverrideReason = 500

export const pendingState = 1
export const acceptedState = 2
export const overriddenState = 3

export const agentApi = {
  list: (caseId: number) => useApi<CaseAgentRun[]>(`/api/CaseAgentRun/ByCaseId/${caseId}`),
  decide: (guid: string, accept: boolean, reason: string | null) =>
    useApi('/api/CaseAgentRun/Decide', {method: 'POST', body: {guid, accept, reason}}),
}

export function currentRun(runs: CaseAgentRun[] | undefined): CaseAgentRun | null {
  return (runs ?? []).find((run) => run.stateId !== 4) ?? null
}
