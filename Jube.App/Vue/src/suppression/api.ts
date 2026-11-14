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

import {useApi} from '../api/useApi'
import type {
  CandidateKey,
  CandidateTransaction,
  CandidateValue,
  CaseSuppression,
  InForce,
  SuppressionDecision,
  SuppressionRequest,
} from './types'

const base = '/api/CaseSuppression'
const candidates = '/api/CaseSuppressionCandidates'

export const suppressionApi = {
  keys: (caseId: number) => useApi<CandidateKey[]>(`${candidates}/Keys`, {query: {caseId}}),
  values: (caseId: number, key: string, merge: string | null) =>
    useApi<CandidateValue[]>(`${candidates}/Values`, {
      query: {caseId, key, merge: merge ?? undefined},
    }),
  transactions: (caseId: number, key: string, value: string | null, merge: string | null) =>
    useApi<CandidateTransaction[]>(`${candidates}/Transactions`, {
      query: {caseId, key, value: value ?? undefined, merge: merge ?? undefined},
    }),
  inForce: (caseId: number, merge: string | null) =>
    useApi<InForce[]>(`${candidates}/InForce`, {query: {caseId, merge: merge ?? undefined}}),
  reasonDraft: (request: SuppressionRequest) =>
    useApi<{ reason: string }>(`${candidates}/ReasonDraft`, {method: 'POST', body: request}),
  request: (request: SuppressionRequest) =>
    useApi<CaseSuppression>(base, {method: 'POST', body: request}),
  listByCase: (caseId: number) => useApi<CaseSuppression[]>(`${base}/ByCase/${caseId}`),
  awaiting: (limit = 100) => useApi<CaseSuppression[]>(`${base}/Awaiting`, {query: {limit}}),
  get: (id: number) => useApi<CaseSuppression>(`${base}/${id}`),
  approve: (decision: SuppressionDecision) =>
    useApi<CaseSuppression>(`${base}/Approve`, {method: 'POST', body: decision}),
  reject: (decision: SuppressionDecision) =>
    useApi<CaseSuppression>(`${base}/Reject`, {method: 'POST', body: decision}),
  retract: (decision: SuppressionDecision) =>
    useApi<CaseSuppression>(`${base}/Retract`, {method: 'POST', body: decision}),
  resubmit: (decision: SuppressionDecision) =>
    useApi<CaseSuppression>(`${base}/Resubmit`, {method: 'POST', body: decision}),
}
