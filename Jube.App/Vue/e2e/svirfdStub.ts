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

export const svirfdRecord = {
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
}

export const svirfdLookup = {
  ip: '8.8.8.8',
  recordJson: JSON.stringify(svirfdRecord),
  headline: 'Google LLC, United States',
  narrative: [
    {id: 'identity', title: 'Identity', paragraphs: ['8.8.8.8 belongs to Google LLC.']},
  ],
  narrativeVersion: 4,
  attribution: 'Data: svirfd.com, built from RIR delegated statistics.',
  licenceUrl: 'https://svirfd.com/terms',
  mode: 'Remote',
  seq: 0,
}

export const svirfdFreshLookup = {
  ...svirfdLookup,
  recordJson: JSON.stringify({
    ...svirfdRecord,
    asn: {...svirfdRecord.asn, name: 'Google Cloud'},
  }),
}

export const wireSchema = {
  hash: 'e2e',
  mode: 'Json',
  generatedDecoders: false,
  encryption: false,
  types: {},
  routes: [],
}
