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
import {isolate, joinIsolated} from '../isolate'

describe('bidi isolation', () => {
  it('wraps a value so it cannot reorder the sentence it sits in', () => {
    expect(isolate('alice')).toBe('\u2068alice\u2069')
  })

  it('leaves nothing for an empty value', () => {
    expect(isolate('')).toBe('')
    expect(isolate(null)).toBe('')
    expect(isolate(undefined)).toBe('')
  })

  it('joins only the parts that exist, each isolated', () => {
    expect(joinIsolated(['sam', '', null, '2026-01-01'])).toBe(
      '\u2068sam\u2069 · \u20682026-01-01\u2069',
    )
    expect(joinIsolated([null, undefined])).toBe('')
  })
})
