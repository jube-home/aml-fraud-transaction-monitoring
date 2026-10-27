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

import { segmentOf } from './geo'
import type { Activation, TrailSegment, WatcherOptions, WatcherState } from './types'

export function emptyState(): WatcherState {
  return { history: [], rows: [], rules: {}, ingested: 0 }
}

export function matches(activation: Activation, options: WatcherOptions): boolean {
  if (options.keyFilter && activation.key !== options.keyFilter) return false

  return !options.keyValueFilter || activation.keyValue === options.keyValueFilter
}

export function ingest(
  state: WatcherState,
  incoming: Activation[],
  options: WatcherOptions,
  historyLimit = 5000,
): WatcherState {
  const accepted = incoming.filter((activation) => matches(activation, options))

  if (accepted.length === 0) return state

  const known = new Set(state.history.map((activation) => activation.id))
  const fresh = accepted.filter((activation) => !known.has(activation.id))

  if (fresh.length === 0) return state

  const history = [...state.history, ...fresh].sort((a, b) => a.createdAt - b.createdAt)
  const rules = { ...state.rules }

  for (const activation of fresh) {
    const name = activation.ruleSummary || '-'
    const current = rules[name] ?? { name, frequency: 0, aggregate: 0 }

    rules[name] = {
      name,
      frequency: current.frequency + 1,
      aggregate: current.aggregate + activation.responseElevation,
    }
  }

  const rows = [...fresh, ...state.rows]
    .sort((a, b) => b.createdAt - a.createdAt)
    .slice(0, options.maxRows)

  return {
    history: history.length > historyLimit ? history.slice(history.length - historyLimit) : history,
    rows,
    rules,
    ingested: state.ingested + fresh.length,
  }
}

export function withinWindow(
  history: Activation[],
  windowMs: number | null,
  now: number,
): Activation[] {
  return windowMs === null
    ? history
    : history.filter((activation) => now - activation.createdAt <= windowMs)
}

export function locatedOnly(history: Activation[]): Activation[] {
  return history.filter((activation) => activation.located)
}

export function latestPerEntity(history: Activation[]): Activation[] {
  const latest = new Map<string, Activation>()

  for (const activation of history) {
    const current = latest.get(activation.entity)

    if (!current || activation.createdAt >= current.createdAt)
      latest.set(activation.entity, activation)
  }

  return [...latest.values()]
}

export function trailsOf(
  history: Activation[],
  limitKmh?: number,
  minimumKilometres?: number,
): TrailSegment[] {
  const usable = locatedOnly(history)
    .filter(joinable)
    .filter((activation) => !activation.dim)
  const segments: TrailSegment[] = []

  for (const activation of usable) {
    const { previous } = activation

    if (
      previous &&
      (previous.latitude !== activation.latitude || previous.longitude !== activation.longitude)
    ) {
      segments.push(segmentOf(previous, activation, limitKmh, minimumKilometres))
    }
  }

  const byEntity = new Map<string, Activation[]>()

  for (const activation of usable.filter((item) => !item.previous)) {
    const list = byEntity.get(activation.entity) ?? []

    list.push(activation)
    byEntity.set(activation.entity, list)
  }

  for (const list of byEntity.values()) {
    const ordered = [...list].sort((a, b) => a.createdAt - b.createdAt)

    for (let index = 1; index < ordered.length; index++) {
      const from = ordered[index - 1]!
      const to = ordered[index]!

      if (from.latitude !== to.latitude || from.longitude !== to.longitude) {
        segments.push(segmentOf(from, to, limitKmh, minimumKilometres))
      }
    }
  }

  return segments
}

export function ruleList(
  state: WatcherState,
): { name: string; frequency: number; aggregate: number }[] {
  return Object.values(state.rules)
    .sort((a, b) => b.frequency - a.frequency || a.name.localeCompare(b.name))
    .slice(0, 100)
}

export function ageOpacity(
  createdAt: number,
  now: number,
  fadeMs: number | null,
  floor = 0.15,
): number {
  if (fadeMs === null) return 1

  const age = Math.max(0, now - createdAt)

  return Math.max(floor, 1 - (age / fadeMs) * (1 - floor))
}

const unnamed = new Set(['', 'missing'])

export function joinable(activation: Activation): boolean {
  return !unnamed.has(activation.keyValue.trim().toLowerCase())
}
