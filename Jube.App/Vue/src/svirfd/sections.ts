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

import type {IpRecord} from './types'

export type FactKind =
  'text' | 'claim' | 'rpki' | 'confidence' | 'date' | 'number' | 'list' | 'bool'

export interface FactSpec {
  section: string
  path: string
  label: string
  kind: FactKind
}

export const sectionIds = [
  'identity',
  'network',
  'routing',
  'registry',
  'location',
  'privacy',
  'cloud',
  'observed',
  'density',
  'caveats',
] as const

export const sectionLabels: Record<string, string> = {
  identity: 'svirfd.section.identity',
  network: 'svirfd.section.network',
  routing: 'svirfd.section.routing',
  registry: 'svirfd.section.registry',
  location: 'svirfd.section.location',
  privacy: 'svirfd.section.privacy',
  cloud: 'svirfd.section.cloud',
  observed: 'svirfd.section.observed',
  density: 'svirfd.section.density',
  caveats: 'svirfd.section.caveats',
}

export const facts: FactSpec[] = [
  {section: 'identity', path: 'Hostname', label: 'svirfd.f.hostname', kind: 'text'},
  {
    section: 'identity',
    path: 'HostnameSampledFrom',
    label: 'svirfd.f.hostnameSampledFrom',
    kind: 'text',
  },
  {section: 'identity', path: 'Org', label: 'svirfd.f.org', kind: 'text'},
  {section: 'identity', path: 'Asn.Domain', label: 'svirfd.f.asnDomain', kind: 'text'},
  {section: 'network', path: 'Asn.Asn', label: 'svirfd.f.asnAsn', kind: 'text'},
  {section: 'network', path: 'Asn.Name', label: 'svirfd.f.asnName', kind: 'text'},
  {section: 'network', path: 'Asn.Handle', label: 'svirfd.f.asnHandle', kind: 'text'},
  {section: 'network', path: 'Asn.Country', label: 'svirfd.f.asnCountry', kind: 'text'},
  {section: 'network', path: 'Asn.Route', label: 'svirfd.f.asnRoute', kind: 'text'},
  {section: 'network', path: 'Asn.Type', label: 'svirfd.f.asnType', kind: 'text'},
  {section: 'network', path: 'Carrier.Name', label: 'svirfd.f.carrierName', kind: 'text'},
  {
    section: 'network',
    path: 'Connection.NetworkType',
    label: 'svirfd.f.connectionNetworkType',
    kind: 'text',
  },
  {
    section: 'network',
    path: 'Connection.Mobile',
    label: 'svirfd.f.connectionMobile',
    kind: 'claim',
  },
  {
    section: 'network',
    path: 'Connection.Hosting',
    label: 'svirfd.f.connectionHosting',
    kind: 'claim',
  },
  {
    section: 'network',
    path: 'Connection.Satellite',
    label: 'svirfd.f.connectionSatellite',
    kind: 'claim',
  },
  {
    section: 'network',
    path: 'Connection.Shared',
    label: 'svirfd.f.connectionShared',
    kind: 'bool',
  },
  {section: 'routing', path: 'Routing.Prefix', label: 'svirfd.f.routingPrefix', kind: 'text'},
  {
    section: 'routing',
    path: 'Routing.Origin.Asn',
    label: 'svirfd.f.routingOriginAsn',
    kind: 'text',
  },
  {
    section: 'routing',
    path: 'Routing.Origin.Name',
    label: 'svirfd.f.routingOriginName',
    kind: 'text',
  },
  {
    section: 'routing',
    path: 'Routing.Origin.Peers',
    label: 'svirfd.f.routingOriginPeers',
    kind: 'number',
  },
  {
    section: 'routing',
    path: 'Routing.Origin.UpstreamName',
    label: 'svirfd.f.routingOriginUpstreamName',
    kind: 'text',
  },
  {
    section: 'routing',
    path: 'Routing.Origin.Rpki',
    label: 'svirfd.f.routingOriginRpki',
    kind: 'rpki',
  },
  {section: 'routing', path: 'Routing.Moas', label: 'svirfd.f.routingMoas', kind: 'bool'},
  {
    section: 'routing',
    path: 'Routing.FirstSeen',
    label: 'svirfd.f.routingFirstSeen',
    kind: 'date',
  },
  {section: 'routing', path: 'Routing.LastSeen', label: 'svirfd.f.routingLastSeen', kind: 'date'},
  {section: 'registry', path: 'Registry.Rir', label: 'svirfd.f.registryRir', kind: 'text'},
  {section: 'registry', path: 'Registry.Status', label: 'svirfd.f.registryStatus', kind: 'text'},
  {
    section: 'registry',
    path: 'Registry.Country',
    label: 'svirfd.f.registryCountry',
    kind: 'text',
  },
  {section: 'registry', path: 'Registry.Range', label: 'svirfd.f.registryRange', kind: 'text'},
  {
    section: 'registry',
    path: 'Registry.Allocated',
    label: 'svirfd.f.registryAllocated',
    kind: 'date',
  },
  {
    section: 'location',
    path: 'Location.Country',
    label: 'svirfd.f.locationCountry',
    kind: 'text',
  },
  {section: 'location', path: 'Location.City', label: 'svirfd.f.locationCity', kind: 'text'},
  {section: 'location', path: 'Timezone.Id', label: 'svirfd.f.timezoneId', kind: 'text'},
  {section: 'location', path: 'Timezone.Basis', label: 'svirfd.f.timezoneBasis', kind: 'text'},
  {
    section: 'location',
    path: 'Location.CityConfidence',
    label: 'svirfd.f.locationCityConfidence',
    kind: 'confidence',
  },
  {
    section: 'location',
    path: 'Location.CitySource',
    label: 'svirfd.f.locationCitySource',
    kind: 'text',
  },
  {section: 'location', path: 'Location.Kind', label: 'svirfd.f.locationKind', kind: 'text'},
  {section: 'location', path: 'Location.Lat', label: 'svirfd.f.locationLat', kind: 'number'},
  {section: 'location', path: 'Location.Lon', label: 'svirfd.f.locationLon', kind: 'number'},
  {
    section: 'location',
    path: 'AccuracyRadiusKm',
    label: 'svirfd.f.accuracyRadiusKm',
    kind: 'number',
  },
  {
    section: 'location',
    path: 'Location.Hub.City',
    label: 'svirfd.f.locationHubCity',
    kind: 'text',
  },
  {section: 'privacy', path: 'Privacy.Vpn', label: 'svirfd.f.privacyVpn', kind: 'claim'},
  {section: 'privacy', path: 'Privacy.Tor', label: 'svirfd.f.privacyTor', kind: 'claim'},
  {section: 'privacy', path: 'Privacy.Relay', label: 'svirfd.f.privacyRelay', kind: 'claim'},
  {section: 'privacy', path: 'Privacy.Hosting', label: 'svirfd.f.privacyHosting', kind: 'claim'},
  {section: 'privacy', path: 'Privacy.Proxy', label: 'svirfd.f.privacyProxy', kind: 'claim'},
  {section: 'privacy', path: 'Privacy.Service', label: 'svirfd.f.privacyService', kind: 'text'},
  {
    section: 'privacy',
    path: 'Privacy.ListsChecked',
    label: 'svirfd.f.privacyListsChecked',
    kind: 'list',
  },
  {section: 'cloud', path: 'Cloud.Provider', label: 'svirfd.f.cloudProvider', kind: 'text'},
  {section: 'cloud', path: 'Cloud.Region', label: 'svirfd.f.cloudRegion', kind: 'text'},
  {section: 'cloud', path: 'Cloud.Service', label: 'svirfd.f.cloudService', kind: 'text'},
  {section: 'cloud', path: 'Cloud.Range', label: 'svirfd.f.cloudRange', kind: 'text'},
  {section: 'observed', path: 'Observed.Prefix', label: 'svirfd.f.observedPrefix', kind: 'text'},
  {
    section: 'observed',
    path: 'Observed.RouterAddresses',
    label: 'svirfd.f.observedRouterAddresses',
    kind: 'number',
  },
  {
    section: 'observed',
    path: 'Observed.Probes',
    label: 'svirfd.f.observedProbes',
    kind: 'number',
  },
  {
    section: 'observed',
    path: 'Observed.Anycast',
    label: 'svirfd.f.observedAnycast',
    kind: 'bool',
  },
  {
    section: 'observed',
    path: 'Observed.RegistryContradiction',
    label: 'svirfd.f.observedRegistryContradiction',
    kind: 'bool',
  },
  {section: 'density', path: 'Density.Sampled', label: 'svirfd.f.densitySampled', kind: 'number'},
  {
    section: 'density',
    path: 'Density.Answered',
    label: 'svirfd.f.densityAnswered',
    kind: 'number',
  },
  {
    section: 'density',
    path: 'Density.Fraction',
    label: 'svirfd.f.densityFraction',
    kind: 'number',
  },
  {section: 'density', path: 'Density.Reading', label: 'svirfd.f.densityReading', kind: 'text'},
  {
    section: 'density',
    path: 'Density.TtlReading',
    label: 'svirfd.f.densityTtlReading',
    kind: 'text',
  },
  {
    section: 'density',
    path: 'Density.MinRttMs',
    label: 'svirfd.f.densityMinRttMs',
    kind: 'number',
  },
  {
    section: 'density',
    path: 'Density.MedianRttMs',
    label: 'svirfd.f.densityMedianRttMs',
    kind: 'number',
  },
  {
    section: 'density',
    path: 'Density.WithinKm',
    label: 'svirfd.f.densityWithinKm',
    kind: 'number',
  },
  {
    section: 'density',
    path: 'Density.CityConsistent',
    label: 'svirfd.f.densityCityConsistent',
    kind: 'claim',
  },
  {section: 'caveats', path: 'Bogon', label: 'svirfd.f.bogon', kind: 'bool'},
  {section: 'caveats', path: 'Special', label: 'svirfd.f.special', kind: 'text'},
  {section: 'caveats', path: 'AsOf.Routing', label: 'svirfd.f.asOfRouting', kind: 'date'},
  {section: 'caveats', path: 'AsOf.Registry', label: 'svirfd.f.asOfRegistry', kind: 'date'},
  {section: 'caveats', path: 'AsOf.IndexLoaded', label: 'svirfd.f.asOfIndexLoaded', kind: 'date'},
  {section: 'caveats', path: 'RequestId', label: 'svirfd.f.requestId', kind: 'text'},
]

