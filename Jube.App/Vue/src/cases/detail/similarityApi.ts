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

export interface CaseSimilarReason {
  kindId: number
  name: string
}

export interface CaseSimilarNeighbour {
  caseId: number
  caseKey: string | null
  caseKeyValue: string | null
  status: string | null
  closedStatusId: number
  outcomeId: number
  createdDate: string | null
  closedDate: string | null
  bandId: number
  score: number
  linked: boolean
  reasons: CaseSimilarReason[]
}

export interface CaseSimilarity {
  built: boolean
  checkedDate: string | null
  similar: number
  closed: number
  confirmed: number
  open: number
  enoughToJudge: boolean
  neighbours: CaseSimilarNeighbour[]
  enabled: boolean
}

export const similarityKey = (caseId: number) => ['service', 'CaseSimilarity', caseId] as const

export const similaritySectionId = 'case-similar'

export const similarityApi = {
  get: (caseId: number) => useApi<CaseSimilarity>(`/api/CaseSimilarity/${caseId}`),
}

export type SimilarOutcome = 'confirmed' | 'notConfirmed' | 'closed' | 'open'

export function outcomeOf(neighbour: Pick<CaseSimilarNeighbour, 'closedStatusId' | 'outcomeId'>): SimilarOutcome {
  if (neighbour.closedStatusId !== 3) return 'open'
  if (neighbour.outcomeId === 1) return 'confirmed'
  if (neighbour.outcomeId === 2) return 'notConfirmed'

  return 'closed'
}

export const outcomeKeys: Record<SimilarOutcome, string> = {
  confirmed: 'caseDetail.similar.outcome.confirmed',
  notConfirmed: 'caseDetail.similar.outcome.notConfirmed',
  closed: 'caseDetail.similar.outcome.closed',
  open: 'caseDetail.similar.outcome.open',
}

export const headlineKeys: Record<Exclude<SimilarHeadline, 'loading' | 'headline'>, string> = {
  failed: 'caseDetail.similar.failed',
  pending: 'caseDetail.similar.pending',
  off: 'caseDetail.similar.off',
  none: 'caseDetail.similar.none',
  tooFew: 'caseDetail.similar.tooFew',
}

export function reasonKey(kindId: number): string {
  if (kindId === 1) return 'caseDetail.similar.reason.rule'
  if (kindId === 2) return 'caseDetail.similar.reason.model'
  if (kindId === 3) return 'caseDetail.similar.reason.tag'

  return 'caseDetail.similar.reason.same'
}

export type SimilarHeadline =
  | 'failed'
  | 'loading'
  | 'pending'
  | 'off'
  | 'none'
  | 'headline'
  | 'tooFew'

export function headlineOf(data: CaseSimilarity | undefined, failed: boolean): SimilarHeadline {
  if (failed) return 'failed'
  if (!data) return 'loading'
  if (!data.built) return data.enabled === false ? 'off' : 'pending'
  if (data.similar === 0) return 'none'

  return data.enoughToJudge ? 'headline' : 'tooFew'
}
