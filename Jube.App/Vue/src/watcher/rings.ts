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

export interface MapRing {
  id: string
  latitude: number
  longitude: number
  radiusKm: number
  colour: string
  label: string
}

const earthRadiusKm = 6371.0088
const steps = 64

export function ringPolygon(ring: MapRing): [number, number][] {
  const lat = (ring.latitude * Math.PI) / 180
  const lon = (ring.longitude * Math.PI) / 180
  const angular = ring.radiusKm / earthRadiusKm
  const coordinates: [number, number][] = []

  for (let i = 0; i <= steps; i += 1) {
    const bearing = (2 * Math.PI * i) / steps
    const pointLat = Math.asin(
      Math.sin(lat) * Math.cos(angular) + Math.cos(lat) * Math.sin(angular) * Math.cos(bearing),
    )
    const pointLon =
      lon +
      Math.atan2(
        Math.sin(bearing) * Math.sin(angular) * Math.cos(lat),
        Math.cos(angular) - Math.sin(lat) * Math.sin(pointLat),
      )

    coordinates.push([(((pointLon * 180) / Math.PI + 540) % 360) - 180, (pointLat * 180) / Math.PI])
  }

  return coordinates
}

export function ringsGeoJson(rings: MapRing[]): {
  type: 'FeatureCollection'
  features: {
    type: 'Feature'
    properties: { id: string; colour: string; label: string }
    geometry: { type: 'Polygon'; coordinates: [number, number][][] }
  }[]
} {
  return {
    type: 'FeatureCollection',
    features: rings
      .filter(
        (ring) =>
          Number.isFinite(ring.latitude) &&
          Number.isFinite(ring.longitude) &&
          Number.isFinite(ring.radiusKm) &&
          ring.radiusKm > 0,
      )
      .map((ring) => ({
        type: 'Feature' as const,
        properties: {id: ring.id, colour: ring.colour, label: ring.label},
        geometry: {type: 'Polygon' as const, coordinates: [ringPolygon(ring)]},
      })),
  }
}
