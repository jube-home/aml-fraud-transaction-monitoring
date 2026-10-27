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

import type { Activation, GeoPoint, TrailSegment } from './types'

const earthKilometres = 6371.0088

const radians = (degrees: number): number => (degrees * Math.PI) / 180

export function haversineKm(
  from: { latitude: number; longitude: number },
  to: { latitude: number; longitude: number },
): number {
  const dLat = radians(to.latitude - from.latitude)
  const dLon = radians(to.longitude - from.longitude)
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(radians(from.latitude)) * Math.cos(radians(to.latitude)) * Math.sin(dLon / 2) ** 2

  return 2 * earthKilometres * Math.asin(Math.min(1, Math.sqrt(a)))
}

export const impossibleKmh = 900

export const impossibleKilometres = 50

export function segmentOf(
  from: GeoPoint,
  to: Activation,
  limitKmh = impossibleKmh,
  minimumKilometres = impossibleKilometres,
): TrailSegment {
  const kilometres = haversineKm(from, to)
  const hours = Math.max(0, to.createdAt - from.createdAt) / 3_600_000
  const speedKmh = hours > 0 ? kilometres / hours : kilometres > 0 ? Number.POSITIVE_INFINITY : 0

  return {
    entity: to.entity,
    from,
    to,
    kilometres,
    hours,
    speedKmh,
    impossible: kilometres >= minimumKilometres && speedKmh > limitKmh,
  }
}
