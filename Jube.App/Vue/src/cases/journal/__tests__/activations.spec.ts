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
import {bandKey, ownCards, parseSort, sortCards} from '../activations'
import type {ActivationCard} from '../types'

function card(overrides: Partial<ActivationCard>): ActivationCard {
  return {
    kind: 'rule',
    source: null,
    name: 'Velocity',
    humanLabel: null,
    fired: 1,
    forced: false,
    shadow: false,
    anchorId: 1,
    anchorDate: '2026-10-01T00:00:00Z',
    value: null,
    threshold: null,
    calibrated: false,
    bandId: null,
    observedRate: null,
    expectedRate: null,
    bandSample: null,
    modelName: null,
    modelVersion: null,
    drivers: [],
    otherDrivers: 0,
    path: [],
    hiddenPath: 0,
    ...overrides,
  }
}

const cards = [
  card({name: 'Velocity', fired: 5, anchorDate: '2026-10-03T00:00:00Z'}),
  card({name: 'Sanctioned', fired: 9, anchorDate: '2026-10-01T00:00:00Z'}),
  card({kind: 'model', source: 'exhaustive', name: 'StructNet', value: 0.7, fired: 2}),
  card({kind: 'model', source: 'advanced', name: 'Mule', value: 0.95, fired: 1}),
]

describe('activation cards', () => {
  it('sorts by score with models first, highest score first, then rules by how often they fired', () => {
    expect(sortCards(cards, 'score').map((item) => item.name)).toEqual([
      'Mule',
      'StructNet',
      'Sanctioned',
      'Velocity',
    ])
  })

  it('sorts by how often, how recently and by name', () => {
    expect(sortCards(cards, 'fired').map((item) => item.name)[0]).toBe('Sanctioned')
    expect(sortCards(cards, 'recent').map((item) => item.name)[0]).toBe('Velocity')
    expect(sortCards(cards, 'name').map((item) => item.name)).toEqual([
      'Mule',
      'Sanctioned',
      'StructNet',
      'Velocity',
    ])
  })

  it('does not reorder the list it was given', () => {
    const before = cards.map((item) => item.name)

    sortCards(cards, 'name')

    expect(cards.map((item) => item.name)).toEqual(before)
  })

  it('narrows to the cards named by the case without regard to case, and only when asked', () => {
    expect(ownCards(cards, ['velocity'], true).map((item) => item.name)).toEqual(['Velocity'])
    expect(ownCards(cards, ['velocity'], false)).toHaveLength(4)
    expect(ownCards(cards, [], true)).toEqual([])
  })

  it('reads a sort it does not know as score and a band it does not know as none', () => {
    expect(parseSort('fired')).toBe('fired')
    expect(parseSort('nonsense')).toBe('score')
    expect(parseSort(null)).toBe('score')
    expect(bandKey(4)).toBe('veryLikely')
    expect(bandKey(1)).toBe('unlikely')
    expect(bandKey(9)).toBeNull()
    expect(bandKey(null)).toBeNull()
  })
})
