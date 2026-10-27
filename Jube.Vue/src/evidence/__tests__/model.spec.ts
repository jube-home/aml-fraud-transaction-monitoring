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
import { countLeaves, evidenceRows, formatLeaf, type LeafText } from '../model'

const text: LeafText = {
  yes: 'Yes',
  no: 'No',
  none: 'No value',
  empty: '(empty)',
  number: (value) => value.toFixed(2),
}

describe('formatLeaf', () => {
  it.each([
    [true, 'Yes', 'leaf'],
    [false, 'No', 'leaf'],
    [null, 'No value', 'null'],
    [undefined, 'No value', 'null'],
    ['', '(empty)', 'empty'],
    ['text', 'text', 'leaf'],
    [1.5, '1.50', 'leaf'],
    [0, '0.00', 'leaf'],
  ])('formats %j', (value, shown, kind) => {
    expect(formatLeaf(value, text)).toEqual({ value: shown, kind })
  })
})

describe('evidenceRows', () => {
  it('makes a root row per top-level key with nested branches below', () => {
    const rows = evidenceRows({ model: { id: 3, tags: ['a', 'b'] }, ok: true }, text)

    expect(rows.map((row) => row.key)).toEqual(['model', 'ok'])
    expect(rows[0]).toMatchObject({ kind: 'branch', value: '' })
    expect(rows[0]!._children!.map((row) => row.key)).toEqual(['id', 'tags'])
    expect(rows[0]!._children![1]!._children!.map((row) => row.key)).toEqual(['[0]', '[1]'])
    expect(rows[1]).toMatchObject({ kind: 'leaf', value: 'Yes' })
  })

  it('shows empty objects and arrays as such instead of a bare branch', () => {
    const rows = evidenceRows({ a: {}, b: [] }, text)

    expect(rows.map((row) => [row.value, row.kind])).toEqual([
      ['{}', 'empty'],
      ['[]', 'empty'],
    ])
  })

  it('tells a null from an empty string', () => {
    const rows = evidenceRows({ a: null, b: '' }, text)

    expect(rows.map((row) => row.value)).toEqual(['No value', '(empty)'])
  })

  it.each([[null], [undefined], ['text'], [5]])('gives no rows for %j', (value) => {
    expect(evidenceRows(value, text)).toEqual([])
  })

  it('treats a top-level array as indexed roots', () => {
    expect(evidenceRows([1, 2], text).map((row) => row.key)).toEqual(['[0]', '[1]'])
  })
})

describe('countLeaves', () => {
  it('counts values, not branches', () => {
    expect(countLeaves(evidenceRows({ a: { b: 1, c: [2, 3] }, d: 4 }, text))).toBe(4)
  })
})
