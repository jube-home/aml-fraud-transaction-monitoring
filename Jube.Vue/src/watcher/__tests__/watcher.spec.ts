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

import { describe, expect, it } from 'vitest'
import { haversineKm, segmentOf } from '../geo'
import { arcBetween, pointsGeoJson, trailsGeoJson } from '../geojson'
import { parseActivation, safeColour } from '../parse'
import {
  ageOpacity,
  joinable,
  locatedOnly,
  emptyState,
  ingest,
  latestPerEntity,
  ruleList,
  trailsOf,
  withinWindow,
} from '../state'
import type { WatcherOptions } from '../types'

const options: WatcherOptions = { mode: 'latest', maxRows: 3 }

const raw = (overrides: Record<string, unknown> = {}) => ({
  key: 'AccountId',
  keyValue: 'ACC-1',
  latitude: 51.5,
  longitude: -0.12,
  activationRuleSummary: 'High value',
  responseElevation: 5,
  responseElevationContent: 'Review',
  backColor: '#CC6E38',
  foreColor: '#ffffff',
  createdDate: '2026-10-21T10:00:00Z',
  ...overrides,
})

const make = (overrides: Record<string, unknown> = {}, sequence = 0) =>
  parseActivation(raw({ id: sequence, ...overrides }))!

describe('parseActivation', () => {
  it('reads either casing, builds the entity and keeps only safe colours', () => {
    const activation = parseActivation({
      Key: 'K',
      KeyValue: 'V',
      Latitude: 1,
      Longitude: 2,
      BackColor: 'url(x)',
    })!

    expect(activation.entity).toBe('K:V')
    expect(activation.backColor).toBeNull()
    expect(make().backColor).toBe('#cc6e38')
  })

  it('marks an activation without a usable position as unlocated rather than dropping it', () => {
    expect(parseActivation(null)).toBeNull()
    expect(parseActivation(raw({ latitude: null }))!.located).toBe(false)
    expect(parseActivation(raw({ longitude: undefined }))!.located).toBe(false)
    expect(parseActivation(raw({ latitude: 91 }))!.located).toBe(false)
    expect(parseActivation(raw({ longitude: -181 }))!.located).toBe(false)
    expect(parseActivation(raw({ latitude: 'north' }))!.located).toBe(false)
    expect(parseActivation(raw({ latitude: 91 }))!.latitude).toBe(0)
  })

  it('treats zero zero as no location, which is what the engine writes when the model has none', () => {
    const none = parseActivation(raw({ latitude: 0, longitude: 0 }))!

    expect(none.located).toBe(false)
    expect(parseActivation(raw({ latitude: 0, longitude: 10 }))!.located).toBe(true)
    expect(parseActivation(raw({ latitude: 10, longitude: 0 }))!.located).toBe(true)
  })
})

describe('the identity of an activation', () => {
  it('is the same when the same thing arrives again, as after a stream reconnects', () => {
    expect(parseActivation(raw())!.id).toBe(parseActivation(raw())!.id)
    expect(parseActivation(raw())!.id).not.toBe(parseActivation(raw({ latitude: 40 }))!.id)
  })
})

describe('safeColour', () => {
  it('only lets hexadecimal colours through', () => {
    expect(safeColour('#fff')).toBe('#fff')
    expect(safeColour(' #ABCDEF ')).toBe('#abcdef')
    expect(safeColour('red')).toBeNull()
    expect(safeColour('#12')).toBeNull()
    expect(safeColour(5)).toBeNull()
  })
})

describe('haversineKm and segmentOf', () => {
  const london = make()
  const paris = make({ latitude: 48.86, longitude: 2.35, createdDate: '2026-10-21T10:30:00Z' })
  const sydney = make({ latitude: -33.87, longitude: 151.21, createdDate: '2026-10-21T10:30:00Z' })

  it('measures great circle distance', () => {
    expect(haversineKm(london, paris)).toBeGreaterThan(330)
    expect(haversineKm(london, paris)).toBeLessThan(350)
    expect(haversineKm(london, london)).toBe(0)
  })

  it('flags travel faster than an aeroplane and not the ordinary kind', () => {
    expect(segmentOf(london, paris).impossible).toBe(false)
    expect(segmentOf(london, sydney).impossible).toBe(true)
    expect(Math.round(segmentOf(london, paris).speedKmh)).toBeGreaterThan(600)
  })

  it('does not flag a short hop even when it is instant', () => {
    const near = make({ latitude: 51.51, createdDate: '2026-10-21T10:00:00Z' }, 1)

    expect(segmentOf(london, near).impossible).toBe(false)
  })
})

