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

import {describe, expect, it} from 'vitest'
import {journalFrom, lookupFrom, parseRecord} from '../convert'
import {changedPaths, isChanged} from '../drift'
import {claimKey, confidenceTone, kmBound, rpkiTone} from '../format'
import {locationFromParams, locationKey, paramsFromLocation} from '../location'
import {legendOf, recordPoints, recordRings} from '../points'
import {facts, sectionIds, valueAt} from '../sections'
import type {IpRecord, SvirfdStatus} from '../types'
import {remoteBlocked, tenantRemoteOff} from '../status'
import {canLookup, isIpLiteral} from '../validate'
import {ringPolygon, ringsGeoJson} from '../../watcher/rings'

const record = {
  ip: '8.8.8.8',
  hostname: 'ns.example.test',
  city: null,
  region: null,
  country: 'US',
  loc: null,
  hostnameSampledFrom: null,
  location: {
    country: 'US',
    city: 'Ashburn',
    citySource: 'dataCentrePrior',
    cityConfidence: 'low',
    lat: 39.04,
    lon: -77.49,
    hub: null,
    measured: {
      prefix: '8.8.8.0/24',
      city: null,
      rttMs: 3.66,
      probes: 2,
      targetAnswered: false,
      edgeOnly: true,
      depth: 0,
      method: 'traceroute',
      probesByRtt: [
        {
          city: 'Los Angeles',
          country: 'US',
          rtt: 3.66,
          reached: false,
          depth: 0,
          lastHop: null,
          lastHopAsn: null,
          lat: 34.05,
          lon: -118.24,
        },
        {
          city: 'Nowhere',
          country: null,
          rtt: 9,
          reached: true,
          depth: 1,
          lastHop: null,
          lastHopAsn: null,
          lat: null,
          lon: null,
        },
      ],
      measuredAt: '2026-10-31T22:13:36Z',
      estimate: null,
    },
    kind: 'registryCountry',
    ptrInference: false,
    prior: null,
    corroboration: null,
    evidence: [{source: 'registry', detail: 'arin lists 8.8.8.0/24'}],
    snap: null,
  },
  org: 'AS15169 Google LLC',
  asn: {
    asn: 'AS15169',
    name: 'Google LLC',
    handle: 'GOOGLE',
    domain: null,
    country: 'US',
    route: '8.8.8.0/24',
    type: 'hosting',
  },
  carrier: null,
  connection: {
    type: 'hosting',
    mobile: false,
    hosting: true,
    networkType: 'hosting',
    sampleHostname: null,
    shared: false,
    satellite: null,
  },
  accuracyRadiusKm: 50,
  registry: null,
  routing: {
    prefix: '8.8.8.0/24',
    origins: [
      {
        asn: 15169,
        name: 'Google LLC',
        peers: 17,
        upstream: 6939,
        upstreamName: 'Hurricane Electric LLC',
        rpki: 'valid',
        streaming: false,
      },
    ],
    moas: false,
    firstSeen: '2026-10-31T16:31:10Z',
    lastSeen: '2026-10-31T16:31:10Z',
  },
  privacy: {
    vpn: false,
    proxy: null,
    tor: false,
    relay: false,
    hosting: true,
    service: null,
    listsChecked: ['Mullvad', 'Tor Project'],
  },
  cloud: null,
  lists: [],
  observed: null,
  bogon: false,
  special: null,
  asOf: {routing: null, registry: null, indexLoaded: '2026-11-06T05:30:14Z'},
  density: null,
} as unknown as IpRecord

