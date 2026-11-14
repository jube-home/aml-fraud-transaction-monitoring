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
import {lineMarkOf} from '../scanGutter'

describe('scan gutter marks', () => {
  it('marks a line that ends the scan', () => {
    expect(lineMarkOf('If Cursor.Matches >= 5 Then Exit For')).toBe('stop')
    expect(lineMarkOf('  exit   for')).toBe('stop')
  })

  it('marks a line that skips to the next record', () => {
    expect(lineMarkOf('If Cursor.IsCurrent Then Continue For')).toBe('skip')
  })

  it('marks a line that answers', () => {
    expect(lineMarkOf('Return True')).toBe('answer')
    expect(lineMarkOf('Matched = Payload.Amount > 1')).toBe('answer')
    expect(lineMarkOf('If x Then Matched = True')).toBe('answer')
    expect(lineMarkOf('If x Then Return True Else Matched = False')).toBe('answer')
  })

  it('prefers the scan mark when a line both answers and ends the scan', () => {
    expect(lineMarkOf('Matched = True : Exit For')).toBe('stop')
  })

  it('leaves a line that only reads the answer, and text and comments', () => {
    expect(lineMarkOf('If Matched Then')).toBeNull()
    expect(lineMarkOf('If x = Matched Then')).toBeNull()
    expect(lineMarkOf("' Return True, Exit For")).toBeNull()
    expect(lineMarkOf('x = "Exit For"')).toBeNull()
    expect(lineMarkOf('')).toBeNull()
  })
})
