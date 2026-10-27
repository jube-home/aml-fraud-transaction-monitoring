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
import {
  buildWorkbook,
  cellValue,
  columnWidth,
  exportableColumns,
  sheetRows,
  type SheetColumn,
} from '../xlsx'

const columns: SheetColumn[] = [
  { title: 'Name', field: 'name', type: 'text' },
  { title: 'Hits', field: 'hits', type: 'number' },
  { title: 'Seen', field: 'seen', type: 'date' },
]

describe('exportableColumns', () => {
  it('keeps data columns and drops buttons, inputs, drawn cells and unnamed columns', () => {
    const kept = exportableColumns([
      { field: 'a', title: 'A' },
      { field: 'b', title: 'B', type: 'number', width: 90 },
      { field: '', title: 'Actions', actions: () => [] },
      { field: 'c', title: 'C', input: () => null },
      { field: 'd', title: 'D', draw: () => document.createElement('i') },
      { field: '', title: 'Blank' },
    ])

    expect(kept).toEqual([
      { title: 'A', field: 'a', type: 'text', width: undefined },
      { title: 'B', field: 'b', type: 'number', width: 90 },
    ])
  })
})

describe('cellValue', () => {
  it.each([
    [null, 'text', null],
    [undefined, 'number', null],
    ['', 'date', null],
    ['=1+1', 'text', '=1+1'],
    [5, 'text', '5'],
    ['12.5', 'number', 12.5],
    [7, 'number', 7],
    ['n/a', 'number', 'n/a'],
  ] as const)('%j as %s is %j', (value, type, expected) => {
    expect(cellValue(value, type)).toEqual(expected)
  })

  it('turns an ISO string into a real date, and leaves junk as text', () => {
    expect(cellValue('2026-10-20T10:30:00Z', 'date')).toEqual(new Date('2026-10-20T10:30:00Z'))
    expect(cellValue('later', 'date')).toBe('later')
  })
})

describe('columnWidth', () => {
  it('fits the longest value, within a floor and a ceiling', () => {
    expect(columnWidth('A', ['x'])).toBe(8)
    expect(columnWidth('Title', ['a much longer value here'])).toBe(26)
    expect(columnWidth('T', ['x'.repeat(200)])).toBe(60)
  })
})

describe('sheetRows', () => {
  it('orders cells by column', () => {
    expect(sheetRows(columns, [{ hits: '3', name: 'a', seen: null }])).toEqual([['a', 3, null]])
  })
})

describe('buildWorkbook', () => {
  it('writes a styled sheet with typed cells', async () => {
    const buffer = await buildWorkbook(
      columns,
      [
        { name: '=HYPERLINK("x")', hits: 3, seen: '2026-10-20T10:30:00Z' },
        { name: 'b', hits: '4', seen: null },
      ],
      'Evidence',
    )
    const { Workbook } = await import('exceljs')
    const workbook = new Workbook()

    await workbook.xlsx.load(buffer)

    const sheet = workbook.getWorksheet('Evidence')!

    expect(sheet.getRow(1).values).toEqual([undefined, 'Name', 'Hits', 'Seen'])
    expect(sheet.getRow(1).font?.bold).toBe(true)
    expect(sheet.getRow(1).getCell(1).fill).toMatchObject({ fgColor: { argb: 'FF005F7F' } })
    expect(sheet.views[0]).toMatchObject({ state: 'frozen', ySplit: 1 })
    expect(sheet.autoFilter).toBeTruthy()
    expect(sheet.getCell('A2').value).toBe('=HYPERLINK("x")')
    expect(sheet.getCell('A2').type).toBe(3)
    expect(sheet.getCell('B3').value).toBe(4)
    expect(sheet.getCell('C2').value).toEqual(new Date('2026-10-20T10:30:00Z'))
    expect(sheet.getCell('C2').numFmt).toBe('yyyy-mm-dd hh:mm')
  })
})
