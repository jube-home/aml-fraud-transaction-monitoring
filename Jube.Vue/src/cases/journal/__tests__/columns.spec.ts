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
import { buildColumns, buildRows, groupLabel, type JournalContext } from '../columns'
import type { JournalColumn, JournalRow, NotableRow } from '../types'

const context: JournalContext = {
  t: (key) => key,
  number: (value) => String(value),
  dateTime: (value) => `at ${value}`,
  fixed: (value, digits = 2) => (value === null ? '–' : value.toFixed(digits)),
}

function column(overrides: Partial<JournalColumn> & { name: string }): JournalColumn {
  return {
    title: overrides.name,
    type: 'string',
    roleId: 2,
    roleInferred: true,
    lens: 'payload',
    group: 'payload',
    drill: false,
    ...overrides,
  }
}

const base: JournalColumn[] = [
  column({ name: 'Id', type: 'number', lens: 'base', group: 'base' }),
  column({ name: 'EntityAnalysisModelInstanceEntryGuid', lens: 'base', group: 'base' }),
  column({ name: 'ReferenceDate', type: 'date', lens: 'base', group: 'base' }),
  column({ name: 'CreatedDate', type: 'date', lens: 'base', group: 'base' }),
  column({ name: 'ResponseElevation', type: 'number', lens: 'base', group: 'base' }),
  column({ name: 'ResponseElevationName', lens: 'base', group: 'base' }),
  column({ name: 'HoldState', type: 'number', lens: 'base', group: 'base' }),
  column({ name: 'EntryKeyValue', lens: 'base', group: 'base' }),
  column({ name: 'Activations', lens: 'base', group: 'base' }),
  column({ name: 'ActivationCount', type: 'number', lens: 'base', group: 'base' }),
  column({ name: 'Tag', type: 'array', lens: 'base', group: 'base' }),
]

function drawn(grid: ReturnType<typeof buildColumns>, field: string, row: object): Element {
  return grid.find((c) => c.field === field)!.draw!(row)
}

describe('buildColumns for a held payment', () => {
  it('shows the range name beside the elevation and a state chip for a hold', () => {
    const grid = buildColumns(base, context, null)
    const row = { Id: 1, ResponseElevation: 12, ResponseElevationName: 'Review hold', HoldState: 1 }

    expect(drawn(grid, 'ResponseElevation', row).textContent).toBe('12 · Review hold')

    const chip = drawn(grid, 'HoldState', row)

    expect(chip.textContent).toBe('caseDetail.hold.state.held')
    expect(chip.className).toContain('badge-warning')
  })

  it('shows the bare elevation and no chip when nothing was held', () => {
    const grid = buildColumns(base, context, null)
    const row = { Id: 2, ResponseElevation: 3, ResponseElevationName: null, HoldState: null }

    expect(drawn(grid, 'ResponseElevation', row).textContent).toBe('3')
    expect(drawn(grid, 'HoldState', row).textContent).toBe('')
  })

  it('puts the payment column straight after the elevation', () => {
    const fields = buildColumns(base, context, null).map((c) => c.field)

    expect(fields.indexOf('HoldState')).toBe(fields.indexOf('ResponseElevation') + 1)
  })
})

