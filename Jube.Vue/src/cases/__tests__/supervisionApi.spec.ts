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
import { defaultIdle, idleChoices, idleOf } from '../supervisionApi'

describe('idleOf', () => {
  it('accepts only the offered idle times and otherwise uses the default', () => {
    for (const choice of idleChoices) expect(idleOf(String(choice))).toBe(choice)

    expect(idleOf(['240', '60'])).toBe(240)
    expect(idleOf('45')).toBe(defaultIdle)
    expect(idleOf(undefined)).toBe(defaultIdle)
    expect(idleOf('abc')).toBe(defaultIdle)
  })

  it('defaults to two hours', () => {
    expect(defaultIdle).toBe(120)
  })
})
