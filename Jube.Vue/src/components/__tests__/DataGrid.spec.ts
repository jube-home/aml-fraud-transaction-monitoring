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

import { afterEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { TabulatorFull } from 'tabulator-tables'
import DataGrid from '../DataGrid.vue'
import { mountVapor, type MountedVapor } from '../../test-support/mountVapor'

const columns = [
  { title: 'Name', field: 'name' },
  { title: 'Amount', field: 'amount' },
]

const rows = [
  { name: 'Alpha', amount: 3 },
  { name: 'Beta', amount: 1 },
  { name: 'Alphabet', amount: 2 },
]

let mounted: MountedVapor | undefined

async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 20))
}

async function mountGrid(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  mounted = mountVapor(DataGrid, {
    props: { columns, rows, emptyText: 'Nothing here', label: 'Test grid', ...props },
    events: ['ready', 'sort', 'visibleRows'],
  })
  await settle()

  return mounted
}

function renderedNames(): string[] {
  const table = (TabulatorFull.findTable('.tabulator') as TabulatorFull[])[0]!

  return (table.getData('active') as { name: string }[]).map((row) => row.name)
}

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('DataGrid', () => {
  it('labels the grid and emits ready once built', async () => {
    const grid = await mountGrid()

    expect(document.querySelector('[aria-label="Test grid"]')).not.toBeNull()
    expect(grid.emitted.ready).toHaveLength(1)
  })

  it('reports the number of visible rows', async () => {
    const grid = await mountGrid()

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([3])
  })

  it('applies an initial filter', async () => {
    const grid = await mountGrid({ filterField: 'name', filterText: 'alpha' })

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([2])
  })

  it('re-filters when the filter text changes', async () => {
    const grid = await mountGrid({ filterField: 'name', filterText: '' })

    grid.setProps({ filterText: 'beta' })
    await settle()

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([1])

    grid.setProps({ filterText: '' })
    await settle()

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([3])
  })

  it('applies an initial sort', async () => {
    await mountGrid({ sort: { field: 'amount', dir: 'asc' } })

    expect(renderedNames()).toEqual(['Beta', 'Alphabet', 'Alpha'])
  })

  it('replaces its data when the rows change', async () => {
    const grid = await mountGrid()

    grid.setProps({ rows: [{ name: 'Gamma', amount: 9 }] })
    await settle()

    expect(renderedNames()).toEqual(['Gamma'])
  })

  it('moves the focusable scroll container under a rowgroup so the grid tree is valid', async () => {
    await mountGrid()

    expect(document.querySelector('.tabulator-tableholder')?.getAttribute('role')).toBe('rowgroup')
    expect(document.querySelector('.tabulator')?.hasAttribute('aria-owns')).toBe(false)
  })

  it('destroys Tabulator on unmount', async () => {
    const grid = await mountGrid()

    grid.unmount()
    mounted = undefined

    expect(document.querySelector('.tabulator')).toBeNull()
  })
})
