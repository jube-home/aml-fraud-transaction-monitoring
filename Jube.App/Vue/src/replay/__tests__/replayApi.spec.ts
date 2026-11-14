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
import {comparable, percent, windowValid} from '../replayApi'

describe('agent replay', () => {
  it('shows an agreement rate as a percentage and a missing one as a dash', () => {
    expect(percent(0.8123)).toBe('81.2%')
    expect(percent(null)).toBe('–')
  })

  it('needs a window whose start is not after its end', () => {
    expect(windowValid('2026-01-01', '2026-02-01')).toBe(true)
    expect(windowValid('2026-02-01', '2026-01-01')).toBe(false)
    expect(windowValid('', '2026-01-01')).toBe(false)
  })

  it('compares exactly two different replays', () => {
    expect(comparable(['a', 'b'])).toBe(true)
    expect(comparable(['a'])).toBe(false)
    expect(comparable(['a', 'a'])).toBe(false)
  })
})
