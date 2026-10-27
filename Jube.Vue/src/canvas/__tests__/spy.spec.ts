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
import { activeSection } from '../spy'

const sections = [
  { id: 'a', top: -400 },
  { id: 'b', top: 40 },
  { id: 'c', top: 900 },
]

describe('activeSection', () => {
  it('is the last section whose top has passed the offset', () => {
    expect(activeSection(sections, 100)).toBe('b')
    expect(activeSection(sections, 1000)).toBe('c')
  })

  it('falls back to the first section before any has reached the offset', () => {
    expect(activeSection([{ id: 'x', top: 500 }], 100)).toBe('x')
  })

  it('is nothing when there are no sections', () => {
    expect(activeSection([], 100)).toBeNull()
  })

  it('is the last section once the page cannot scroll any further', () => {
    expect(activeSection(sections, 100, true)).toBe('c')
    expect(activeSection(sections, 100, false)).toBe('b')
  })

  it('counts a section whose top is exactly at the offset', () => {
    expect(activeSection(sections, 40)).toBe('b')
  })
})
