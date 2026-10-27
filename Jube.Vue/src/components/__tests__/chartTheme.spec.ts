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
import { chartThemeOf } from '../chartTheme'

describe('chartThemeOf', () => {
  it('draws charts dark only in the dark app theme, whatever the operating system prefers', () => {
    expect(chartThemeOf('jube-dark')).toBe('dark')
    expect(chartThemeOf('jube')).toBe('light')
    expect(chartThemeOf('anything else')).toBe('light')
  })
})