describe('ingest', () => {
  it('keeps history in time order, newest first in the rows, and counts rules', () => {
    const a = make({ createdDate: '2026-10-21T10:00:00Z' }, 1)
    const b = make(
      {
        createdDate: '2026-10-21T10:05:00Z',
        activationRuleSummary: 'Velocity',
        responseElevation: 2,
      },
      2,
    )
    const state = ingest(emptyState(), [b, a], options)

    expect(state.history.map((x) => x.id)).toEqual([a.id, b.id])
    expect(state.rows.map((x) => x.id)).toEqual([b.id, a.id])
    expect(ruleList(state)[0]).toEqual({ name: 'High value', frequency: 1, aggregate: 5 })
    expect(state.ingested).toBe(2)
  })

  it('ignores a repeat of something it already holds', () => {
    const a = make({}, 1)
    const once = ingest(emptyState(), [a], options)

    expect(ingest(once, [a], options)).toBe(once)
  })

  it('caps the rows and the history', () => {
    const many = Array.from({ length: 10 }, (_, i) =>
      make({ createdDate: `2026-10-21T10:0${i}:00Z` }, i),
    )
    const state = ingest(emptyState(), many, options, 4)

    expect(state.rows).toHaveLength(3)
    expect(state.history).toHaveLength(4)
    expect(state.ingested).toBe(10)
  })

  it('holds only the entity a host asks for', () => {
    const mine = make({ keyValue: 'ACC-1' }, 1)
    const other = make({ keyValue: 'ACC-2' }, 2)
    const state = ingest(emptyState(), [mine, other], {
      ...options,
      keyFilter: 'AccountId',
      keyValueFilter: 'ACC-1',
    })

    expect(state.history.map((x) => x.keyValue)).toEqual(['ACC-1'])
  })
})

describe('selectors', () => {
  const first = make({ createdDate: '2026-10-21T10:00:00Z' }, 1)
  const second = make({ createdDate: '2026-10-21T10:10:00Z', latitude: 48.86, longitude: 2.35 }, 2)
  const stranger = make({ keyValue: 'ACC-2', createdDate: '2026-10-21T10:02:00Z' }, 3)

  it('keeps the latest activation of each entity', () => {
    expect(
      latestPerEntity([first, second, stranger])
        .map((x) => x.id)
        .sort(),
    ).toEqual([second.id, stranger.id].sort())
  })

  it('joins an entity events in time order and skips a repeat at the same place', () => {
    const repeat = make(
      { createdDate: '2026-10-21T10:11:00Z', latitude: 48.86, longitude: 2.35 },
      4,
    )
    const trails = trailsOf([second, first, stranger, repeat])

    expect(trails).toHaveLength(1)
    expect(trails[0]!.from.createdAt).toBe(first.createdAt)
    expect(trails[0]!.to.id).toBe(second.id)
  })

  it('marks a leg impossible only by the speed and shortest leg it is given', () => {
    const london = make(
      { latitude: 51.5074, longitude: -0.1278, createdDate: '2026-10-21T10:00:00Z' },
      20,
    )
    const paris = make(
      { latitude: 48.8566, longitude: 2.3522, createdDate: '2026-10-21T11:00:00Z' },
      21,
    )

    expect(trailsOf([london, paris])[0]!.impossible).toBe(false)
    expect(trailsOf([london, paris], 300)[0]!.impossible).toBe(true)
    expect(trailsOf([london, paris], 300, 400)[0]!.impossible).toBe(false)
    expect(trailsOf([london, paris], 300, 100)[0]!.impossible).toBe(true)
  })

  it('leaves unlocated activations out of the pins and the trails', () => {
    const nowhere = make({ latitude: 0, longitude: 0, createdDate: '2026-10-21T10:05:00Z' }, 9)

    expect(locatedOnly([first, nowhere]).map((x) => x.id)).toEqual([first.id])
    expect(trailsOf([first, nowhere, second])).toHaveLength(1)
    expect(trailsOf([first, nowhere, second])[0]!.from.createdAt).toBe(first.createdAt)
  })

  it('windows by age and fades with it', () => {
    const now = Date.parse('2026-10-21T10:11:00Z')

    expect(withinWindow([first, second], 5 * 60_000, now).map((x) => x.id)).toEqual([second.id])
    expect(withinWindow([first, second], null, now)).toHaveLength(2)
    expect(ageOpacity(now, now, 60_000)).toBe(1)
    expect(ageOpacity(now - 60_000, now, 60_000)).toBeCloseTo(0.15, 6)
    expect(ageOpacity(now - 30_000, now, 60_000)).toBeCloseTo(0.575, 3)
    expect(ageOpacity(now - 600_000, now, 60_000)).toBe(0.15)
    expect(ageOpacity(0, now, null)).toBe(1)
  })
})

