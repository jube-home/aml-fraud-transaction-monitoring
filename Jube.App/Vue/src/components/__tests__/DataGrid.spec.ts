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

import {afterEach, beforeEach, describe, expect, it} from 'vitest'
import {nextTick} from 'vue'
import {TabulatorFull} from 'tabulator-tables'
import DataGrid from '../DataGrid.vue'
import DataGridSummaryHost from './DataGridSummaryHost.vue'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'

const columns = [
  {title: 'Name', field: 'name'},
  {title: 'Amount', field: 'amount'},
]

const rows = [
  {name: 'Alpha', amount: 3},
  {name: 'Beta', amount: 1},
  {name: 'Alphabet', amount: 2},
]

let mounted: MountedVapor | undefined

async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 20))
}

async function mountGrid(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  mounted = mountVapor(DataGrid, {
    props: {columns, rows, emptyText: 'Nothing here', label: 'Test grid', ...props},
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
    const grid = await mountGrid({filterField: 'name', filterText: 'alpha'})

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([2])
  })

  it('re-filters when the filter text changes', async () => {
    const grid = await mountGrid({filterField: 'name', filterText: ''})

    grid.setProps({filterText: 'beta'})
    await settle()

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([1])

    grid.setProps({filterText: ''})
    await settle()

    expect(grid.emitted.visibleRows?.at(-1)).toEqual([3])
  })

  it('applies an initial sort', async () => {
    await mountGrid({sort: {field: 'amount', dir: 'asc'}})

    expect(renderedNames()).toEqual(['Beta', 'Alphabet', 'Alpha'])
  })

  it('replaces its data when the rows change', async () => {
    const grid = await mountGrid()

    grid.setProps({rows: [{name: 'Gamma', amount: 9}]})
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

  describe('summary slot', () => {
    const summaryRows = [
      {id: 1, name: 'Alpha'},
      {id: 2, name: 'Beta'},
    ]

    const heights = ['clientHeight', 'offsetHeight'] as const
    const originals = heights.map((name) =>
      Object.getOwnPropertyDescriptor(HTMLElement.prototype, name),
    )

    beforeEach(() => {
      for (const name of heights) {
        Object.defineProperty(HTMLElement.prototype, name, {configurable: true, value: 600})
      }

      return () => {
        heights.forEach((name, index) => {
          const original = originals[index]

          if (original) Object.defineProperty(HTMLElement.prototype, name, original)
          else delete (HTMLElement.prototype as unknown as Record<string, unknown>)[name]
        })
      }
    })

    async function mountSummaryGrid(): Promise<void> {
      mounted = mountVapor(DataGridSummaryHost, {
        props: {columns: [{title: 'Name', field: 'name'}], rows: summaryRows},
      })
      await settle()
    }

    function expanders(): HTMLButtonElement[] {
      return [...document.querySelectorAll<HTMLButtonElement>('[data-testid="grid-expand"]')]
    }

    function bodies(): string[] {
      return [...document.querySelectorAll('[data-testid="summary-body"]')].map(
        (node) => node.textContent ?? '',
      )
    }

    it('adds an expander column per row when a summary slot is supplied', async () => {
      await mountSummaryGrid()

      expect(expanders()).toHaveLength(2)
      expect(expanders()[0]?.getAttribute('aria-expanded')).toBe('false')
      expect(expanders()[0]?.getAttribute('aria-label')).toBe('Show summary')
      expect(bodies()).toEqual([])
    })

    it('renders the slot for the expanded row with its id and data', async () => {
      await mountSummaryGrid()

      expanders()[1]!.click()
      await settle()

      expect(bodies()).toEqual(['2:Beta'])
      expect(expanders()[1]?.getAttribute('aria-expanded')).toBe('true')
      expect(expanders()[1]?.getAttribute('aria-label')).toBe('Hide summary')
    })

    it('keeps several rows open independently', async () => {
      await mountSummaryGrid()

      expanders()[0]!.click()
      expanders()[1]!.click()
      await settle()

      expect(bodies().sort()).toEqual(['1:Alpha', '2:Beta'])
    })

    it('removes the summary when the row is collapsed again', async () => {
      await mountSummaryGrid()

      expanders()[0]!.click()
      await settle()
      expanders()[0]!.click()
      await settle()

      expect(bodies()).toEqual([])
      expect(expanders()[0]?.getAttribute('aria-expanded')).toBe('false')
    })

    it('keeps a visible reset in the header that is live only while something is open, and collapses every open row', async () => {
      await mountSummaryGrid()

      const reset = () =>
        document.querySelector<HTMLButtonElement>('[data-testid="grid-reset"]')

      expect(reset()).not.toBeNull()
      expect(reset()!.disabled).toBe(true)

      expanders()[0]!.click()
      expanders()[1]!.click()
      await settle()

      expect(reset()!.disabled).toBe(false)
      expect(bodies()).toHaveLength(2)

      reset()!.click()
      await settle()

      expect(bodies()).toEqual([])
      expect(expanders().map((button) => button.getAttribute('aria-expanded'))).toEqual([
        'false',
        'false',
      ])
      expect(reset()!.disabled).toBe(true)
    })

    it('shows no expander column when no summary slot or expandable flag is given', async () => {
      await mountGrid()

      expect(expanders()).toHaveLength(0)
    })
  })
})
