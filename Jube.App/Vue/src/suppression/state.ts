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

import {ApiError} from '../api/useApi'
import {bodyReasons, validationReasons} from '../api/problems'
import {intlTag} from '../composables/useFormat'
import type {CandidateValue, SuppressionCounts} from './types'

export const state = {pending: 1, approved: 2, rejected: 3, retracted: 4} as const

export const standing = {
  pending: 1,
  approved: 2,
  rejected: 3,
  retracted: 4,
  expired: 5,
  removed: 6,
} as const

export const decision = {approved: 1, rejected: 2, retracted: 3, resubmitted: 4} as const

export const path = {caseKey: 1, transaction: 2} as const

export const widerState = {counted: 1, notIndexed: 2} as const

export const reachWarningKeys = 10

export type StandingKey = 'pending' | 'approved' | 'rejected' | 'retracted' | 'expired' | 'removed'

export function standingKey(id: number): StandingKey {
  switch (id) {
    case standing.approved:
      return 'approved'
    case standing.rejected:
      return 'rejected'
    case standing.retracted:
      return 'retracted'
    case standing.expired:
      return 'expired'
    case standing.removed:
      return 'removed'
    default:
      return 'pending'
  }
}

export function standingTone(id: number): 'success' | 'warning' | 'error' | 'neutral' {
  if (id === standing.approved) return 'success'
  if (id === standing.pending) return 'warning'
  if (id === standing.rejected || id === standing.removed) return 'error'

  return 'neutral'
}

export function widerKnown(counts: {
  widerCountStateId: number
  widerKeys: number | null
  widerTransactions: number | null
}): boolean {
  return (
    counts.widerCountStateId === widerState.counted &&
    counts.widerKeys !== null &&
    counts.widerTransactions !== null
  )
}

export function otherKeys(counts: Pick<SuppressionCounts, 'widerKeys'>): number {
  return Math.max(0, (counts.widerKeys ?? 0) - 1)
}

export function widerNumber(value: number | null, capped: boolean, locale = 'en'): string {
  if (value === null) return ''

  return `${new Intl.NumberFormat(intlTag(locale)).format(value)}${capped ? '+' : ''}`
}

export function isolate(value: string | null | undefined): string {
  return value ? `\u2068${value}\u2069` : ''
}

export function targetName(model: string | null, rule: string | null): string {
  return rule ? `${isolate(model)} / ${isolate(rule)}` : isolate(model)
}

export function wideReach(value: Pick<CandidateValue, 'widerKeys' | 'widerCountStateId'>): number {
  return value.widerCountStateId === widerState.counted
    ? Math.max(0, (value.widerKeys ?? 0) - 1)
    : 0
}

export function isWide(value: Pick<CandidateValue, 'widerKeys' | 'widerCountStateId'>): boolean {
  return wideReach(value) >= reachWarningKeys
}

export function laneNames(value: CandidateValue): string {
  return value.lanes
    .filter((lane) => lane.laneKey !== '')
    .map((lane) => `${lane.laneKey} ${lane.laneValue}`)
    .join(', ')
}

export function mergeParameter(lanes: { key: string; value: string }[]): string | null {
  return lanes.length === 0
    ? null
    : JSON.stringify(lanes.map((lane) => ({key: lane.key, value: lane.value})))
}

export function refusalText(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) return fallback

  if (error.kind === 'conflict') return bodyReasons(error.body, fallback)[0] ?? fallback

  if (error.kind === 'validation') return validationReasons(error, fallback)[0] ?? fallback

  if (error.kind === 'forbidden') return fallback

  return fallback
}

export function defaultExpiry(now: Date = new Date(), days = 30): Date {
  const next = new Date(now.getTime())

  next.setUTCDate(next.getUTCDate() + days)

  return next
}
