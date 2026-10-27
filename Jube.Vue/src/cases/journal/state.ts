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

import type { LocationQueryRaw } from 'vue-router'
import type { BuilderGroup } from '../../ruleBuilder/types'
import { parseFilter, serialiseFilter } from './filter'
import { journalLenses, type JournalLens } from './types'

export const windowDays = [7, 30, 90, 365] as const

export const defaultLimitKmh = 900

export const defaultMinimumKilometres = 50

export const maximumLimitKmh = 10000

export const maximumKilometres = 20000

export const defaultWindow = 90

export interface Lane {
  key: string
  value: string
}

export interface JournalState {
  lens: JournalLens
  days: number
  pivot: { key: string; value: string } | null
  transaction: number | null
  activationsOnly: boolean
  merge: Lane[]
  pinnedOnly: boolean
  filter: BuilderGroup | null
  speed: number
  distance: number
  improbable: boolean
  all: boolean
  trails: boolean
  heat: boolean
}

export const maximumLanes = 3

export const defaultJournalState: JournalState = {
  lens: 'payload',
  days: defaultWindow,
  pivot: null,
  transaction: null,
  activationsOnly: false,
  merge: [],
  pinnedOnly: false,
  filter: null,
  speed: defaultLimitKmh,
  distance: defaultMinimumKilometres,
  improbable: false,
  all: false,
  trails: true,
  heat: false,
}

type Query = Record<string, unknown>

const stateKeys = [
  'jl',
  'jw',
  'jk',
  'jv',
  'jt',
  'ja',
  'jm',
  'jp',
  'jf',
  'jx',
  'js',
  'jd',
  'ji',
  'jb',
]

function bounded(text: string | undefined, fallback: number, low: number, high: number): number {
  const value = Number(text)

  return text !== undefined &&
    text !== '' &&
    Number.isFinite(value) &&
    value >= low &&
    value <= high
    ? value
    : fallback
}

function first(value: unknown): string | undefined {
  const single = Array.isArray(value) ? value[0] : value

  return typeof single === 'string' ? single : undefined
}

export function parseLanes(raw: string | undefined): Lane[] {
  if (!raw) return []

  try {
    const parsed: unknown = JSON.parse(raw)

    if (!Array.isArray(parsed)) return []

    return parsed
      .filter(
        (item): item is [string, string] =>
          Array.isArray(item) &&
          item.length === 2 &&
          typeof item[0] === 'string' &&
          typeof item[1] === 'string' &&
          item[0] !== '' &&
          item[1] !== '',
      )
      .slice(0, maximumLanes)
      .map(([key, value]) => ({ key, value }))
  } catch {
    return []
  }
}

export function parseJournalState(query: Query): JournalState {
  const lens = first(query.jl)
  const days = Number(first(query.jw))
  const key = first(query.jk)
  const value = first(query.jv)
  const transaction = Number(first(query.jt))

  return {
    lens: (journalLenses as readonly string[]).includes(lens ?? '')
      ? (lens as JournalLens)
      : defaultJournalState.lens,
    days: (windowDays as readonly number[]).includes(days) ? days : defaultWindow,
    pivot: key && value ? { key, value } : null,
    transaction: Number.isInteger(transaction) && transaction > 0 ? transaction : null,
    activationsOnly: first(query.ja) === '1',
    merge: parseLanes(first(query.jm)),
    pinnedOnly: first(query.jp) === '1',
    filter: parseFilter(first(query.jf)),
    speed: bounded(first(query.js), defaultLimitKmh, 1, maximumLimitKmh),
    distance: bounded(first(query.jd), defaultMinimumKilometres, 0, maximumKilometres),
    improbable: first(query.ji) === '1',
    all: first(query.jb) === '1',
    trails: first(query.jn) !== '0',
    heat: first(query.jh) === '1',
  }
}

export function serialiseJournalState(state: JournalState): Record<string, string> {
  const query: Record<string, string> = {}

  if (state.lens !== defaultJournalState.lens) query.jl = state.lens
  if (state.days !== defaultWindow) query.jw = String(state.days)
  if (state.pivot) {
    query.jk = state.pivot.key
    query.jv = state.pivot.value
  }
  if (state.transaction !== null) query.jt = String(state.transaction)
  if (state.activationsOnly) query.ja = '1'
  if (state.merge.length > 0) {
    query.jm = JSON.stringify(state.merge.map((lane) => [lane.key, lane.value]))
  }
  if (state.pinnedOnly) query.jp = '1'

  const filter = serialiseFilter(state.filter)

  if (filter) query.jf = filter
  if (state.speed !== defaultLimitKmh) query.js = String(state.speed)
  if (state.distance !== defaultMinimumKilometres) query.jd = String(state.distance)
  if (state.improbable) query.ji = '1'
  if (state.all) query.jb = '1'
  if (!state.trails) query.jn = '0'
  if (state.heat) query.jh = '1'

  return query
}

export function mergeJournalState(query: Query, state: JournalState): LocationQueryRaw {
  const kept = Object.fromEntries(
    Object.entries(query).filter(([name]) => !stateKeys.includes(name)),
  )

  return { ...kept, ...serialiseJournalState(state) } as LocationQueryRaw
}

export function windowFrom(days: number, now: Date): string {
  const start =
    Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()) - days * 86_400_000

  return new Date(start).toISOString()
}
