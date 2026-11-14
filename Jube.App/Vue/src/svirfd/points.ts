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

import type {MapRing} from '../watcher/rings'
import {kmBound} from './format'
import type {IpRecord} from './types'

export interface LegendEntry {
  kind: string
  colour: string
}

export const kindColours: Record<string, string> = {
  location: '#2fab9b',
  hub: '#005f7f',
  prior: '#9ca3af',
  estimate: '#0ea5e9',
  probe: '#64748b',
  asnHub: '#d97706',
  snapSite: '#16a34a',
  vantage: '#475569',
  storedLocation: '#b42318',
}

function row(
  kind: string,
  index: number,
  place: string,
  lat: number,
  lon: number,
  elevation: number,
  summary: string,
  stamp: string,
  extra: Record<string, unknown> = {},
): Record<string, unknown> {
  return {
    id: `${kind}:${index}`,
    key: kind,
    keyValue: place || kind,
    latitude: lat,
    longitude: lon,
    responseElevation: elevation,
    activationRuleSummary: summary,
    backColor: kindColours[kind] ?? '#64748b',
    createdDate: stamp,
    ...extra,
  }
}

function finite(value: number | null | undefined): value is number {
  return typeof value === 'number' && Number.isFinite(value)
}

export function recordPoints(
  record: IpRecord,
  stored?: IpRecord | null,
): Record<string, unknown>[] {
  const rows: Record<string, unknown>[] = []
  const stamp = record.location.measured?.measuredAt ?? record.asOf.indexLoaded
  const {location} = record

  let anchor: { lat: number; lon: number } | null = null

  if (finite(location.lat) && finite(location.lon)) {
    anchor = {lat: location.lat, lon: location.lon}
    rows.push(
      row(
        'location',
        0,
        location.city ?? record.ip,
        location.lat,
        location.lon,
        10,
        `${location.city ?? record.ip} · ${location.cityConfidence}`,
        stamp,
      ),
    )
  }

  if (location.hub) {
    anchor ??= {lat: location.hub.lat, lon: location.hub.lon}
    rows.push(
      row(
        'hub',
        0,
        location.hub.city,
        location.hub.lat,
        location.hub.lon,
        8,
        `${location.hub.city} · ${location.hub.rttMs} ms`,
        stamp,
      ),
    )
  }

  if (location.prior) {
    rows.push(
      row(
        'prior',
        0,
        location.prior.city,
        location.prior.lat,
        location.prior.lon,
        5,
        `${location.prior.city} · ${location.prior.basis}`,
        stamp,
        {dim: true},
      ),
    )
  }

  const estimate = location.measured?.estimate

  if (estimate) {
    anchor ??= {lat: estimate.lat, lon: estimate.lon}
    rows.push(
      row(
        'estimate',
        0,
        estimate.city ?? record.ip,
        estimate.lat,
        estimate.lon,
        7,
        `${estimate.city ?? record.ip} · ${estimate.probes} probes`,
        stamp,
      ),
    )
  }

  location.measured?.probesByRtt.forEach((probe, index) => {
    if (!finite(probe.lat) || !finite(probe.lon)) return

    rows.push(
      row(
        'probe',
        index,
        probe.city ?? '',
        probe.lat,
        probe.lon,
        probe.reached ? 6 : 3,
        `${probe.city ?? '-'} · ${probe.rtt} ms${probe.reached ? '' : ' · edge only'}`,
        stamp,
        anchor
          ? {
            previousLatitude: anchor.lat,
            previousLongitude: anchor.lon,
            previousCreatedDate: stamp,
          }
          : {},
      ),
    )
  })

  record.observed?.asnHubs?.forEach((hub, index) => {
    rows.push(
      row(
        'asnHub',
        index,
        hub.city,
        hub.lat,
        hub.lon,
        Math.min(10, hub.weight * 2),
        `${hub.city} · ${hub.routers} routers`,
        stamp,
      ),
    )
  })

  location.snap?.sites.forEach((site, index) => {
    rows.push(
      row(
        'snapSite',
        index,
        site.site,
        site.lat,
        site.lon,
        site.fits ? 6 : 3,
        `${site.site} · ${site.fits ? 'fits' : 'ruled out'}`,
        stamp,
        site.fits ? {} : {dim: true},
      ),
    )
  })

  const vantage = record.density?.vantage

  if (vantage && finite(vantage.lat) && finite(vantage.lon)) {
    rows.push(
      row(
        'vantage',
        0,
        vantage.site,
        vantage.lat,
        vantage.lon,
        4,
        `Vantage ${vantage.site}`,
        stamp,
      ),
    )
  }

  if (
    stored &&
    stored.location.lat !== null &&
    stored.location.lon !== null &&
    anchor &&
    (stored.location.lat !== anchor.lat || stored.location.lon !== anchor.lon)
  ) {
    rows.push(
      row(
        'storedLocation',
        0,
        stored.location.city ?? stored.ip,
        stored.location.lat,
        stored.location.lon,
        8,
        `Archived · ${stored.location.city ?? stored.ip}`,
        stamp,
        {previousLatitude: anchor.lat, previousLongitude: anchor.lon, previousCreatedDate: stamp},
      ),
    )
  }

  return rows
}

