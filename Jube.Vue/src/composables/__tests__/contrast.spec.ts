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
import { contrastRatio, parseCssColor } from '../contrast'

describe('contrast', () => {
  it('parses rgb and rgba computed colours', () => {
    expect(parseCssColor('rgb(0, 95, 127)')).toEqual({ r: 0, g: 95, b: 127 })
    expect(parseCssColor('rgba(255, 255, 255, 0.5)')).toEqual({ r: 255, g: 255, b: 255 })
    expect(parseCssColor('rgb(0 95 127)')).toEqual({ r: 0, g: 95, b: 127 })
  })

  it('returns undefined for formats it cannot read', () => {
    expect(parseCssColor('oklch(0.5 0.1 200)')).toBeUndefined()
  })

  it('gives 21 for black on white and 1 for identical colours', () => {
    const black = { r: 0, g: 0, b: 0 }
    const white = { r: 255, g: 255, b: 255 }

    expect(contrastRatio(black, white)).toBeCloseTo(21, 5)
    expect(contrastRatio(white, black)).toBeCloseTo(21, 5)
    expect(contrastRatio(white, white)).toBeCloseTo(1, 5)
  })

  it('matches a known pair', () => {
    expect(contrastRatio({ r: 119, g: 119, b: 119 }, { r: 255, g: 255, b: 255 })).toBeCloseTo(
      4.48,
      1,
    )
  })
})
