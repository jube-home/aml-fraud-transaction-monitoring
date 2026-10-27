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
import type { GridColumn } from '../../../components/dataGridTypes'
import {
  apply,
  complete,
  emptyLayout,
  moved,
  parseLayout,
  resized,
  serialiseLayout,
  shown,
  visibleFields,
  withLens,
} from '../layout'

const defaults: GridColumn[] = [
  { field: '__selected', title: 'Select', pinned: true, width: 56 },
  { field: 'actions', title: 'Actions', pinned: true, width: 110 },
  { field: 'a', title: 'A', minWidth: 120 },
  { field: 'b', title: 'B' },
  { field: 'c', title: 'C' },
]

const fields = (columns: GridColumn[]): string[] => columns.map((column) => column.field)

describe('journal column layout', () => {
  it('keeps the default order when nothing is saved', () => {
    expect(fields(apply(defaults, undefined))).toEqual(['__selected', 'actions', 'a', 'b', 'c'])
  })

  it('orders by the saved layout and appends columns it has not seen', () => {
    const entries = [{ field: 'c' }, { field: 'a' }]

    expect(fields(apply(defaults, entries))).toEqual(['__selected', 'actions', 'c', 'a', 'b'])
  })

  it('never moves or hides the pinned columns', () => {
    const entries = [{ field: '__selected', hidden: true }, { field: 'c' }]

    expect(fields(apply(defaults, entries)).slice(0, 2)).toEqual(['__selected', 'actions'])
    expect(complete(defaults, entries).map((entry) => entry.field)).toContain('__selected')
    expect(fields(apply(defaults, entries))).toContain('__selected')
  })

  it('hides columns and lets a saved width replace the default width', () => {
    const entries = [
      { field: 'a', width: 300 },
      { field: 'b', hidden: true },
    ]
    const columns = apply(defaults, entries)

    expect(fields(columns)).toEqual(['__selected', 'actions', 'a', 'c'])
    expect(columns.find((column) => column.field === 'a')).toMatchObject({
      width: 300,
      minWidth: undefined,
    })
  })

  it('keeps a saved entry for a column that is not in this view', () => {
    const entries = [{ field: 'gone', width: 90 }, { field: 'a' }]

    expect(fields(apply(defaults, entries))).toEqual(['__selected', 'actions', 'a', 'b', 'c'])
    expect(complete(defaults, entries).map((entry) => entry.field)).toEqual(['gone', 'a', 'b', 'c'])
  })

  it('records a move, keeping hidden columns where they were', () => {
    const entries = [{ field: 'a' }, { field: 'b', hidden: true }, { field: 'c' }]
    const result = moved(defaults, entries, ['c', 'a'])

    expect(result.map((entry) => entry.field)).toEqual(['c', 'b', 'a'])
    expect(result.find((entry) => entry.field === 'b')?.hidden).toBe(true)
  })

  it('ignores a move that does not name every visible column', () => {
    expect(moved(defaults, undefined, ['c', 'a']).map((entry) => entry.field)).toEqual([
      'a',
      'b',
      'c',
    ])
  })

  it('records a resize with a floor on the width', () => {
    expect(resized(defaults, undefined, 'b', 250.4).find((e) => e.field === 'b')?.width).toBe(250)
    expect(resized(defaults, undefined, 'b', 3).find((e) => e.field === 'b')?.width).toBe(40)
  })

  it('shows and hides a column and lists the visible ones', () => {
    const off = shown(defaults, undefined, 'b', false)

    expect(visibleFields(defaults, off)).toEqual(['a', 'c'])
    expect(visibleFields(defaults, shown(defaults, off, 'b', true))).toEqual(['a', 'b', 'c'])
  })

  it('round trips and drops anything that is not a layout', () => {
    const layout = withLens(emptyLayout(), 'payload', [{ field: 'a', width: 90, hidden: true }])

    expect(parseLayout(serialiseLayout(layout))).toEqual(layout)
    expect(parseLayout('[{"field":"a"}]')).toEqual(emptyLayout())
    expect(parseLayout('not json')).toEqual(emptyLayout())
    expect(parseLayout(null)).toEqual(emptyLayout())
    expect(parseLayout('{"lenses":{"x":[{"field":""},{"field":"a"},{"field":"a"},5]}}')).toEqual({
      version: 1,
      lenses: { x: [{ field: 'a' }] },
    })
  })

  it('clears a lens back to the defaults', () => {
    const layout = withLens(emptyLayout(), 'rules', [{ field: 'a' }])

    expect(withLens(layout, 'rules', undefined).lenses).toEqual({})
  })
})