describe('map data', () => {
  const now = Date.parse('2026-10-21T10:00:30Z')
  const recent = make({ createdDate: '2026-10-21T10:00:28Z' }, 1)
  const old = make(
    { createdDate: '2026-10-21T09:00:00Z', backColor: '#ffffff', keyValue: 'ACC-2' },
    2,
  )

  it('turns activations into points, colouring white as the accent and marking the new ones', () => {
    const { features } = pointsGeoJson([recent, old], now, 3_600_000, '#005f7f')

    expect(features[0]!.geometry.coordinates).toEqual([-0.12, 51.5])
    expect(features[0]!.properties.color).toBe('#cc6e38')
    expect(features[0]!.properties.fresh).toBe(true)
    expect(features[1]!.properties.color).toBe('#005f7f')
    expect(features[1]!.properties.fresh).toBe(false)
    expect(features[1]!.properties.opacity).toBeLessThan(features[0]!.properties.opacity)
  })

  it('turns trail segments into lines carrying the travel speed', () => {
    const london = make({ createdDate: '2026-10-21T10:00:00Z' }, 3)
    const sydney = make(
      { latitude: -33.87, longitude: 151.21, createdDate: '2026-10-21T10:30:00Z' },
      4,
    )
    const { features } = trailsGeoJson(
      [segmentOf(london, sydney)],
      Date.parse('2026-10-21T10:30:00Z'),
      900_000,
    )

    const arc = features[0]!.geometry.coordinates

    expect(arc[0]![0]).toBeCloseTo(-0.12, 6)
    expect(arc[0]![1]).toBeCloseTo(51.5, 6)
    expect(arc.at(-1)![0]).toBeCloseTo(151.21, 2)
    expect(arc.at(-1)![1]).toBeCloseTo(-33.87, 2)
    expect(features[0]!.properties.impossible).toBe(true)
    expect(features[0]!.properties.kilometres).toBeGreaterThan(16000)
  })
})

