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
import { kindKey, offeredKinds } from '../state'

describe('kindKey', () => {
  it('names 1 as force and anything else as suppress', () => {
    expect(kindKey(1)).toBe('force')
    expect(kindKey(0)).toBe('suppress')
    expect(kindKey(undefined)).toBe('suppress')
  })
})

describe('offeredKinds', () => {
  it('offers Force only where the rule allows it, or where it is already Force', () => {
    expect(offeredKinds(0, false)).toEqual([0])
    expect(offeredKinds(0, true)).toEqual([0, 1])
    expect(offeredKinds(1, false)).toEqual([0, 1])
  })
})
