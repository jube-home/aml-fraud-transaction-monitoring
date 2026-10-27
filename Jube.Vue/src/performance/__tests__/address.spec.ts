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
import {
  decodeAddress,
  emptyAddress,
  encodeAddress,
  levelOf,
  levelsOf,
  normalise,
  readAddress,
  reopen,
} from '../address'

describe('performance address', () => {
  it('round trips a family and an area', () => {
    const address = { family: 'Engine', area: 'HttpProcessingCounter' }

    expect(encodeAddress(address)).toBe('family:Engine,area:HttpProcessingCounter')
    expect(decodeAddress('family:Engine,area:HttpProcessingCounter')).toEqual(address)
  })

  it('writes only the family when no area is chosen', () => {
    expect(encodeAddress({ family: 'Logs', area: '' })).toBe('family:Logs')
    expect(encodeAddress(emptyAddress)).toBe('')
  })

  it('infers the family from an area and corrects a mismatch', () => {
    expect(decodeAddress('area:RedisMetric')).toEqual({ family: 'Redis', area: 'RedisMetric' })
    expect(decodeAddress('family:Logs,area:RedisMetric')).toEqual({
      family: 'Redis',
      area: 'RedisMetric',
    })
  })

  it('drops anything it does not know', () => {
    expect(decodeAddress('family:Nope,area:Nothing')).toEqual(emptyAddress)
    expect(decodeAddress('garbage,,:,x')).toEqual(emptyAddress)
    expect(decodeAddress(undefined)).toEqual(emptyAddress)
    expect(normalise({ family: 'Engine', area: 'Nothing' })).toEqual({ family: 'Engine', area: '' })
  })

  it('reads the first path value from a query', () => {
    expect(readAddress({ path: ['family:Postgres', 'x'] })).toEqual({
      family: 'Postgres',
      area: '',
    })
    expect(readAddress({})).toEqual(emptyAddress)
  })

  it('has one beat per level and reopens upwards', () => {
    const address = { family: 'Engine', area: 'CaptureQueueHealth' }

    expect(levelOf(emptyAddress)).toBe('families')
    expect(levelOf({ family: 'Engine', area: '' })).toBe('areas')
    expect(levelsOf(address)).toEqual(['families', 'areas', 'grid'])
    expect(reopen(address, 1)).toEqual({ family: 'Engine', area: '' })
    expect(reopen(address, 0)).toEqual(emptyAddress)
  })
})
