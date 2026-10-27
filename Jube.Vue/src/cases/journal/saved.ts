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

import { parseFilter } from './filter'
import {
  defaultJournalState,
  defaultLimitKmh,
  defaultMinimumKilometres,
  maximumKilometres,
  maximumLanes,
  maximumLimitKmh,
  parseLanes,
  windowDays,
  type JournalState,
  type Lane,
} from './state'
import { journalLenses, type JournalLens, type SavedView } from './types'

export function mergeText(lanes: readonly Lane[]): string | undefined {
  return lanes.length === 0
    ? undefined
    : JSON.stringify(lanes.map((lane) => ({ key: lane.key, value: lane.value })))
}

export function viewPatch(view: SavedView): Partial<JournalState> {
  return {
    days: (windowDays as readonly number[]).includes(view.days)
      ? view.days
      : defaultJournalState.days,
    merge: lanesOf(view.merge),
    filter: parseFilter(view.filter ?? undefined),
    speed: inRange(view.limitKmh, 1, maximumLimitKmh) ?? defaultLimitKmh,
    distance: inRange(view.minimumKilometres, 0, maximumKilometres) ?? defaultMinimumKilometres,
    improbable: view.impossibleOnly === true,
    all: view.showAll === true,
    lens: (journalLenses as readonly string[]).includes(view.lens ?? '')
      ? (view.lens as JournalLens)
      : defaultJournalState.lens,
    transaction: null,
    pinnedOnly: false,
  }
}

function inRange(value: number | null, low: number, high: number): number | null {
  return value !== null && Number.isFinite(value) && value >= low && value <= high ? value : null
}

function lanesOf(text: string | null): Lane[] {
  if (!text) return []

  try {
    const parsed: unknown = JSON.parse(text)

    if (!Array.isArray(parsed)) return []

    return parseLanes(
      JSON.stringify(
        parsed
          .filter(
            (item): item is Lane =>
              typeof item === 'object' &&
              item !== null &&
              typeof (item as Lane).key === 'string' &&
              typeof (item as Lane).value === 'string',
          )
          .slice(0, maximumLanes)
          .map((item) => [item.key, item.value]),
      ),
    )
  } catch {
    return []
  }
}
