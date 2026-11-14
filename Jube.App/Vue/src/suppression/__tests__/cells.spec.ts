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

import {describe, expect, it, vi} from 'vitest'
import {tickCell} from '../cells'

describe('tickCell', () => {
  it('draws a labelled checkbox that reports each change', () => {
    const changed = vi.fn()
    const cell = tickCell(true, 'Use this transaction', changed) as HTMLInputElement

    expect(cell.type).toBe('checkbox')
    expect(cell.checked).toBe(true)
    expect(cell.getAttribute('aria-label')).toBe('Use this transaction')

    cell.checked = false
    cell.dispatchEvent(new Event('change'))

    expect(changed).toHaveBeenCalledWith(false)
  })
})
