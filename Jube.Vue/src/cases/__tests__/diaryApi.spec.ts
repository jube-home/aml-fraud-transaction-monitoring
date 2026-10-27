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
import { isoToLocalInput, localInputToIso } from '../diaryApi'

describe('diary date conversion', () => {
  it('reads the picker value as UTC so the date the analyst sees is the date that is stored', () => {
    expect(localInputToIso('2026-10-30T09:30')).toBe('2026-10-30T09:30:00.000Z')
  })

  it('returns nothing for an empty or half-typed value', () => {
    expect(localInputToIso('')).toBeNull()
    expect(localInputToIso('2026-10-30')).toBeNull()
    expect(localInputToIso('2026-13-45T99:99')).toBeNull()
  })

  it('round trips a stored date back into the picker', () => {
    expect(isoToLocalInput('2026-10-30T09:30:00Z')).toBe('2026-10-30T09:30')
    expect(isoToLocalInput(null)).toBe('')
    expect(isoToLocalInput('not a date')).toBe('')
  })
})
