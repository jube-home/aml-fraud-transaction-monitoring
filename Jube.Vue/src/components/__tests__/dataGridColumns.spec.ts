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

import { describe, expect, it, vi } from 'vitest'
import { csvSafe, resolveAlign, toTabulatorColumns } from '../dataGridColumns'
import type { GridColumn } from '../dataGridTypes'

function cell(value: unknown) {
  return { getValue: () => value } as never
}

function formatterOf(definition: ReturnType<typeof toTabulatorColumns>[number], value: unknown) {
  const formatter = definition.formatter as unknown as (cell: never) => HTMLElement

  return formatter(cell(value))
}

describe('resolveAlign', () => {
  it.each([
    ['start', 'ltr', 'left'],
    ['end', 'ltr', 'right'],
    ['start', 'rtl', 'right'],
    ['end', 'rtl', 'left'],
    ['center', 'ltr', 'center'],
    ['center', 'rtl', 'center'],
  ] as const)('maps %s in %s to %s', (align, direction, expected) => {
    expect(resolveAlign(align, direction)).toBe(expected)
  })

  it('leaves alignment to Tabulator when none is given', () => {
    expect(resolveAlign(undefined, 'ltr')).toBeUndefined()
  })
})

describe('toTabulatorColumns', () => {
  const columns: GridColumn[] = [
    { field: 'id', title: 'Case', mono: true, width: 90 },
    { field: 'amount', title: 'Amount', align: 'end', format: (value) => `£${String(value)}` },
    { field: 'risk', title: 'Risk', badge: () => ({ label: 'High', tone: 'error' }) },
  ]

  it('carries title, field, width and monospace across', () => {
    const [id] = toTabulatorColumns(columns, 'ltr')

    expect(id).toMatchObject({
      title: 'Case',
      field: 'id',
      width: 90,
      cssClass: 'font-mono tabular-nums',
    })
    expect(id?.formatter).toBeUndefined()
  })

  it('aligns the header and the cells together and mirrors them for right-to-left', () => {
    expect(toTabulatorColumns(columns, 'ltr')[1]).toMatchObject({
      hozAlign: 'right',
      headerHozAlign: 'right',
    })
    expect(toTabulatorColumns(columns, 'rtl')[1]).toMatchObject({
      hozAlign: 'left',
      headerHozAlign: 'left',
    })
  })

  it('renders formatted text as a text node, never as markup', () => {
    const element = formatterOf(
      toTabulatorColumns(columns, 'ltr')[1]!,
      '<img src=x onerror=alert(1)>',
    )

    expect(element.tagName).toBe('SPAN')
    expect(element.textContent).toBe('£<img src=x onerror=alert(1)>')
    expect(element.querySelector('img')).toBeNull()
  })

  it('renders a badge with its tone class and a word', () => {
    const element = formatterOf(toTabulatorColumns(columns, 'ltr')[2]!, 'high')

    expect(element.textContent).toBe('High')
    expect(element.className).toContain('badge-error')
    expect(element.className).not.toContain('badge-soft')
  })

  it('never uses a soft or outline style for fill-only tones, so their colour is never text', () => {
    for (const tone of ['success', 'warning', 'error'] as const) {
      const element = formatterOf(
        toTabulatorColumns(
          [{ field: 'x', title: 'X', badge: () => ({ label: tone, tone }) }],
          'ltr',
        )[0]!,
        null,
      )

      expect(element.className).toContain(`badge-${tone}`)
      expect(element.className).not.toMatch(/badge-(soft|outline|ghost)/)
    }
  })

  it('gives every tone its own class', () => {
    const tones = ['neutral', 'info', 'success', 'warning', 'error'] as const
    const classes = tones.map(
      (tone) =>
        formatterOf(
          toTabulatorColumns(
            [{ field: 'x', title: 'X', badge: () => ({ label: tone, tone }) }],
            'ltr',
          )[0]!,
          null,
        ).className,
    )

    expect(new Set(classes).size).toBe(5)
  })
})

