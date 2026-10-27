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

import { contrastRatio, parseCssColor, type Rgb } from '../../composables/contrast'
import type { Translate } from './columns'
import type { JournalRow } from './types'

export interface Paint {
  fore?: string
  back?: string
}

export interface RowPaint {
  row: Paint & { bold: boolean }
  cells: Record<string, Paint>
  reasons: string[]
}

const dark = '#000000'
const light = '#ffffff'

function hexRgb(value: string): Rgb | undefined {
  const match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(value.trim())

  if (!match) return undefined

  const digits = match[1] as string
  const full =
    digits.length === 3
      ? digits
          .split('')
          .map((digit) => digit + digit)
          .join('')
      : digits

  return {
    r: Number.parseInt(full.slice(0, 2), 16),
    g: Number.parseInt(full.slice(2, 4), 16),
    b: Number.parseInt(full.slice(4, 6), 16),
  }
}

function canvasRgb(value: string): Rgb | undefined {
  if (
    typeof document === 'undefined' ||
    typeof CSS === 'undefined' ||
    !CSS.supports?.('color', value)
  ) {
    return undefined
  }

  try {
    const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true })

    if (!context) return undefined

    context.fillStyle = '#010203'
    context.fillStyle = value
    context.fillRect(0, 0, 1, 1)

    const [r, g, b] = context.getImageData(0, 0, 1, 1).data

    return { r: r ?? 0, g: g ?? 0, b: b ?? 0 }
  } catch {
    return undefined
  }
}

export function rgbOf(value: string | null | undefined): Rgb | undefined {
  const text = (value ?? '').trim()

  if (text === '') return undefined

  return hexRgb(text) ?? parseCssColor(text) ?? canvasRgb(text)
}

export function readableOn(back: Rgb): string {
  const black = contrastRatio(back, { r: 0, g: 0, b: 0 })
  const white = contrastRatio(back, { r: 255, g: 255, b: 255 })

  return black >= white ? dark : light
}

export function paintOf(fore: string | null | undefined, back: string | null | undefined): Paint {
  const foreRgb = rgbOf(fore)
  const backRgb = rgbOf(back)
  const paint: Paint = {}

  if (backRgb) paint.back = (back as string).trim()

  if (foreRgb) paint.fore = (fore as string).trim()
  else if (backRgb) paint.fore = readableOn(backRgb)

  return paint
}

function asText(value: unknown): string {
  return value === null || value === undefined ? '' : String(value)
}

interface CellFormat {
  field: string
  pattern: string
  fore: string | null
  back: string | null
  foreRow: boolean
  backRow: boolean
}

function cellFormats(raw: unknown): CellFormat[] {
  if (!Array.isArray(raw)) return []

  return raw
    .filter((item): item is Record<string, unknown> => typeof item === 'object' && item !== null)
    .map((item) => ({
      field: asText(item.CellFormatKey),
      pattern: asText(item.CellFormatPattern),
      fore: item.CellFormatForeColor === null ? null : asText(item.CellFormatForeColor),
      back: item.CellFormatBackColor === null ? null : asText(item.CellFormatBackColor),
      foreRow: item.CellFormatForeRow === true,
      backRow: item.CellFormatBackRow === true,
    }))
    .filter((item) => item.field !== '')
}

export function paintFor(row: JournalRow, t: Translate): RowPaint | null {
  const result: RowPaint = { row: { bold: false }, cells: {}, reasons: [] }
  const bold = row.BoldLine

  if (typeof bold === 'object' && bold !== null) {
    const line = bold as Record<string, unknown>
    const paint = paintOf(
      line.BoldLineFormatForeColor as string | null,
      line.BoldLineFormatBackColor as string | null,
    )

    result.row = { ...paint, bold: true }
    result.reasons.push(
      t('journal.format.bold', {
        field: asText(line.BoldLineKey),
        value: asText(line.BoldLineValue),
      }),
    )
  }

  for (const format of cellFormats(row.CellFormat)) {
    const paint = paintOf(format.fore, format.back)
    const cell: Paint = {}
    const rowWide: Paint = {}

    if (format.foreRow) rowWide.fore = paint.fore
    else cell.fore = paint.fore

    if (format.backRow) rowWide.back = paint.back
    else cell.back = paint.back

    if (!format.backRow && paint.back && !cell.fore) cell.fore = paint.fore

    if (cell.fore || cell.back) {
      result.cells[format.field] = { ...result.cells[format.field], ...cell }
    }

    if (rowWide.fore) result.row.fore = rowWide.fore
    if (rowWide.back) result.row.back = rowWide.back

    const rowBack = rgbOf(rowWide.back)

    if (format.backRow && rowBack && !format.foreRow) result.row.fore = readableOn(rowBack)

    result.reasons.push(t('journal.format.cell', { field: format.field, pattern: format.pattern }))
  }

  return result.reasons.length === 0 ? null : result
}
