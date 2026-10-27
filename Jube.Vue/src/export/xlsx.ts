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
import type { GridColumn } from '../components/dataGridTypes'

export interface SheetColumn {
  title: string
  field: string
  type: 'text' | 'number' | 'date'
  width?: number
}

export const headerFill = 'FF005F7F'
export const headerText = 'FFFFFFFF'
export const bandFill = 'FFF4F3EF'
export const dateFormat = 'yyyy-mm-dd hh:mm'

const minimumWidth = 8
const maximumWidth = 60

export function exportableColumns(columns: GridColumn[]): SheetColumn[] {
  return columns
    .filter((column) => !column.actions && !column.input && !column.draw && column.field !== '')
    .map((column) => ({
      title: column.title,
      field: column.field,
      type: column.type ?? 'text',
      width: column.width,
    }))
}

export function cellValue(
  value: unknown,
  type: SheetColumn['type'],
): string | number | Date | null {
  if (value === null || value === undefined || value === '') return null

  if (type === 'number') {
    const number = typeof value === 'number' ? value : Number(value)

    return Number.isFinite(number) ? number : String(value)
  }

  if (type === 'date') {
    const date = value instanceof Date ? value : new Date(String(value))

    return Number.isNaN(date.getTime()) ? String(value) : date
  }

  return typeof value === 'string' ? value : String(value)
}

export function columnWidth(title: string, values: (string | number | Date | null)[]): number {
  const longest = values.reduce<number>((best, value) => {
    if (value === null) return best

    const length = value instanceof Date ? dateFormat.length : String(value).length

    return Math.max(best, length)
  }, title.length)

  return Math.min(Math.max(longest + 2, minimumWidth), maximumWidth)
}

export function sheetRows(
  columns: SheetColumn[],
  rows: Record<string, unknown>[],
): (string | number | Date | null)[][] {
  return rows.map((row) => columns.map((column) => cellValue(row[column.field], column.type)))
}

export async function buildWorkbook(
  columns: SheetColumn[],
  rows: Record<string, unknown>[],
  sheetName: string,
): Promise<ArrayBuffer> {
  const module = await import('exceljs')
  const workbook = new (module.default ?? module).Workbook()
  const sheet = workbook.addWorksheet(sheetName.slice(0, 31) || 'Sheet1', {
    views: [{ state: 'frozen', ySplit: 1 }],
  })
  const data = sheetRows(columns, rows)

  sheet.columns = columns.map((column, index) => ({
    header: column.title,
    key: column.field,
    width: columnWidth(
      column.title,
      data.map((row) => row[index] ?? null),
    ),
  }))

  const header = sheet.getRow(1)

  header.font = { bold: true, color: { argb: headerText } }
  header.alignment = { vertical: 'middle' }
  header.height = 20
  header.eachCell((cell) => {
    cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: headerFill } }
  })

  data.forEach((values, rowIndex) => {
    const row = sheet.addRow(values)

    if (rowIndex % 2 === 1) {
      row.eachCell({ includeEmpty: true }, (cell) => {
        cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: bandFill } }
      })
    }
  })

  columns.forEach((column, index) => {
    const target = sheet.getColumn(index + 1)

    if (column.type === 'number') target.alignment = { horizontal: 'right' }
    if (column.type === 'date') target.numFmt = dateFormat
  })

  sheet.getRow(1).alignment = { vertical: 'middle' }

  if (columns.length > 0) {
    sheet.autoFilter = { from: { row: 1, column: 1 }, to: { row: 1, column: columns.length } }
  }

  return (await workbook.xlsx.writeBuffer()) as ArrayBuffer
}

export function saveBuffer(buffer: ArrayBuffer, filename: string): void {
  const blob = new Blob([buffer], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = filename
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
