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

import {analystLikelihoodKeys} from './assessmentApi'
import {closedStatusKeys, diaryTypeKeys, eventKeys, triageOutcomeKeys} from '../labels'

export type EventValueKind =
  | 'none'
  | 'text'
  | 'user'
  | 'date'
  | 'number'
  | 'status'
  | 'diaryType'
  | 'closedStatus'
  | 'triageOutcome'
  | 'analystLikelihood'

export type EventSide = 'before' | 'after'

export interface EventValueContext {
  t: (key: string) => string
  dateTime: (value: string) => string
  statusName: (guid: string) => string | null
}

const kinds: Record<number, [EventValueKind, EventValueKind]> = {
  5: ['none', 'date'],
  7: ['diaryType', 'diaryType'],
  9: ['status', 'status'],
  10: ['date', 'date'],
  11: ['number', 'number'],
  12: ['date', 'date'],
  13: ['date', 'date'],
  14: ['user', 'user'],
  15: ['closedStatus', 'closedStatus'],
  17: ['status', 'status'],
  18: ['user', 'text'],
  19: ['diaryType', 'none'],
  20: ['none', 'number'],
  29: ['none', 'triageOutcome'],
  30: ['triageOutcome', 'none'],
  31: ['triageOutcome', 'text'],
  34: ['analystLikelihood', 'analystLikelihood'],
  35: ['analystLikelihood', 'none'],
}

export function eventValueKind(typeId: number, side: EventSide): EventValueKind {
  const pair = kinds[typeId]

  if (!pair) return 'text'

  return pair[side === 'before' ? 0 : 1]
}

const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const isoDate = /^\d{4}-\d{2}-\d{2}T/

function asDate(raw: string, context: EventValueContext): string {
  if (!isoDate.test(raw)) return raw

  const utc = /(Z|[+-]\d{2}:\d{2})$/.test(raw) ? raw : `${raw}Z`

  return Number.isNaN(Date.parse(utc)) ? raw : context.dateTime(utc)
}

function asEnum(raw: string, keys: Record<number, string>, context: EventValueContext): string {
  const key = /^\d+$/.test(raw) ? keys[Number(raw)] : undefined

  return key ? context.t(key) : raw
}

export function formatEventValue(
  typeId: number,
  side: EventSide,
  raw: string | null,
  context: EventValueContext,
): string {
  if (raw === null || raw === '') return ''

  switch (eventValueKind(typeId, side)) {
    case 'none':
      return ''
    case 'date':
      return asDate(raw, context)
    case 'status':
      return (
        context.statusName(raw) ??
        (guid.test(raw) ? context.t('caseDetail.history.unknownStatus') : raw)
      )
    case 'diaryType':
      return asEnum(raw, diaryTypeKeys, context)
    case 'closedStatus':
      return asEnum(raw, closedStatusKeys, context)
    case 'triageOutcome':
      return asEnum(raw, triageOutcomeKeys, context)
    case 'analystLikelihood':
      return asEnum(raw, analystLikelihoodKeys, context)
    default:
      return raw
  }
}

export function eventTypeKey(typeId: number): string | null {
  return eventKeys[typeId] ?? null
}
