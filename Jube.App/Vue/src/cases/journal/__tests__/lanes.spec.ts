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
  isLaneOperator,
  isRangeType,
  laneLetters,
  laneOperators,
  laneValueText,
  lettersOf,
  sameLane,
} from '../lanes'

describe('journal merge lanes', () => {
  it('offers four lanes and every operator the server accepts', () => {
    expect(laneLetters).toEqual(['A', 'B', 'C', 'D'])
    expect([...laneOperators]).toEqual(['eq', 'gt', 'ge', 'lt', 'le', 'between'])
  })

  it('keeps only lane letters from a row value and nothing from a non string', () => {
    expect(lettersOf('ABZ')).toEqual(['A', 'B'])
    expect(lettersOf('')).toEqual([])
    expect(lettersOf(42)).toEqual([])
    expect(lettersOf(null)).toEqual([])
  })

  it('allows a range operator on integer, float and date keys only', () => {
    for (const dataTypeId of [2, 3, 4, 6, 7]) expect(isRangeType(dataTypeId)).toBe(true)
    for (const dataTypeId of [1, 5, 8, 0]) expect(isRangeType(dataTypeId)).toBe(false)
    expect(isRangeType(undefined)).toBe(false)
  })

  it('recognises an operator only when it is one of the known names', () => {
    expect(isLaneOperator('between')).toBe(true)
    expect(isLaneOperator('like')).toBe(false)
    expect(isLaneOperator('')).toBe(false)
    expect(isLaneOperator(undefined)).toBe(false)
    expect(isLaneOperator('__proto__')).toBe(false)
  })

  it('writes a lane value as the bare value, a comparison or a range', () => {
    expect(laneValueText({value: 'A1'})).toBe('A1')
    expect(laneValueText({value: 'A1', op: 'eq'})).toBe('A1')
    expect(laneValueText({value: '100', op: 'ge'})).toBe('≥ 100')
    expect(laneValueText({value: '100', op: 'lt'})).toBe('< 100')
    expect(laneValueText({value: '1', op: 'between', to: '9'})).toBe('1 – 9')
    expect(laneValueText({value: '1', op: 'between'})).toBe('1 – ')
  })

  it('treats a missing operator as equality and compares the upper bound too', () => {
    expect(sameLane({key: 'k', value: 'v'}, {key: 'k', value: 'v', op: 'eq'})).toBe(true)
    expect(sameLane({key: 'k', value: 'v'}, {key: 'k', value: 'v', op: 'ge'})).toBe(false)
    expect(
      sameLane(
        {key: 'k', value: '1', op: 'between', to: '2'},
        {key: 'k', value: '1', op: 'between', to: '3'},
      ),
    ).toBe(false)
    expect(sameLane({key: 'k', value: 'v'}, {key: 'other', value: 'v'})).toBe(false)
  })
})
