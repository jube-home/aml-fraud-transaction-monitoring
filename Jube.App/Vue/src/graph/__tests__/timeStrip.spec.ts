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
import {parseTimeStripBins} from '../timeStrip'

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 20">' +
  '<rect data-bin="1" data-from="200" data-to="300" width="10" height="5"/>' +
  '<rect data-bin="0" data-from="100" data-to="200" width="10" height="5"/>' +
  '<rect data-bin="x" data-from="1" data-to="2"/>' +
  '</svg>'

describe('time strip bins', () => {
  it('reads the bars in bin order and drops malformed ones', () => {
    expect(parseTimeStripBins(svg)).toEqual([
      {index: 0, from: 100, to: 200},
      {index: 1, from: 200, to: 300},
    ])
  })

  it('returns nothing for markup that is not SVG', () => {
    expect(parseTimeStripBins('<svg><rect')).toEqual([])
  })
})
