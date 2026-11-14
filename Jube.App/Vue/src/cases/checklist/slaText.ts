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

import {slaState, type ChecklistItem} from './types'

export type SlaTone = 'neutral' | 'error'

export interface SlaView {
  key: string
  minutes: number | null
  tone: SlaTone
}

export type TranslateWith = (key: string, values?: Record<string, unknown>) => string

export function durationText(t: TranslateWith, minutes: number): string {
  const total = Math.max(0, Math.round(minutes))
  const days = Math.floor(total / 1440)
  const hours = Math.floor((total % 1440) / 60)
  const rest = total % 60
  const parts: string[] = []

  if (days > 0) parts.push(t('caseDetail.checklist.duration.days', {n: days}))
  if (hours > 0) parts.push(t('caseDetail.checklist.duration.hours', {n: hours}))
  if (rest > 0 || parts.length === 0) {
    parts.push(t('caseDetail.checklist.duration.minutes', {n: rest}))
  }

  return parts.slice(0, 2).join(' ')
}

export function slaView(item: ChecklistItem): SlaView | null {
  switch (item.slaStateId) {
    case slaState.onTrack:
      return {
        key: 'caseDetail.checklist.sla.dueIn',
        minutes: Math.max(0, item.minutesRemaining ?? 0),
        tone: 'neutral',
      }
    case slaState.breached:
      return {
        key: 'caseDetail.checklist.sla.overdue',
        minutes: Math.abs(Math.min(0, item.minutesRemaining ?? 0)),
        tone: 'error',
      }
    case slaState.completedOnTime:
      return {key: 'caseDetail.checklist.sla.onTime', minutes: null, tone: 'neutral'}
    case slaState.completedLate:
      return {key: 'caseDetail.checklist.sla.late', minutes: null, tone: 'neutral'}
    default:
      return null
  }
}
