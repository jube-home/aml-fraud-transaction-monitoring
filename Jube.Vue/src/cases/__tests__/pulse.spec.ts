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
import { againstTypical, dayState, ringValue, sparkPaths } from '../pulse'

describe('sparkPaths', () => {
  it('draws a flat baseline for an empty series and no area', () => {
    expect(sparkPaths([])).toEqual({ line: 'M0 22H100', area: '' })
  })

  it('puts the largest value at the top and the smallest on the floor', () => {
    const { line } = sparkPaths([0, 10], 100, 24, 2)

    expect(line).toBe('M0 22L100 2')
  })

  it('closes the area down to the floor', () => {
    const { area } = sparkPaths([1, 2, 3])

    expect(area.endsWith('L100 22L0 22Z')).toBe(true)
  })

  it('centres a single point rather than dividing by zero', () => {
    expect(sparkPaths([5]).line).toBe('M50 12')
  })

  it('runs level through the middle when nothing changes', () => {
    expect(sparkPaths([3, 3, 3]).line).toBe('M0 12L50 12L100 12')
  })
})

describe('ringValue', () => {
  it('is the percentage of the goal, capped at a full ring', () => {
    expect(ringValue(5, 10)).toBe(50)
    expect(ringValue(25, 10)).toBe(100)
    expect(ringValue(0, 10)).toBe(0)
  })

  it('is full once anything is closed when there is no goal', () => {
    expect(ringValue(0, null)).toBe(0)
    expect(ringValue(1, null)).toBe(100)
  })
})

describe('dayState', () => {
  it('names where the day stands against the goal', () => {
    expect(dayState(0, 10)).toBe('none')
    expect(dayState(4, 10)).toBe('toGo')
    expect(dayState(10, 10)).toBe('met')
    expect(dayState(12, 10)).toBe('met')
  })

  it('has no goal to meet without one', () => {
    expect(dayState(0, null)).toBe('none')
    expect(dayState(3, null)).toBe('started')
  })
})

describe('againstTypical', () => {
  it('is the signed whole-case difference from a typical day', () => {
    expect(againstTypical(9, 7)).toBe(2)
    expect(againstTypical(4, 6.4)).toBe(-2)
  })
})
