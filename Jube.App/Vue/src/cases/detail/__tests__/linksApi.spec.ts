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
import {parseCaseNumber} from '../linksApi'

describe('parseCaseNumber', () => {
  it('reads a plain number and one with a leading hash', () => {
    expect(parseCaseNumber('42')).toBe(42)
    expect(parseCaseNumber(' #42 ')).toBe(42)
  })

  it.each(['', ' ', '0', '-3', '4.5', 'abc', '12a', '1234567890', '#'])('refuses %j', (text) => {
    expect(parseCaseNumber(text)).toBeNull()
  })
})
