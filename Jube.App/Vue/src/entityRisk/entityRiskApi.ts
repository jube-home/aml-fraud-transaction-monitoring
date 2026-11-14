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

export interface Rating {
  keyName: string
  keyValue: string
  version: number
  tierId: number
  score: number | null
  sourceId: number
  reason: string | null
  reviewByDate: string | null
  createdUser: string
  createdDate: string
}

export interface RatingHistory {
  current: Rating | null
  history: Rating[]
}

export interface RatingRequest {
  guid: string
  keyName: string
  keyValue: string
  tierId: number
  score: number | null
  reviewByDate: string | null
  reason: string
  basedOnVersion: number
  stateId: number
  resultingVersion: number | null
  requestedUser: string
  requestedDate: string
  decidedUser: string | null
  decidedDate: string | null
  decisionNote: string | null
}

export const entityRiskKey = ['service', 'EntityRiskRating'] as const
export const entityRiskPermission = {write: 18, approve: 80} as const
export const maximumReason = 500
export const maximumKeyName = 128
export const maximumKeyValue = 256

const base = '/api/EntityRiskRating'

export const entityRiskApi = {
  byKey: (keyName: string, keyValue: string) =>
    useApi<RatingHistory>(`${base}/ByKey`, {query: {keyName, keyValue}}),
  due: () => useApi<Rating[]>(`${base}/Due`),
  requests: () => useApi<RatingRequest[]>(`${base}/Requests`),
  propose: (rating: {
    keyName: string
    keyValue: string
    tierId: number
    score: number | null
    reviewByDate: string | null
    reason: string
  }) => useApi<RatingRequest>(`${base}/Set`, {method: 'POST', body: rating}),
  approve: (guid: string, note: string | null) =>
    useApi<RatingRequest>(`${base}/Approve`, {method: 'POST', body: {guid, note}}),
  reject: (guid: string, note: string) =>
    useApi<RatingRequest>(`${base}/Reject`, {method: 'POST', body: {guid, note}}),
  retract: (guid: string) => useApi<RatingRequest>(`${base}/Retract/${guid}`, {method: 'POST'}),
}

export function proposalReady(
  keyName: string,
  keyValue: string,
  tier: number,
  reason: string,
): boolean {
  return (
    keyName.trim().length > 0 &&
    keyName.length <= maximumKeyName &&
    keyValue.length > 0 &&
    keyValue.length <= maximumKeyValue &&
    tier >= 1 &&
    tier <= 3 &&
    reason.trim().length > 0 &&
    reason.trim().length <= maximumReason
  )
}

export function canDecide(request: RatingRequest, user: string | null): boolean {
  return request.stateId === 1 && request.requestedUser !== user
}

export function canRetract(request: RatingRequest, user: string | null): boolean {
  return request.stateId === 1 && request.requestedUser === user
}