export function recordRings(record: IpRecord): MapRing[] {
  const rings: MapRing[] = []
  const {location} = record
  const anchor = location.hub ?? location.measured?.estimate

  record.location.measured?.probesByRtt.forEach((probe, index) => {
    if (!finite(probe.lat) || !finite(probe.lon)) return

    rings.push({
      id: `probe:${index}`,
      latitude: probe.lat,
      longitude: probe.lon,
      radiusKm: kmBound(probe.rtt),
      colour: kindColours['probe'] ?? '#64748b',
      label: `${probe.city ?? ''} ${probe.rtt} ms`,
    })
  })

  if (finite(location.lat) && finite(location.lon) && finite(record.accuracyRadiusKm)) {
    rings.push({
      id: 'accuracy',
      latitude: location.lat,
      longitude: location.lon,
      radiusKm: record.accuracyRadiusKm,
      colour: kindColours['location'] ?? '#2fab9b',
      label: 'accuracy',
    })
  }

  if (anchor && 'radiusKm' in anchor && finite(anchor.radiusKm)) {
    rings.push({
      id: 'anchor',
      latitude: anchor.lat,
      longitude: anchor.lon,
      radiusKm: anchor.radiusKm,
      colour: kindColours['hub'] ?? '#005f7f',
      label: 'radius',
    })
  }

  const density = record.density

  if (
    density &&
    finite(density.withinKm) &&
    finite(density.vantage.lat) &&
    finite(density.vantage.lon)
  ) {
    rings.push({
      id: 'density',
      latitude: density.vantage.lat,
      longitude: density.vantage.lon,
      radiusKm: density.withinKm,
      colour: kindColours['vantage'] ?? '#475569',
      label: 'density',
    })
  }

  return rings
}

export function legendOf(rows: Record<string, unknown>[]): LegendEntry[] {
  const seen = new Set<string>()

  return rows.flatMap((candidate) => {
    const kind = String(candidate.key)

    if (seen.has(kind)) return []

    seen.add(kind)

    return [{kind, colour: kindColours[kind] ?? '#64748b'}]
  })
}

export const legendLabels: Record<string, string> = {
  location: 'svirfd.legend.location',
  hub: 'svirfd.legend.hub',
  prior: 'svirfd.legend.prior',
  estimate: 'svirfd.legend.estimate',
  probe: 'svirfd.legend.probe',
  asnHub: 'svirfd.legend.asnHub',
  snapSite: 'svirfd.legend.snapSite',
  vantage: 'svirfd.legend.vantage',
  storedLocation: 'svirfd.legend.storedLocation',
}
