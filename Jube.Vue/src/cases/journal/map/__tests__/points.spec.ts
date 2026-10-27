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
import { parseActivation } from '../../../../watcher/parse'
import type { JournalRow } from '../../types'
import { onImpossibleLegs, toRawPoints } from '../points'

const t = (key: string, values?: Record<string, unknown>) =>
  values ? `${key} ${JSON.stringify(values)}` : key

const location = { latitude: 'Lat', longitude: 'Lon' }
const lanes = [
  { key: 'AccountId', value: 'A1' },
  { key: 'Device', value: 'D9' },
]

const row = (extra: Record<string, unknown>): JournalRow =>
  ({
    Id: 1,
    EntityAnalysisModelInstanceEntryGuid: 'g1',
    ReferenceDate: '2026-10-19T09:00:00Z',
    ResponseElevation: 3,
    Activations: '',
    ...extra,
  }) as JournalRow

describe('replay points', () => {
  it('orders by time and gives each point the previous located point of its lane', () => {
    const points = toRawPoints(
      [
        row({
          Id: 3,
          EntityAnalysisModelInstanceEntryGuid: 'g3',
          ReferenceDate: '2026-10-21T09:00:00Z',
          Lat: 48.8,
          Lon: 2.3,
        }),
        row({ Id: 1, Lat: 51.5, Lon: -0.12 }),
        row({
          Id: 2,
          EntityAnalysisModelInstanceEntryGuid: 'g2',
          ReferenceDate: '2026-10-20T09:00:00Z',
          Lat: null,
          Lon: null,
        }),
      ],
      location,
      lanes,
      t,
    )

    expect(points.map((point) => point.id)).toEqual(['g1', 'g2', 'g3'])
    expect(points[0]?.previousLatitude).toBeUndefined()
    expect(points[2]).toMatchObject({
      previousLatitude: 51.5,
      previousLongitude: -0.12,
      previousCreatedDate: '2026-10-19T09:00:00Z',
    })
  })

  it('draws each lane as its own trail', () => {
    const points = toRawPoints(
      [
        row({ Lat: 51.5, Lon: -0.12 }),
        row({
          Id: 2,
          EntityAnalysisModelInstanceEntryGuid: 'g2',
          ReferenceDate: '2026-10-20T09:00:00Z',
          Lane: 'B',
          Lat: 40.7,
          Lon: -74,
        }),
        row({
          Id: 3,
          EntityAnalysisModelInstanceEntryGuid: 'g3',
          ReferenceDate: '2026-10-21T09:00:00Z',
          Lane: 'AB',
          Lat: 48.8,
          Lon: 2.3,
        }),
      ],
      location,
      lanes,
      t,
    )

    expect(points.map((point) => [point.key, point.keyValue])).toEqual([
      ['AccountId', 'A1'],
      ['Device', 'D9'],
      ['AccountId', 'A1'],
    ])
    expect(points[2]?.previousLatitude).toBe(51.5)
    expect(points[1]?.previousLatitude).toBeUndefined()
  })

  it('leaves a transaction with no usable location unlocated for the map to count', () => {
    const parsed = [
      row({ Lat: null, Lon: null }),
      row({ Id: 2, EntityAnalysisModelInstanceEntryGuid: 'g2', Lat: 0, Lon: 0 }),
      row({ Id: 3, EntityAnalysisModelInstanceEntryGuid: 'g3', Lat: 'x', Lon: 1 }),
      row({ Id: 4, EntityAnalysisModelInstanceEntryGuid: 'g4', Lat: 120, Lon: 1 }),
      row({ Id: 5, EntityAnalysisModelInstanceEntryGuid: 'g5', Lat: '51.5', Lon: '-0.1' }),
    ]
      .map((item) => toRawPoints([item], location, lanes, t)[0])
      .map((raw) => parseActivation(raw))

    expect(parsed.map((point) => point?.located)).toEqual([false, false, false, false, true])
  })

  it('says why a transaction is highlighted and carries its colours', () => {
    const [point] = toRawPoints(
      [
        row({
          Lat: 51.5,
          Lon: -0.12,
          Activations: 'Velocity',
          BoldLine: {
            BoldLineKey: 'Device',
            BoldLineValue: 'D9',
            BoldLineFormatForeColor: '#000000',
            BoldLineFormatBackColor: '#ffe9a8',
          },
        }),
      ],
      location,
      lanes,
      t,
    )

    expect(point?.activationRuleSummary).toContain('Velocity')
    expect(point?.activationRuleSummary).toContain('journal.format.bold')
    expect(point).toMatchObject({
      backColor: '#ffe9a8',
      foreColor: '#000000',
      responseElevation: 3,
    })
  })

  describe('improbable legs only', () => {
    const at = (
      hours: number,
      latitude: number,
      longitude: number,
      over: Record<string, unknown> = {},
    ) => ({
      id: `p${hours}-${latitude}`,
      key: 'AccountId',
      keyValue: 'A1',
      latitude,
      longitude,
      createdDate: new Date(Date.UTC(2026, 9, 20, 9 + hours)).toISOString(),
      ...over,
    })

    const ids = (points: Record<string, unknown>[]) => points.map((point) => point.id)

    it('keeps both ends of a leg that could not have been travelled and drops the rest', () => {
      const london = at(0, 51.5074, -0.1278)
      const paris = at(1, 48.8566, 2.3522)
      const sydney = at(2, -33.8688, 151.2093)
      const sydneyAgain = at(30, -33.9, 151.2)

      expect(ids(onImpossibleLegs([london, paris, sydney, sydneyAgain], 900, 50))).toEqual(
        ids([paris, sydney]),
      )
    })

    it('follows the limits it is given', () => {
      const points = [at(0, 51.5074, -0.1278), at(1, 48.8566, 2.3522)]

      expect(onImpossibleLegs(points, 900, 50)).toEqual([])
      expect(onImpossibleLegs(points, 300, 50)).toHaveLength(2)
      expect(onImpossibleLegs(points, 300, 400)).toEqual([])
    })

    it('keeps each key to its own trail and ignores what has no usable location', () => {
      const here = at(0, 51.5074, -0.1278)
      const there = at(1, 48.8566, 2.3522, { keyValue: 'D9', id: 'other' })
      const nowhere = at(1, 0, 0, { id: 'nowhere' })
      const missing = at(1, 0, 0, { id: 'missing', latitude: null, longitude: null })

      expect(onImpossibleLegs([here, there, nowhere, missing], 300, 50)).toEqual([])
    })

    it('counts two places at the same instant as a leg that could not happen', () => {
      const points = [at(0, 51.5074, -0.1278), at(0, 48.8566, 2.3522, { id: 'same' })]

      expect(onImpossibleLegs(points, 900, 50)).toHaveLength(2)
    })
  })
  it('draws the rows the filter left out dim and never lets them feed a trail', () => {
    const points = toRawPoints(
      [
        row({ Id: 1, Lat: 51.5, Lon: -0.12 }),
        row({
          Id: 2,
          EntityAnalysisModelInstanceEntryGuid: 'g2',
          ReferenceDate: '2026-10-20T09:00:00Z',
          Lat: 10,
          Lon: 10,
          Matched: false,
        }),
        row({
          Id: 3,
          EntityAnalysisModelInstanceEntryGuid: 'g3',
          ReferenceDate: '2026-10-21T09:00:00Z',
          Lat: 48.8,
          Lon: 2.3,
          Matched: true,
        }),
      ],
      location,
      lanes,
      t,
    )

    expect(points.map((point) => point.dim)).toEqual([false, true, false])
    expect(points[1]).toMatchObject({ previousLatitude: undefined })
    expect(points[2]).toMatchObject({ previousLatitude: 51.5, previousLongitude: -0.12 })

    const activation = parseActivation(points[1])

    expect(activation?.dim).toBe(true)
    expect(parseActivation(points[0])?.dim).toBe(false)
  })

  it('keeps the dim rows when only the improbable legs are asked for', () => {
    const points = toRawPoints(
      [
        row({ Id: 1, Lat: 51.5, Lon: -0.12 }),
        row({
          Id: 2,
          EntityAnalysisModelInstanceEntryGuid: 'g2',
          ReferenceDate: '2026-10-19T10:00:00Z',
          Lat: -33.8,
          Lon: 151.2,
        }),
        row({
          Id: 3,
          EntityAnalysisModelInstanceEntryGuid: 'g3',
          ReferenceDate: '2026-10-20T09:00:00Z',
          Lat: 1,
          Lon: 1,
          Matched: false,
        }),
        row({
          Id: 4,
          EntityAnalysisModelInstanceEntryGuid: 'g4',
          ReferenceDate: '2026-10-20T10:00:00Z',
          Lat: 51.4,
          Lon: -0.1,
        }),
      ],
      location,
      lanes,
      t,
    )

    expect(onImpossibleLegs(points, 900, 50).map((point) => point.id)).toEqual(['g1', 'g2', 'g3'])
  })
})
