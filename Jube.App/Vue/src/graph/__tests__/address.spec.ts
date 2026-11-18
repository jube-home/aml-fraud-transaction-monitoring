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
import {
  defaultGraphAddress,
  graphAddressKeys,
  graphAddressText,
  mergeGraphAddress,
  parseGraphAddress,
  parseGraphAddressText,
  serialiseGraphAddress,
  type GraphAddress,
} from '../address'

const full: GraphAddress = {
  view: 'relevance',
  hops: 3,
  layout: 3,
  edges: ['def-a', 'def-b'],
  expanded: ['12', '44'],
  selected: '44',
  flow: {seed: '12', direction: 'backward'},
  pattern: 'p-9',
  registry: 'reg-guid',
  seedKey: 'AccountId',
  seedValue: 'ACC-441',
  cursor: 1_760_000_000,
  explode: true,
  flowWindowHours: 48,
  flowTolerance: 0.35,
  flowArchiveId: 9001,
  pathsTo: '77',
  community: '812',
}

describe('graph address', () => {
  it('has the defaults for an empty address and writes nothing for them', () => {
    expect(parseGraphAddress({})).toEqual(defaultGraphAddress)
    expect(serialiseGraphAddress(defaultGraphAddress)).toEqual({})
  })

  it('round trips every key', () => {
    const written = serialiseGraphAddress(full)

    expect(Object.keys(written).sort()).toEqual([...graphAddressKeys].sort())
    expect(parseGraphAddress(written)).toEqual(full)
    expect(parseGraphAddressText(graphAddressText(full))).toEqual(full)
  })

  it('writes the short forms the design names', () => {
    expect(serialiseGraphAddress(full)).toEqual({
      gv: 'r',
      gh: '3',
      gl: '3',
      ge: 'def-a,def-b',
      gx: '12,44',
      gn: '44',
      gf: '12:b',
      gp: 'p-9',
      gr: 'reg-guid',
      gk: 'AccountId',
      gq: 'ACC-441',
      gt: '1760000000',
      gd: '1',
      gw: '48',
      go: '0.35',
      ga: '9001',
      gy: '77',
      gc: '812',
    })
  })

  it('reads a forward flow without a direction suffix and a seed holding colons', () => {
    expect(parseGraphAddress({gf: '12'}).flow).toEqual({seed: '12', direction: 'forward'})
    expect(parseGraphAddress({gf: 'a:b:f'}).flow).toEqual({seed: 'a:b', direction: 'forward'})
    expect(parseGraphAddress({gf: ':b'}).flow).toBeNull()
  })

  it('reads hostile or malformed values as the defaults and never throws', () => {
    for (const hostile of ["' OR '1'='1", '<script>', '-1', '9999999999999999999', '', 'NaN']) {
      const parsed = parseGraphAddress({
        gv: hostile,
        gh: hostile,
        gl: hostile,
        gt: hostile,
        gd: hostile,
      })

      expect(parsed.view).toBe('hops')
      expect(parsed.hops).toBeNull()
      expect(parsed.layout).toBeNull()
      expect(parsed.cursor).toBeNull()
      expect(parsed.explode).toBe(false)
    }

    expect(parseGraphAddress({gh: ['3', '1']}).hops).toBe(3)
    expect(parseGraphAddress({gh: '2'}).hops).toBe(2)
    expect(serialiseGraphAddress({...defaultGraphAddress, hops: 2})).toEqual({gh: '2'})
    expect(parseGraphAddress({gv: 'f'}).view).toBe('flow')
    expect(parseGraphAddress({gw: '0'}).flowWindowHours).toBeNull()
    expect(parseGraphAddress({gw: '721'}).flowWindowHours).toBeNull()
    expect(parseGraphAddress({gw: '12.5'}).flowWindowHours).toBeNull()
    expect(parseGraphAddress({go: '0.95'}).flowTolerance).toBeNull()
    expect(parseGraphAddress({go: '0'}).flowTolerance).toBe(0)
    expect(parseGraphAddress({ga: '-5'}).flowArchiveId).toBeNull()
  })

  it('caps the lists and drops duplicates and blanks', () => {
    const many = Array.from({length: 40}, (_, index) => `n${index}`).join(',')

    expect(parseGraphAddress({gx: many}).expanded).toHaveLength(20)
    expect(parseGraphAddress({ge: 'a,,a, b '}).edges).toEqual(['a', 'b'])
  })

  it('merges into an address keeping unrelated keys and dropping stale graph keys', () => {
    const merged = mergeGraphAddress(
      {gn: '1', gx: '1,2', jw: '30', section: 'network'},
      {...defaultGraphAddress, selected: '7'},
    )

    expect(merged).toEqual({jw: '30', section: 'network', gn: '7'})
  })
})
