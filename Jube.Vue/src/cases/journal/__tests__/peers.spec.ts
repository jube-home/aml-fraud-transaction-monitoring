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
import { ordinal } from '../peers'

describe('journal peers', () => {
  it('turns a fraction into a whole percentile between 1 and 99', () => {
    expect(ordinal(0.5)).toBe(50)
    expect(ordinal(0.784)).toBe(78)
    expect(ordinal(0)).toBe(1)
    expect(ordinal(1)).toBe(99)
    expect(ordinal(0.004)).toBe(1)
    expect(ordinal(0.996)).toBe(99)
  })
})