describe('fading and joining', () => {
  it('fades newest to oldest so an overplotted map still reads', () => {
    const now = Date.parse('2026-10-21T10:30:00Z')
    const fresh = make({ createdDate: '2026-10-21T10:29:50Z' }, 1)
    const stale = make({ createdDate: '2026-10-21T10:05:00Z', keyValue: 'ACC-2' }, 2)
    const { features } = pointsGeoJson([fresh, stale], now, 900_000, '#005f7f')

    expect(features[0]!.properties.opacity).toBeGreaterThan(0.95)
    expect(features[1]!.properties.opacity).toBeCloseTo(0.15, 6)
  })

  it('fades an arc by the age of the journey that ended it', () => {
    const now = Date.parse('2026-10-21T10:30:00Z')
    const a = make({ createdDate: '2026-10-21T10:00:00Z' }, 1)
    const b = make({ latitude: 48.86, longitude: 2.35, createdDate: '2026-10-21T10:29:00Z' }, 2)
    const { features } = trailsGeoJson([segmentOf(a, b)], now, 900_000)

    expect(features[0]!.properties.opacity).toBeGreaterThan(0.9)
  })

  it('does not join activations whose key value is missing, which would join strangers', () => {
    const a = make({ keyValue: 'Missing', createdDate: '2026-10-21T10:00:00Z' }, 1)
    const b = make(
      {
        keyValue: 'Missing',
        latitude: 48.86,
        longitude: 2.35,
        createdDate: '2026-10-21T10:10:00Z',
      },
      2,
    )
    const c = make(
      { keyValue: '', latitude: 40, longitude: -74, createdDate: '2026-10-21T10:20:00Z' },
      3,
    )

    expect(joinable(a)).toBe(false)
    expect(joinable(c)).toBe(false)
    expect(joinable(make({}, 4))).toBe(true)
    expect(trailsOf([a, b, c])).toHaveLength(0)
  })

  it('draws an arc along the great circle, unwrapped across the antimeridian', () => {
    const arc = arcBetween({ latitude: 35, longitude: 170 }, { latitude: 35, longitude: -170 })

    expect(arc).toHaveLength(33)
    expect(arc[0]).toEqual([170, 35])
    expect(arc.at(-1)![0]).toBeCloseTo(190, 3)
    expect(Math.max(...arc.map((point) => point[1]))).toBeGreaterThan(35)

    for (let index = 1; index < arc.length; index++) {
      expect(Math.abs(arc[index]![0] - arc[index - 1]![0])).toBeLessThan(20)
    }
  })

  it('gives a single point for a journey of no distance', () => {
    expect(arcBetween({ latitude: 1, longitude: 1 }, { latitude: 1, longitude: 1 })).toEqual([
      [1, 1],
    ])
  })
})

describe('journeys the engine already knows', () => {
  it('reads the previous location the engine recorded', () => {
    const activation = parseActivation(
      raw({
        previousLatitude: 48.86,
        previousLongitude: 2.35,
        previousCreatedDate: '2026-10-21T09:00:00Z',
      }),
    )!

    expect(activation.previous).toEqual({
      latitude: 48.86,
      longitude: 2.35,
      createdAt: Date.parse('2026-10-21T09:00:00Z'),
    })
    expect(parseActivation(raw())!.previous).toBeNull()
    expect(
      parseActivation(
        raw({
          previousLatitude: 0,
          previousLongitude: 0,
          previousCreatedDate: '2026-10-21T09:00:00Z',
        }),
      )!.previous,
    ).toBeNull()
    expect(
      parseActivation(raw({ previousLatitude: 48.86, previousLongitude: 2.35 }))!.previous,
    ).toBeNull()
  })

  it('draws the journey from the engine record even when the earlier activation was never sampled', () => {
    const lone = make(
      {
        latitude: -33.87,
        longitude: 151.21,
        createdDate: '2026-10-21T10:30:00Z',
        previousLatitude: 51.5,
        previousLongitude: -0.12,
        previousCreatedDate: '2026-10-21T10:00:00Z',
      },
      1,
    )

    const trails = trailsOf([lone])

    expect(trails).toHaveLength(1)
    expect(trails[0]!.impossible).toBe(true)
    expect(trails[0]!.from.latitude).toBe(51.5)
  })

  it('does not draw a journey that went nowhere, and still chains activations that carry no record', () => {
    const still = make(
      {
        previousLatitude: 51.5,
        previousLongitude: -0.12,
        previousCreatedDate: '2026-10-21T09:00:00Z',
      },
      1,
    )
    const older = make({ createdDate: '2026-10-21T10:00:00Z', keyValue: 'ACC-9' }, 2)
    const newer = make(
      { latitude: 48.86, longitude: 2.35, createdDate: '2026-10-21T10:20:00Z', keyValue: 'ACC-9' },
      3,
    )

    expect(trailsOf([still])).toHaveLength(0)
    expect(trailsOf([older, newer])).toHaveLength(1)
  })
})
