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

export interface AnalystAssessment {
  guid: string
  caseId: number
  likelihoodId: number
  reason: string | null
  computedLikelihoodProbability: number | null
  createdUser: string
  createdDate: string
  withdrawnDate: string | null
  mine: boolean
}

export interface AnalystAssessmentState {
  current: AnalystAssessment | null
  history: AnalystAssessment[]
}

export const assessmentKey = (caseId: number) =>
  ['service', 'CaseAnalystAssessment', 'byCase', caseId] as const

export const maximumReason = 1000

export const likelihoodChoices = [1, 2, 3] as const

export const analystLikelihoodKeys: Record<number, string> = {
  1: 'caseDetail.assessment.low',
  2: 'caseDetail.assessment.medium',
  3: 'caseDetail.assessment.high',
}

export function canRecord(likelihoodId: number, reason: string): boolean {
  const trimmed = reason.trim()

  return (
    likelihoodId in analystLikelihoodKeys && trimmed.length > 0 && trimmed.length <= maximumReason
  )
}

export function earlierOf(state: AnalystAssessmentState | undefined): AnalystAssessment[] {
  if (!state) return []

  return state.history.filter((item) => item.guid !== state.current?.guid)
}

export const assessmentApi = {
  state: (caseId: number) =>
    useApi<AnalystAssessmentState>(`/api/CaseAnalystAssessment/ByCaseId/${caseId}`),
  record: (caseId: number, likelihoodId: number, reason: string) =>
    useApi<AnalystAssessment>('/api/CaseAnalystAssessment/Record', {
      method: 'POST',
      body: {caseId, likelihoodId, reason},
    }),
  withdraw: (guid: string) =>
    useApi('/api/CaseAnalystAssessment/Withdraw', {method: 'POST', body: {guid}}),
}
