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

export interface QaReview {
  caseId: number
  analystVerdictId: number
  agentVerdictId: number | null
  comment: string | null
  reviewerUser: string
  createdDate: string
}

export interface QaState {
  eligible: boolean
  hasRecommendation: boolean
  reviews: QaReview[]
}

export const qaKey = (caseId: number) => ['service', 'CaseQaReview', 'byCase', caseId] as const

export const maximumComment = 1000

export const verdictKeys: Record<number, string> = {
  1: 'caseDetail.qa.pass',
  2: 'caseDetail.qa.fail',
  3: 'caseDetail.qa.notApplicable',
}

export const qaApi = {
  state: (caseId: number) => useApi<QaState>(`/api/CaseQaReview/ByCaseId/${caseId}`),
  submit: (
    caseId: number,
    analystVerdictId: number,
    agentVerdictId: number | null,
    comment: string | null,
  ) =>
    useApi('/api/CaseQaReview/Submit', {
      method: 'POST',
      body: {caseId, analystVerdictId, agentVerdictId, comment},
    }),
}