describe('buildColumns', () => {
  it('offers the map as a second action only when the key can be located', () => {
    const plain = buildColumns(base, context, null)
    const located = buildColumns(base, { ...context, mappable: true }, null)

    expect(plain[0]!.actions!({}).map((action) => action.id)).toEqual(['open'])
    expect(located[0]!.actions!({}).map((action) => action.id)).toEqual(['open', 'map'])
    expect(located[0]!.width).toBeGreaterThan(plain[0]!.width!)
  })

  it('opens a row from a pinned action and hides the technical columns', () => {
    const grid = buildColumns([...base, column({ name: 'Amount', type: 'number' })], context, null)

    expect(grid[0]!.field).toBe('actions')
    expect(grid[0]!.pinned).toBe(true)
    expect(grid[0]!.actions!({})[0]!.id).toBe('open')

    const fields = grid.map((c) => c.field)

    expect(fields).not.toContain('Id')
    expect(fields).not.toContain('EntityAnalysisModelInstanceEntryGuid')
    expect(fields).not.toContain('CreatedDate')
    expect(fields).not.toContain('EntryKeyValue')
    expect(fields).not.toContain('ActivationCount')
  })

  it('puts when, elevation, payment and activations first, then what stood out, then the lens', () => {
    const grid = buildColumns([...base, column({ name: 'Amount', type: 'number' })], context, null)

    expect(grid.map((c) => c.field)).toEqual([
      'actions',
      'ReferenceDate',
      'ResponseElevation',
      'HoldState',
      'Activations',
      '__unusual',
      'Amount',
      'Tag',
    ])
  })

  it('adds the new-value column only when there is novelty to report', () => {
    expect(buildColumns(base, context, null).map((c) => c.field)).not.toContain('__novel')
    expect(buildColumns(base, context, []).map((c) => c.field)).not.toContain('__novel')
    expect(
      buildColumns(base, context, [{ field: 'Beneficiary', distinct: 3, recent: [] }]).map(
        (c) => c.field,
      ),
    ).toContain('__novel')
  })

  it('formats dates and numbers and leaves other values as text', () => {
    const grid = buildColumns(
      [
        column({ name: 'When', type: 'date' }),
        column({ name: 'Amount', type: 'number' }),
        column({ name: 'Rate', type: 'number' }),
        column({ name: 'Currency' }),
      ],
      context,
      null,
    )
    const byField = (field: string) => grid.find((c) => c.field === field)!

    expect(byField('When').format!('2026-10-24T00:00:00Z')).toBe('at 2026-10-24T00:00:00Z')
    expect(byField('When').format!(null)).toBe('')
    expect(byField('Amount').format!(120)).toBe('120')
    expect(byField('Rate').format!(0.5)).toBe('0.50')
    expect(byField('Amount').format!('x')).toBe('')
    expect(byField('Currency').format!(null)).toBe('')
    expect(byField('Amount').align).toBe('end')
  })

  it('prefixes a lens column with the facet it came from', () => {
    const grid = buildColumns(
      [
        column({
          name: 'abstraction:Volume',
          title: 'Volume',
          lens: 'features',
          group: 'abstraction',
        }),
        column({
          name: 'exhaustive:StructNet',
          title: 'StructNet',
          lens: 'models',
          group: 'exhaustive',
        }),
        column({ name: 'Amount' }),
      ],
      context,
      null,
    )

    expect(grid.find((c) => c.field === 'abstraction:Volume')!.title).toBe(
      'journal.group.abstraction: Volume',
    )
    expect(grid.find((c) => c.field === 'exhaustive:StructNet')!.title).toBe(
      'journal.group.exhaustive: StructNet',
    )
    expect(grid.find((c) => c.field === 'Amount')!.title).toBe('Amount')
  })

  it('draws a rule as fired or forced and nothing when it did not fire', () => {
    const grid = buildColumns(
      [column({ name: 'rule:Velocity', title: 'Velocity', lens: 'rules', group: 'rule' })],
      context,
      null,
    )

    expect(drawn(grid, 'rule:Velocity', { 'rule:Velocity': 'fired' }).textContent).toBe(
      'journal.rule.fired',
    )
    expect(drawn(grid, 'rule:Velocity', { 'rule:Velocity': 'forced' }).className).toContain(
      'badge-warning',
    )
    expect(drawn(grid, 'rule:Velocity', {}).textContent).toBe('')
  })

  it('draws tags as chips and an unusual value with its direction and size', () => {
    const grid = buildColumns(base, context, null)

    expect(drawn(grid, 'Tag', { Tag: ['mule', 'review'] }).querySelectorAll('span')).toHaveLength(2)
    expect(drawn(grid, 'Tag', { Tag: null }).querySelectorAll('span')).toHaveLength(0)

    const high = drawn(grid, '__unusual', { __unusual: 'Amount|4.2' })
    const low = drawn(grid, '__unusual', { __unusual: 'Amount|-3.1' })

    expect(high.textContent).toContain('Amount')
    expect(high.textContent).toContain('journal.unusual.above 4.2')
    expect(low.textContent).toContain('journal.unusual.below 3.1')
    expect(drawn(grid, '__unusual', { __unusual: '' }).textContent).toBe('')
  })

  it('labels every facet and none for the plain ones', () => {
    for (const group of [
      'abstraction',
      'calculation',
      'counter',
      'dictionary',
      'exhaustive',
      'advanced',
      'sanction',
      'http',
    ])
      expect(groupLabel(group, context.t)).toBe(`journal.group.${group}`)

    expect(groupLabel('payload', context.t)).toBe('')
  })
})

describe('buildRows', () => {
  const rows = [{ Id: 10 }, { Id: 11 }, { Id: 12 }] as JournalRow[]
  const notable: NotableRow[] = [
    {
      id: 11,
      score: 5,
      worst: 'Amount',
      reasons: [{ field: 'Amount', value: 9000, median: 100, z: 5 }],
    },
  ]

  it('marks the unusual rows with their worst field and the new values with their fields', () => {
    const built = buildRows(rows, notable, { '12': ['Beneficiary', 'Device'] }, context.t)

    expect(built.map((row) => row.__row)).toEqual([0, 1, 2])
    expect(built.map((row) => row.__unusual)).toEqual(['', 'Amount|5.0', ''])
    expect(built.map((row) => row.__novel)).toEqual(['', '', 'Beneficiary|Device'])
  })

  it('copes with no analytics at all', () => {
    expect(
      buildRows(rows, [], null, context.t).every(
        (row) => row.__unusual === '' && row.__novel === '',
      ),
    ).toBe(true)
  })

  it('carries the colours and the reasons a row was painted for', () => {
    const painted = [
      {
        Id: 1,
        BoldLine: {
          BoldLineKey: 'Device',
          BoldLineValue: 'D1',
          BoldLineFormatForeColor: '#000000',
          BoldLineFormatBackColor: '#ffff00',
        },
      },
      { Id: 2 },
    ] as JournalRow[]

    const built = buildRows(painted, [], null, context.t)

    expect(built[0]?.__paint?.row).toEqual({ fore: '#000000', back: '#ffff00', bold: true })
    expect(built[0]?.__paint?.reasons).toEqual(['journal.format.bold'])
    expect(built[1]?.__paint).toBeNull()
  })
})

describe('buildColumns formatting', () => {
  it('adds the reason column only when something is painted, and paints lens cells from the row', () => {
    const plain = buildColumns(base, context, null).map((column) => column.field)
    const withAmount = [...base, column({ name: 'Amount', type: 'number' })]
    const formatted = buildColumns(withAmount, { ...context, formatted: true }, null)

    expect(plain).not.toContain('__why')
    expect(formatted.map((column) => column.field)).toContain('__why')

    const cell = formatted.find((column) => column.field === 'Amount')

    expect(
      cell?.paint?.({
        __paint: { row: { bold: false }, cells: { Amount: { back: '#fff' } }, reasons: [] },
      }),
    ).toEqual({ back: '#fff' })
    expect(cell?.paint?.({ __paint: null })).toBeNull()
    expect(formatted.find((column) => column.field === '__why')?.paint).toBeUndefined()
  })
})
