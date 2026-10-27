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

import type { Activation } from './types'

const hex = /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i

export function safeColour(value: unknown): string | null {
  return typeof value === 'string' && hex.test(value.trim()) ? value.trim().toLowerCase() : null
}

function pick(record: Record<string, unknown>, name: string): unknown {
  return record[name] ?? record[name[0]!.toUpperCase() + name.slice(1)]
}

function text(value: unknown): string {
  return typeof value === 'string'
    ? value
    : value === null || value === undefined
      ? ''
      : String(value)
}

export function parseActivation(raw: unknown): Activation | null {
  if (typeof raw !== 'object' || raw === null) return null

  const record = raw as Record<string, unknown>
  const latitude = Number(pick(record, 'latitude') ?? Number.NaN)
  const longitude = Number(pick(record, 'longitude') ?? Number.NaN)
  const inRange =
    Number.isFinite(latitude) &&
    Number.isFinite(longitude) &&
    Math.abs(latitude) <= 90 &&
    Math.abs(longitude) <= 180
  const located = inRange && !(latitude === 0 && longitude === 0)

  const previousLatitude = Number(pick(record, 'previousLatitude') ?? Number.NaN)
  const previousLongitude = Number(pick(record, 'previousLongitude') ?? Number.NaN)
  const previousAt = Date.parse(text(pick(record, 'previousCreatedDate')))
  const previous =
    located &&
    Number.isFinite(previousLatitude) &&
    Number.isFinite(previousLongitude) &&
    Math.abs(previousLatitude) <= 90 &&
    Math.abs(previousLongitude) <= 180 &&
    !(previousLatitude === 0 && previousLongitude === 0) &&
    !Number.isNaN(previousAt)
      ? { latitude: previousLatitude, longitude: previousLongitude, createdAt: previousAt }
      : null

  const key = text(pick(record, 'key'))
  const keyValue = text(pick(record, 'keyValue'))
  const createdDate = text(pick(record, 'createdDate'))
  const parsed = Date.parse(createdDate)
  const elevation = Number(pick(record, 'responseElevation'))
  const stamp = Number.isNaN(parsed) ? Date.now() : parsed

  return {
    id: `${stamp}|${key}:${keyValue}|${text(pick(record, 'id')) || `${latitude},${longitude}|${text(pick(record, 'activationRuleSummary'))}`}`,
    key,
    keyValue,
    entity: `${key}:${keyValue}`,
    longitude: located ? longitude : 0,
    latitude: located ? latitude : 0,
    located,
    previous,
    ruleSummary: text(pick(record, 'activationRuleSummary')),
    responseElevation: Number.isFinite(elevation) ? elevation : 0,
    responseElevationContent: text(pick(record, 'responseElevationContent')),
    responseElevationName: text(pick(record, 'responseElevationName')),
    backColor: safeColour(pick(record, 'backColor')),
    foreColor: safeColour(pick(record, 'foreColor')),
    createdAt: stamp,
    createdDate,
    dim: pick(record, 'dim') === true,
  }
}
