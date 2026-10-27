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

import { ageOpacity } from './state'
import type { Activation, TrailSegment } from './types'

export interface PointFeature {
  type: 'Feature'
  geometry: { type: 'Point'; coordinates: [number, number] }
  properties: {
    id: string
    entity: string
    color: string
    elevation: number
    opacity: number
    fresh: boolean
    rule: string
  }
}

export interface TrailFeature {
  type: 'Feature'
  geometry: { type: 'LineString'; coordinates: [number, number][] }
  properties: {
    entity: string
    impossible: boolean
    speed: number
    kilometres: number
    opacity: number
  }
}

export interface Collection<T> {
  type: 'FeatureCollection'
  features: T[]
}

export const freshMilliseconds = 6000

const dimColour = '#8a8a8a'

const dimOpacity = 0.4

export function pointsGeoJson(
  activations: Activation[],
  now: number,
  fadeMs: number | null,
  fallbackColour: string,
): Collection<PointFeature> {
  return {
    type: 'FeatureCollection',
    features: activations.map((activation) => ({
      type: 'Feature',
      geometry: { type: 'Point', coordinates: [activation.longitude, activation.latitude] },
      properties: {
        id: activation.id,
        entity: activation.entity,
        color: activation.dim
          ? dimColour
          : activation.backColor && activation.backColor !== '#ffffff'
            ? activation.backColor
            : fallbackColour,
        elevation: activation.responseElevation,
        opacity: ageOpacity(activation.createdAt, now, fadeMs) * (activation.dim ? dimOpacity : 1),
        fresh: !activation.dim && now - activation.createdAt <= freshMilliseconds,
        rule: activation.ruleSummary,
      },
    })),
  }
}

const toRadians = (degrees: number): number => (degrees * Math.PI) / 180
const toDegrees = (radians: number): number => (radians * 180) / Math.PI

export function arcBetween(
  from: { latitude: number; longitude: number },
  to: { latitude: number; longitude: number },
  steps = 32,
): [number, number][] {
  const lat1 = toRadians(from.latitude)
  const lon1 = toRadians(from.longitude)
  const lat2 = toRadians(to.latitude)
  const lon2 = toRadians(to.longitude)
  const delta =
    2 *
    Math.asin(
      Math.sqrt(
        Math.sin((lat2 - lat1) / 2) ** 2 +
          Math.cos(lat1) * Math.cos(lat2) * Math.sin((lon2 - lon1) / 2) ** 2,
      ),
    )

  if (!(delta > 1e-9)) return [[from.longitude, from.latitude]]

  const points: [number, number][] = []
  let previous = from.longitude

  for (let index = 0; index <= steps; index++) {
    const fraction = index / steps
    const a = Math.sin((1 - fraction) * delta) / Math.sin(delta)
    const b = Math.sin(fraction * delta) / Math.sin(delta)
    const x = a * Math.cos(lat1) * Math.cos(lon1) + b * Math.cos(lat2) * Math.cos(lon2)
    const y = a * Math.cos(lat1) * Math.sin(lon1) + b * Math.cos(lat2) * Math.sin(lon2)
    const z = a * Math.sin(lat1) + b * Math.sin(lat2)
    let longitude = toDegrees(Math.atan2(y, x))
    const latitude = toDegrees(Math.atan2(z, Math.hypot(x, y)))

    while (longitude - previous > 180) longitude -= 360
    while (longitude - previous < -180) longitude += 360

    previous = longitude
    points.push([longitude, latitude])
  }

  return points
}

export function trailsGeoJson(
  segments: TrailSegment[],
  now: number,
  fadeMs: number | null,
): Collection<TrailFeature> {
  return {
    type: 'FeatureCollection',
    features: segments.map((segment) => ({
      type: 'Feature',
      geometry: { type: 'LineString', coordinates: arcBetween(segment.from, segment.to) },
      properties: {
        entity: segment.entity,
        impossible: segment.impossible,
        speed: Number.isFinite(segment.speedKmh) ? Math.round(segment.speedKmh) : -1,
        kilometres: Math.round(segment.kilometres),
        opacity: ageOpacity(segment.to.createdAt, now, fadeMs),
      },
    })),
  }
}