export function valueAt(record: IpRecord, path: string): unknown {
  const segments = path.split('.')
  let current: unknown = record

  for (const segment of segments) {
    if (current === null || current === undefined) return null

    if (segment === 'Origin') {
      const origins = (current as { origins?: unknown[] }).origins

      current = origins && origins.length > 0 ? origins[0] : null
      continue
    }

    const key = segment.charAt(0).toLowerCase() + segment.slice(1)

    current = (current as Record<string, unknown>)[key]
  }

  return current ?? null
}

export interface Provenance {
  label: string
  tone: 'measured' | 'prior' | 'plain'
}

export function provenance(record: IpRecord, section: string): Provenance | null {
  const location = record.location
  const plain = (label: string): Provenance => ({label, tone: 'plain'})
  const measured = (label: string): Provenance => ({label, tone: 'measured'})

  switch (section) {
    case 'identity':
      return plain(record.hostname ? 'sampled' : 'null')
    case 'network':
      return plain(record.asn?.name ? 'published' : 'null')
    case 'routing':
      return record.routing?.prefix ? measured('measured') : plain('null')
    case 'registry':
      return plain(record.registry?.rir ? 'published' : 'null')
    case 'privacy':
      return plain(
        record.privacy?.vpn || record.privacy?.tor || record.privacy?.relay ? 'published' : 'null',
      )
    case 'cloud':
      return plain(record.cloud ? 'published' : 'null')
    case 'observed':
      return record.observed ? measured('measured') : plain('null')
    case 'density':
      return record.density ? plain(record.density.reading) : plain('null')
    case 'caveats':
      return plain('published')
    case 'location': {
      if (location?.prior) return {label: 'prior', tone: 'prior'}

      const measuredSource = /measured|traceroute/.test(location?.citySource ?? '')

      return {
        label: location?.cityConfidence || (record.city ? 'published' : 'null'),
        tone: measuredSource ? 'measured' : 'plain',
      }
    }
    default:
      return null
  }
}
