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
import { paintFor, paintOf, readableOn, rgbOf } from '../paint'
import type { JournalRow } from '../types'

const t = (key: string, values?: Record<string, unknown>) =>
  `${key}${values ? ` ${JSON.stringify(values)}` : ''}`

const cell = (over: Record<string, unknown> = {}) => ({
  CellFormatKey: 'Channel',
  CellFormatPattern: '^Ap',
  CellFormatForeColor: '#112233',
  CellFormatBackColor: '#ffeecc',
  CellFormatForeRow: false,
  CellFormatBackRow: false,
  ...over,
})

const rowOf = (extra: Record<string, unknown>): JournalRow => ({ Id: 1, ...extra }) as JournalRow

describe('journal colours', () => {
  it('reads hex, rgb and ignores what it cannot read', () => {
    expect(rgbOf('#fc0')).toEqual({ r: 255, g: 204, b: 0 })
    expect(rgbOf('#112233')).toEqual({ r: 17, g: 34, b: 51 })
    expect(rgbOf('rgb(1, 2, 3)')).toEqual({ r: 1, g: 2, b: 3 })
    expect(rgbOf('')).toBeUndefined()
    expect(rgbOf(null)).toBeUndefined()
    expect(rgbOf('not a colour')).toBeUndefined()
  })

  it('picks the more readable of black and white', () => {
    expect(readableOn({ r: 255, g: 255, b: 255 })).toBe('#000000')
    expect(readableOn({ r: 0, g: 0, b: 40 })).toBe('#ffffff')
  })

  it('keeps the configured colours and fills in a readable text colour when none is set', () => {
    expect(paintOf('#111111', '#eeeeee')).toEqual({ fore: '#111111', back: '#eeeeee' })
    expect(paintOf(null, '#ffffff')).toEqual({ fore: '#000000', back: '#ffffff' })
    expect(paintOf('', '#101010')).toEqual({ fore: '#ffffff', back: '#101010' })
    expect(paintOf('garbage', 'nonsense')).toEqual({})
  })

  it('has nothing to say about a row without formatting', () => {
    expect(paintFor(rowOf({}), t)).toBeNull()
    expect(paintFor(rowOf({ CellFormat: [] }), t)).toBeNull()
  })

  it('makes the bold line a bold row in its own colours and says why', () => {
    const paint = paintFor(
      rowOf({
        BoldLine: {
          BoldLineKey: 'Device',
          BoldLineValue: 'D1',
          BoldLineFormatForeColor: '#222222',
          BoldLineFormatBackColor: '#111111',
        },
      }),
      t,
    )

    expect(paint?.row).toEqual({ fore: '#222222', back: '#111111', bold: true })
    expect(paint?.reasons).toEqual(['journal.format.bold {"field":"Device","value":"D1"}'])
  })

  it('paints only the cell when neither colour is for the row', () => {
    const paint = paintFor(rowOf({ CellFormat: [cell()] }), t)

    expect(paint?.cells.Channel).toEqual({ fore: '#112233', back: '#ffeecc' })
    expect(paint?.row).toEqual({ bold: false })
    expect(paint?.reasons).toEqual(['journal.format.cell {"field":"Channel","pattern":"^Ap"}'])
  })

  it('paints the row background, with readable row text, and the cell text when only the background is for the row', () => {
    const paint = paintFor(rowOf({ CellFormat: [cell({ CellFormatBackRow: true })] }), t)

    expect(paint?.row.back).toBe('#ffeecc')
    expect(paint?.row.fore).toBe('#000000')
    expect(paint?.cells.Channel).toEqual({ fore: '#112233' })
  })

  it('paints the row text and the cell background when only the text is for the row', () => {
    const paint = paintFor(rowOf({ CellFormat: [cell({ CellFormatForeRow: true })] }), t)

    expect(paint?.row.fore).toBe('#112233')
    expect(paint?.cells.Channel).toEqual({ back: '#ffeecc', fore: '#112233' })
  })

  it('paints the whole row when both are for the row, over the bold line colours', () => {
    const paint = paintFor(
      rowOf({
        BoldLine: {
          BoldLineKey: 'Device',
          BoldLineValue: 'D1',
          BoldLineFormatForeColor: '#222222',
          BoldLineFormatBackColor: '#111111',
        },
        CellFormat: [cell({ CellFormatForeRow: true, CellFormatBackRow: true })],
      }),
      t,
    )

    expect(paint?.row).toEqual({ fore: '#112233', back: '#ffeecc', bold: true })
    expect(paint?.cells.Channel).toBeUndefined()
    expect(paint?.reasons).toHaveLength(2)
  })

  it('skips a malformed format entry', () => {
    expect(paintFor(rowOf({ CellFormat: [null, 5, { CellFormatKey: '' }] }), t)).toBeNull()
  })
})
