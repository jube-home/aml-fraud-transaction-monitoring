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
import { fromLocalInput, isFuture, toLocalInput } from '../localDateTime'

describe('isFuture', () => {
  const now = new Date('2026-10-21T12:00:00Z')

  it('accepts no expiry and a later one, and refuses now or earlier', () => {
    expect(isFuture(null, now)).toBe(true)
    expect(isFuture(new Date('2026-10-21T12:00:01Z'), now)).toBe(true)
    expect(isFuture(new Date('2026-10-21T12:00:00Z'), now)).toBe(false)
    expect(isFuture(new Date('2026-10-20T12:00:00Z'), now)).toBe(false)
  })
})

describe('local input round trip', () => {
  it('shows an instant in local time to the minute and reads it back', () => {
    const shown = toLocalInput('2026-10-21T12:34:56Z')

    expect(shown).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/)
    expect(fromLocalInput(shown)!.getTime()).toBe(new Date('2026-10-21T12:34:00Z').getTime())
  })

  it('treats empty and invalid as no value', () => {
    expect(toLocalInput(null)).toBe('')
    expect(toLocalInput('nonsense')).toBe('')
    expect(fromLocalInput('')).toBeNull()
    expect(fromLocalInput('nonsense')).toBeNull()
  })
})

describe('toLocalInput of a Date', () => {
  it('pads to the minute in local time', () => {
    expect(toLocalInput(new Date(2026, 0, 5, 3, 7))).toBe('2026-01-05T03:07')
  })
})
