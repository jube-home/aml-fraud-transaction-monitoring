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

export type Claim = boolean | null

export interface Evidence {
  source: string
  detail: string
}

export interface Hub {
  city: string
  country: string | null
  lat: number
  lon: number
  radiusKm: number
  rttMs: number
  source: string
}

export interface ProbeCity {
  city: string | null
  country: string | null
  rtt: number
  reached: boolean
  depth: number
  lastHop: string | null
  lastHopAsn: number | null
  lat: number | null
  lon: number | null
}

export interface Estimate {
  city: string | null
  country: string | null
  lat: number
  lon: number
  radiusKm: number
  probes: number
}

export interface Measured {
  prefix: string
  city: string | null
  rttMs: number
  probes: number
  targetAnswered: boolean
  edgeOnly: boolean
  depth: number
  method: string
  probesByRtt: ProbeCity[]
  measuredAt: string
  estimate: Estimate | null
}

export interface SnapSite {
  site: string
  country: string
  lat: number
  lon: number
  fits: boolean
}

export interface IpRecord {
  ip: string
  hostname: string | null
  city: string | null
  region: string | null
  country: string | null
  loc: string | null
  hostnameSampledFrom: string | null
  location: {
    country: string | null
    city: string | null
    citySource: string | null
    cityConfidence: 'none' | 'low' | 'medium' | 'high'
    lat: number | null
    lon: number | null
    hub: Hub | null
    measured: Measured | null
    kind: string
    ptrInference: boolean
    prior: { basis: string; city: string; country: string | null; lat: number; lon: number } | null
    corroboration: { source: string; result: string; distanceKm: number } | null
    evidence: Evidence[] | null
    snap: {
      provider: string
      site: string
      probes: number
      fitting: number
      sites: SnapSite[]
    } | null
  }
  org: string | null
  asn: {
    asn: string
    name: string | null
    handle: string | null
    domain: string | null
    country: string | null
    route: string
    type: string | null
    domainMatchesOperator?: boolean | null
  } | null
  timezone?: { id: string; basis: string } | null
  carrier: { name: string; asn: string } | null
  connection: {
    type: string | null
    mobile: Claim
    hosting: Claim
    networkType: string | null
    sampleHostname: string | null
    shared: boolean
    satellite: Claim
  }
  accuracyRadiusKm: number | null
  registry: {
    rir: string
    status: string
    country: string | null
    range: string
    allocated: string | null
  } | null
  routing: {
    prefix: string
    origins: {
      asn: number
      name: string | null
      peers: number
      upstream: number | null
      upstreamName: string | null
      rpki: string
      streaming: boolean
    }[]
    moas: boolean
    firstSeen: string
    lastSeen: string
  } | null
  privacy: {
    vpn: Claim
    proxy: Claim
    tor: Claim
    relay: Claim
    hosting: Claim
    service: string | null
    listsChecked: string[] | null
  }
  cloud: {
    provider: string
    region: string | null
    service: string | null
    range: string
    basis: string
  } | null
  lists: {
    provider: string
    category: string
    range: string
    region: string | null
    service: string | null
  }[]
  observed: {
    prefix: string
    countries: { country: string; routers: number; minRtt: number }[]
    routerAddresses: number
    probes: number
    anycast: boolean
    registryCountry: string | null
    registryContradiction: boolean
    asnHubs:
      | {
      city: string
      country: string | null
      lat: number
      lon: number
      routers: number
      minRtt: number
      weight: number
      named: boolean
    }[]
      | null
  } | null
  bogon: boolean
  special: string | null
  asOf: { routing: string | null; registry: string | null; indexLoaded: string }
  density: {
    prefix: string
    sampled: number
    answered: number
    fraction: number
    reading: string
    ttlReading: string | null
    minRttMs: number | null
    medianRttMs: number | null
    withinKm: number | null
    vantage: { site: string; location: string | null; lat: number | null; lon: number | null }
    cityConsistent: boolean | null
    sampledAt: string
  } | null
  servedFrom?: { site: string; location: string | null } | null
  requestId?: string | null
}

export interface SvirfdNarrativeGroup {
  id: string
  title: string
  paragraphs: string[]
}

export interface SvirfdLookupDto {
  ip: string
  recordJson: string
  headline: string | null
  narrative: SvirfdNarrativeGroup[]
  narrativeVersion: number
  attribution: string
  licenceUrl: string
  mode: string
  seq: number
  rttChartSvg?: string | null
}

export interface SvirfdLookup extends Omit<SvirfdLookupDto, 'recordJson'> {
  record: IpRecord
}

export interface SvirfdStatus {
  enabled: boolean
  ready: boolean
  mode: string
  seq: number
  remoteEngineLookup: boolean
  tenantRemoteLookupsEnabled: boolean
  attribution: string
  licenceUrl: string
  state: string
  lastError: string
  nextRetryAt: string | null
  stalenessSeconds: number | null
}

export interface DriftEntry {
  path: string
  stored: string | null
  fresh: string | null
}

export interface JournalSvirfdDto {
  field: string
  ip: string
  storedJson: string | null
  storedAt: string | null
  fresh: SvirfdLookupDto | null
  drift: DriftEntry[]
}

export interface JournalSvirfd {
  field: string
  ip: string
  stored: IpRecord | null
  storedAt: string | null
  fresh: SvirfdLookup | null
  drift: DriftEntry[]
}
