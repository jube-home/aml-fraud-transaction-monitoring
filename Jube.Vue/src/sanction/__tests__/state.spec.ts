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
import { canSearch, paramsOf, sanctionQuery } from '../state'

describe('paramsOf', () => {
  it('fills the old page defaults for anything not supplied', () => {
    expect(paramsOf({ multiPartString: 'Ivan' })).toEqual({
      multiPartString: 'Ivan',
      distance: 2,
      maxDistanceRatio: 0.3,
      maxCoverageRatio: 2,
    })
  })

  it('keeps what a host supplies, including a zero', () => {
    expect(paramsOf({ multiPartString: 'x', distance: 0, maxDistanceRatio: 0 })).toMatchObject({
      distance: 0,
      maxDistanceRatio: 0,
    })
  })
})

describe('canSearch', () => {
  it('needs some text that is not only white space', () => {
    expect(canSearch(paramsOf({}))).toBe(false)
    expect(canSearch(paramsOf({ multiPartString: '   ' }))).toBe(false)
    expect(canSearch(paramsOf({ multiPartString: ' Ivan ' }))).toBe(true)
  })
})

describe('sanctionQuery', () => {
  it('sends the four parameters the Invoke endpoint reads, with the text trimmed', () => {
    expect(sanctionQuery(paramsOf({ multiPartString: '  Ivan Petrov ' }))).toEqual({
      multiPartString: 'Ivan Petrov',
      distance: 2,
      maxDistanceRatio: 0.3,
      maxCoverageRatio: 2,
    })
  })
})
