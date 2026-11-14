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
import {statusStyle} from '../statusStyle'

describe('statusStyle', () => {
  it('styles a status with two hex colours', () => {
    expect(statusStyle('#445566', '#112233')).toBe('background-color:#445566;color:#112233')
  })

  it('accepts short hex and trims', () => {
    expect(statusStyle(' #abc ', ' #fff ')).toBe('background-color:#abc;color:#fff')
  })

  it('falls back to inherited text when the foreground is not a hex colour', () => {
    expect(statusStyle('#445566', 'red')).toBe('background-color:#445566;color:inherit')
    expect(statusStyle('#445566', null)).toBe('background-color:#445566;color:inherit')
  })

  it.each([
    [null],
    [undefined],
    [''],
    ['red'],
    ['red;background-image:url(//evil.example/x)'],
    ['#445566;position:fixed'],
    ['#44556'],
    ['url(javascript:alert(1))'],
  ])('refuses the background %j and styles nothing', (back) => {
    expect(statusStyle(back, '#112233')).toBeUndefined()
  })

  it('never lets a hostile foreground into the style', () => {
    expect(statusStyle('#445566', 'red;position:fixed')).toBe(
      'background-color:#445566;color:inherit',
    )
  })
})