describe('template cells', () => {
  function rowCell(row: object, value: unknown = undefined) {
    return { getValue: () => value, getRow: () => ({ getData: () => row }) } as never
  }

  function render(
    column: GridColumn,
    row: object,
    handlers: Parameters<typeof toTabulatorColumns>[2] = {},
    value: unknown = undefined,
  ) {
    const definition = toTabulatorColumns([column], 'ltr', handlers)[0]!
    const formatter = definition.formatter as unknown as (cell: never) => HTMLElement

    return { definition, element: formatter(rowCell(row, value)) }
  }

  it('builds one native button per action and reports the click with its row', () => {
    const onAction = vi.fn()
    const row = { id: 7 }
    const { definition, element } = render(
      {
        field: '',
        title: '',
        actions: () => [
          { id: 'approve', label: 'Approve', tone: 'success' },
          { id: 'reject', label: 'Reject', tone: 'error', disabled: true },
        ],
      },
      row,
      { onAction },
    )
    const buttons = element.querySelectorAll('button')

    expect(definition.headerSort).toBe(false)
    expect([...buttons].map((button) => button.textContent)).toEqual(['Approve', 'Reject'])
    expect(buttons[0]!.className).toContain('btn-success')
    expect(buttons[1]!.disabled).toBe(true)

    buttons[0]!.click()

    expect(onAction).toHaveBeenCalledWith({ id: 'approve', row })
  })

  it('passes minimum width and end pinning through to Tabulator', () => {
    const definition = toTabulatorColumns(
      [{ field: 'a', title: 'A', minWidth: 200, pinned: true }],
      'ltr',
    )[0]!

    expect(definition.minWidth).toBe(200)
    expect(definition.frozen).toBe(true)
  })

  it('draws a custom element for the row and keeps the column unsortable', () => {
    const row = { id: 1 }
    const draw = vi.fn(() => document.createElement('i'))
    const { definition, element } = render({ field: 'h', title: 'H', draw }, row)

    expect(definition.headerSort).toBe(false)
    expect(element.tagName).toBe('I')
    expect(draw).toHaveBeenCalledWith(row)
  })

  it('renders no buttons for a row with no actions', () => {
    const { element } = render({ field: '', title: '', actions: () => [] }, {})

    expect(element.querySelectorAll('button')).toHaveLength(0)
  })

  it('builds a labelled text input and reports what is typed', () => {
    const onInput = vi.fn()
    const row = { id: 1 }
    const { element } = render(
      {
        field: 'note',
        title: 'Note',
        input: () => ({
          placeholder: 'Note',
          label: 'Checker note',
          value: 'kept',
          prefix: 'Earlier',
        }),
      },
      row,
      { onInput },
    )
    const input = element.querySelector('input')!

    expect(input.value).toBe('kept')
    expect(input.getAttribute('aria-label')).toBe('Checker note')
    expect(element.textContent).toContain('Earlier')

    input.value = 'typed'
    input.dispatchEvent(new Event('input'))

    expect(onInput).toHaveBeenCalledWith({ field: 'note', value: 'typed', row })
  })

  it('builds typed inputs and formats the fallback text with the column format', () => {
    const typed = render(
      { field: 'n', title: 'N', input: () => ({ placeholder: 'N', label: 'N', type: 'number' }) },
      {},
    ).element.querySelector('input')!

    expect(typed.type).toBe('number')
    expect(typed.step).toBe('any')

    const { element } = render(
      { field: 'd', title: 'D', format: (value) => `on ${String(value)}`, input: () => null },
      {},
      {},
      '2026',
    )

    expect(element.textContent).toBe('on 2026')
  })

  it('falls back to the cell text when a row has no input', () => {
    const { element } = render({ field: 'note', title: 'Note', input: () => null }, {}, {}, 'plain')

    expect(element.textContent).toBe('plain')
  })
})

describe('csvSafe', () => {
  it.each([
    ['=SUM(A1:A2)', "'=SUM(A1:A2)"],
    ['+1+1', "'+1+1"],
    ['-cmd', "'-cmd"],
    ['@SUM(1)', "'@SUM(1)"],
    ['\tTabbed', "'\tTabbed"],
    ['\rReturn', "'\rReturn"],
  ])('neutralises the formula start of %j', (value, expected) => {
    expect(csvSafe(value)).toBe(expected)
  })

  it.each([['plain'], ['a=b'], ['12'], ['-12'], ['+3.5'], [''], [5], [-5], [null], [true]])(
    'leaves %j alone',
    (value) => {
      expect(csvSafe(value)).toBe(value)
    },
  )
})

describe('csv columns', () => {
  it('guards data columns and leaves button, input and drawn columns out of the export', () => {
    const [data, actions, input, draw] = toTabulatorColumns(
      [
        { field: 'a', title: 'A' },
        { field: '', title: 'Actions', actions: () => [] },
        { field: 'n', title: 'N', input: () => null },
        { field: 'h', title: 'H', draw: () => document.createElement('i') },
      ],
      'ltr',
    )

    expect(data!.accessorDownload).toBe(csvSafe)
    expect(data!.download).toBeUndefined()
    expect([actions, input, draw].map((definition) => definition!.download)).toEqual([
      false,
      false,
      false,
    ])
  })
})
