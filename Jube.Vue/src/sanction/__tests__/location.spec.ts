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
import { locationFromParams, locationKey, paramsFromLocation } from '../location'
import { paramsOf } from '../state'

describe('paramsFromLocation', () => {
  it('uses the defaults for an empty address', () => {
    expect(paramsFromLocation({})).toEqual(paramsOf({}))
  })

  it('reads every setting, including a zero', () => {
    expect(paramsFromLocation({ q: 'Ivan', d: '0', r: '0.5', c: '4' })).toEqual({
      multiPartString: 'Ivan',
      distance: 0,
      maxDistanceRatio: 0.5,
      maxCoverageRatio: 4,
    })
  })

  it('clamps out-of-range values and ignores rubbish', () => {
    expect(paramsFromLocation({ d: '99', r: '-3', c: 'abc' })).toMatchObject({
      distance: 5,
      maxDistanceRatio: 0,
      maxCoverageRatio: 2,
    })
  })

  it('takes the first of a repeated parameter', () => {
    expect(paramsFromLocation({ q: ['a', 'b'] }).multiPartString).toBe('a')
  })
})

describe('locationFromParams', () => {
  it('writes nothing when there is no text', () => {
    expect(locationFromParams(paramsOf({ multiPartString: '  ', distance: 4 }))).toEqual({})
  })

  it('omits settings left at their defaults and trims the text', () => {
    expect(locationFromParams(paramsOf({ multiPartString: ' Ivan ' }))).toEqual({ q: 'Ivan' })
  })

  it('round trips non-default settings', () => {
    const params = paramsOf({
      multiPartString: 'Ivan Petrov',
      distance: 3,
      maxDistanceRatio: 0.5,
      maxCoverageRatio: 0,
    })

    expect(paramsFromLocation(locationFromParams(params))).toEqual(params)
  })

  it('gives equal keys for equal searches', () => {
    expect(locationKey(paramsOf({ multiPartString: 'a ' }))).toBe(
      locationKey(paramsOf({ multiPartString: 'a' })),
    )
  })
})