describe('svirfd module', () => {
  it('reads the address from the location and writes it back', () => {
    expect(paramsFromLocation({ip: ' 8.8.8.8 '})).toEqual({ip: '8.8.8.8'})
    expect(paramsFromLocation({})).toEqual({ip: ''})
    expect(locationFromParams({ip: '8.8.8.8'})).toEqual({ip: '8.8.8.8'})
    expect(locationFromParams({ip: '  '})).toEqual({})
    expect(locationKey({ip: ' 1.1.1.1 '})).toBe('1.1.1.1')
  })

  it('accepts addresses and refuses everything else', () => {
    for (const good of ['8.8.8.8', '203.0.113.9', '2001:4860:4860::8888', '::1'])
      expect(isIpLiteral(good)).toBe(true)

    for (const bad of ['', 'abc', '123.456.789.200', '8.8.8', '8.8.8.8.8', 'host.example'])
      expect(canLookup(bad)).toBe(false)
  })

  it('never renders a null claim as false', () => {
    expect(claimKey(null)).toBe('notClaimed')
    expect(claimKey(undefined)).toBe('notClaimed')
    expect(claimKey(false)).toBe('no')
    expect(claimKey(true)).toBe('yes')
  })

  it('tones confidence and rpki', () => {
    expect(confidenceTone('high')).toBe('success')
    expect(confidenceTone('none')).toBe('neutral')
    expect(rpkiTone('invalid')).toBe('error')
    expect(kmBound(3.66)).toBeCloseTo(366)
  })

  it('reads the dotted paths of the fact table including the first origin', () => {
    expect(valueAt(record, 'Asn.Name')).toBe('Google LLC')
    expect(valueAt(record, 'Routing.Origin.Rpki')).toBe('valid')
    expect(valueAt(record, 'Routing.Origin.Peers')).toBe(17)
    expect(valueAt(record, 'Cloud.Provider')).toBeNull()
    expect(valueAt(record, 'Privacy.Proxy')).toBeNull()
    expect(new Set(facts.map((fact) => fact.section))).toEqual(new Set(sectionIds))
  })

  it('draws located items and trails and leaves the unlocated out', () => {
    const rows = recordPoints(record)

    expect(rows.map((row) => row.id)).toEqual(['location:0', 'probe:0'])
    expect(rows[1]).toMatchObject({previousLatitude: 39.04, previousLongitude: -77.49})
    expect(legendOf(rows).map((entry) => entry.kind)).toEqual(['location', 'probe'])
  })

  it('marks an archived location that differs as a trail', () => {
    const stored = {
      ...record,
      location: {...record.location, lat: 52.23, lon: 21.01, city: 'Warsaw'},
    } as IpRecord
    const rows = recordPoints(record, stored)

    expect(rows.at(-1)).toMatchObject({id: 'storedLocation:0', previousLatitude: 39.04})
  })

  it('bounds every probe with its round trip and adds the accuracy radius', () => {
    const rings = recordRings(record)

    expect(rings.map((ring) => ring.id)).toEqual(['probe:0', 'accuracy'])
    expect(rings[0]?.radiusKm).toBeCloseTo(366)
  })

  it('builds closed geodesic rings', () => {
    const polygon = ringPolygon({
      id: 'a',
      latitude: 0,
      longitude: 0,
      radiusKm: 111.195,
      colour: '#000',
      label: '',
    })

    expect(polygon).toHaveLength(65)
    expect(polygon[0]).toEqual(polygon.at(-1))
    expect(Math.max(...polygon.map(([, lat]) => lat))).toBeCloseTo(1, 1)
    expect(
      ringsGeoJson([{id: 'x', latitude: 0, longitude: 0, radiusKm: 0, colour: '', label: ''}])
        .features,
    ).toEqual([])
  })

  it('parses a record and a lookup, and reports drift paths case-insensitively', () => {
    const json = JSON.stringify(record)

    expect(parseRecord(json)?.ip).toBe('8.8.8.8')
    expect(parseRecord('not json')).toBeNull()
    expect(parseRecord(null)).toBeNull()

    const lookup = lookupFrom({
      ip: '8.8.8.8',
      recordJson: json,
      headline: 'h',
      narrative: [],
      narrativeVersion: 4,
      attribution: 'credit',
      licenceUrl: 'u',
      mode: 'Remote',
      seq: -1,
    })

    expect(lookup.record.ip).toBe('8.8.8.8')
    expect(() => lookupFrom({...lookup, recordJson: 'nope'} as never)).toThrow()

    const journal = journalFrom({
      field: 'IP',
      ip: '8.8.8.8',
      storedJson: json,
      storedAt: null,
      fresh: null,
      drift: [{path: 'Privacy.Vpn', stored: 'false', fresh: 'true'}],
    })

    expect(journal.stored?.ip).toBe('8.8.8.8')
    expect(isChanged(changedPaths(journal.drift), 'privacy.vpn')).toBe(true)
    expect(isChanged(changedPaths(null), 'Privacy.Vpn')).toBe(false)
  })

  it('blocks remote lookups only when the tenant switch is off and the mode is Remote', () => {
    const status: SvirfdStatus = {
      enabled: true,
      ready: true,
      mode: 'Remote',
      seq: -1,
      remoteEngineLookup: true,
      tenantRemoteLookupsEnabled: false,
      attribution: '',
      licenceUrl: '',
      state: 'Remote',
      lastError: '',
      nextRetryAt: null,
      stalenessSeconds: null,
    }

    expect(tenantRemoteOff(status)).toBe(true)
    expect(remoteBlocked(status)).toBe(true)
    expect(remoteBlocked({...status, mode: 'Directory'})).toBe(false)
    expect(tenantRemoteOff({...status, mode: 'Directory'})).toBe(true)
    expect(tenantRemoteOff({...status, tenantRemoteLookupsEnabled: true})).toBe(false)
    expect(remoteBlocked(undefined)).toBe(false)
    expect(tenantRemoteOff(undefined)).toBe(false)
  })
})
