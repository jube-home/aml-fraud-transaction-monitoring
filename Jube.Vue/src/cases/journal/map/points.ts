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

import { segmentOf } from '../../../watcher/geo'
import type { Activation } from '../../../watcher/types'
import { paintFor } from '../paint'
import { laneLetters } from '../lanes'
import type { Lane } from '../state'
import type { Translate } from '../columns'
import type { JournalRow } from '../types'

export interface MapLocation {
  latitude: string
  longitude: string
}

function coordinate(value: unknown): number | null {
  if (value === null || value === undefined || value === '') return null

  const number = Number(value)

  return Number.isFinite(number) ? number : null
}

function stamp(row: JournalRow): string {
  const value = row.ReferenceDate ?? row.CreatedDate

  return typeof value === 'string' ? value : ''
}

function laneOf(row: JournalRow, lanes: readonly Lane[]): { letter: string; lane: Lane } {
  const letters = typeof row.Lane === 'string' ? row.Lane : ''
  const letter = [...letters].find((item) => laneLetters.includes(item as 'A')) ?? 'A'
  const lane = lanes[letter.charCodeAt(0) - 65] ?? lanes[0]!

  return { letter, lane }
}

export function toRawPoints(
  rows: readonly JournalRow[],
  location: MapLocation,
  lanes: readonly Lane[],
  t: Translate,
): Record<string, unknown>[] {
  const ordered = [...rows].sort((a, b) => Date.parse(stamp(a)) - Date.parse(stamp(b)))
  const last = new Map<string, { latitude: number; longitude: number; at: string }>()

  return ordered.map((row) => {
    const { letter, lane } = laneOf(row, lanes)
    const dim = row.Matched === false
    const latitude = coordinate(row[location.latitude])
    const longitude = coordinate(row[location.longitude])
    const at = stamp(row)
    const entity = `${lane.key}:${lane.value}`
    const previous = dim ? undefined : last.get(entity)
    const paint = paintFor(row, t)
    const activations = typeof row.Activations === 'string' ? row.Activations : ''
    const summary = [activations, ...(paint?.reasons ?? [])]
      .filter((part) => part !== '')
      .join('. ')

    if (!dim && latitude !== null && longitude !== null && !(latitude === 0 && longitude === 0)) {
      last.set(entity, { latitude, longitude, at })
    }

    return {
      id: String(row.EntityAnalysisModelInstanceEntryGuid ?? row.Id),
      key: lane.key,
      keyValue: lane.value,
      latitude,
      longitude,
      previousLatitude: previous?.latitude,
      previousLongitude: previous?.longitude,
      previousCreatedDate: previous?.at,
      activationRuleSummary: `${letter}. ${summary}`.trim(),
      responseElevation: typeof row.ResponseElevation === 'number' ? row.ResponseElevation : 0,
      backColor: paint?.row.back,
      foreColor: paint?.row.fore,
      createdDate: at,
      dim,
    }
  })
}

function usable(
  point: Record<string, unknown>,
): { latitude: number; longitude: number; at: number } | null {
  const latitude = coordinate(point.latitude)
  const longitude = coordinate(point.longitude)
  const at = Date.parse(String(point.createdDate))

  if (
    latitude === null ||
    longitude === null ||
    Math.abs(latitude) > 90 ||
    Math.abs(longitude) > 180 ||
    (latitude === 0 && longitude === 0) ||
    Number.isNaN(at)
  ) {
    return null
  }

  return { latitude, longitude, at }
}

export function onImpossibleLegs(
  points: readonly Record<string, unknown>[],
  limitKmh: number,
  minimumKilometres: number,
): Record<string, unknown>[] {
  const keep = new Set<string>()
  const last = new Map<string, { id: string; latitude: number; longitude: number; at: number }>()

  for (const point of points) {
    const found = usable(point)

    if (!found || point.dim === true) continue

    const entity = `${String(point.key)}:${String(point.keyValue)}`
    const id = String(point.id)
    const previous = last.get(entity)

    if (previous) {
      const leg = segmentOf(
        { latitude: previous.latitude, longitude: previous.longitude, createdAt: previous.at },
        {
          latitude: found.latitude,
          longitude: found.longitude,
          createdAt: found.at,
        } as Activation,
        limitKmh,
        minimumKilometres,
      )

      if (leg.impossible) {
        keep.add(previous.id)
        keep.add(id)
      }
    }

    last.set(entity, { id, ...found })
  }

  return points.filter((point) => point.dim === true || keep.has(String(point.id)))
}
