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
  acceptsColumn,
  assignRole,
  columnRoles,
  dropMissingColumns,
  gridColumns,
  gridRows,
  isValid,
  sameMapping,
  sqlState,
} from '../state'
import { cardState, dataType, roleAccepts, templateKindId, type Introspection } from '../types'

const introspection = (valid: boolean, mappingError: string | null = null): Introspection => ({
  valid,
  columns: [],
  error: null,
  errorLine: null,
  errorColumn: null,
  kinds: [],
  resolvedMapping: [],
  mappingError,
  templateError: null,
})

describe('acceptsColumn', () => {
  it('lets any role take any type', () => {
    expect(acceptsColumn(roleAccepts.any, dataType.text)).toBe(true)
  })

  it('keeps text out of numeric roles', () => {
    expect(acceptsColumn(roleAccepts.numeric, dataType.text)).toBe(false)
    expect(acceptsColumn(roleAccepts.numeric, dataType.decimal)).toBe(true)
  })

  it('takes only time columns for time roles', () => {
    expect(acceptsColumn(roleAccepts.time, dataType.dateTime)).toBe(true)
    expect(acceptsColumn(roleAccepts.time, dataType.integer)).toBe(false)
  })

  it('takes either for numeric or time roles', () => {
    expect(acceptsColumn(roleAccepts.numericOrTime, dataType.dateTime)).toBe(true)
    expect(acceptsColumn(roleAccepts.numericOrTime, dataType.integer)).toBe(true)
    expect(acceptsColumn(roleAccepts.numericOrTime, dataType.text)).toBe(false)
  })
})

describe('assignRole', () => {
  it('moves a column out of its previous role', () => {
    const next = assignRole([{ roleId: 1, columns: ['a', 'b'] }], 2, ['a'])

    expect(next).toEqual([
      { roleId: 1, columns: ['b'] },
      { roleId: 2, columns: ['a'] },
    ])
  })

  it('removes a role emptied of columns', () => {
    expect(assignRole([{ roleId: 1, columns: ['a'] }], 1, [])).toEqual([])
  })

  it('orders roles by id', () => {
    const next = assignRole([{ roleId: 5, columns: ['x'] }], 2, ['y'])

    expect(next.map((entry) => entry.roleId)).toEqual([2, 5])
  })
})

describe('dropMissingColumns', () => {
  it('drops columns the query no longer returns', () => {
    const next = dropMissingColumns(
      [
        { roleId: 1, columns: ['a', 'gone'] },
        { roleId: 2, columns: ['gone'] },
      ],
      ['a'],
    )

    expect(next).toEqual([{ roleId: 1, columns: ['a'] }])
  })
})

describe('sameMapping', () => {
  it('ignores order and empty roles', () => {
    expect(
      sameMapping(
        [
          { roleId: 2, columns: ['b'] },
          { roleId: 1, columns: ['a'] },
          { roleId: 3, columns: [] },
        ],
        [
          { roleId: 1, columns: ['a'] },
          { roleId: 2, columns: ['b'] },
        ],
      ),
    ).toBe(true)
  })
})

describe('sqlState', () => {
  const base = { command: 'select 1', settled: 'select 1', fetching: false, failed: false }

  it('is incomplete when empty', () => {
    expect(sqlState({ ...base, command: ' ', introspection: undefined })).toBe('incomplete')
  })

  it('is checking while the debounce has not settled', () => {
    expect(sqlState({ ...base, settled: 'select', introspection: introspection(true) })).toBe(
      'checking',
    )
  })

  it('is disconnected when the call failed', () => {
    expect(sqlState({ ...base, failed: true, introspection: undefined })).toBe('disconnected')
  })

  it('is valid or errors by the response', () => {
    expect(sqlState({ ...base, introspection: introspection(true) })).toBe('valid')
    expect(sqlState({ ...base, introspection: introspection(false) })).toBe('errors')
  })
})

describe('isValid', () => {
  it('needs a chart or a grid', () => {
    const input = {
      state: 'valid' as const,
      introspection: introspection(true),
      displayKindId: 0,
      includeGrid: false,
    }

    expect(isValid(input)).toBe(false)
    expect(isValid({ ...input, includeGrid: true })).toBe(true)
    expect(isValid({ ...input, displayKindId: 3 })).toBe(true)
  })

  it('fails on a mapping error', () => {
    expect(
      isValid({
        state: 'valid',
        introspection: introspection(true, 'bad'),
        displayKindId: 3,
        includeGrid: false,
      }),
    ).toBe(false)
  })
})

describe('isValid for the template kind', () => {
  const ready = {
    state: 'valid' as const,
    introspection: introspection(true),
    displayKindId: templateKindId,
    includeGrid: false,
  }

  it('needs template text', () => {
    expect(isValid({ ...ready, templateText: '  ' })).toBe(false)
    expect(isValid({ ...ready, templateText: '<p>x</p>' })).toBe(true)
  })

  it('fails on a template error', () => {
    expect(
      isValid({
        ...ready,
        templateText: '<p>x</p>',
        introspection: { ...introspection(true), templateError: 'line 1' },
      }),
    ).toBe(false)
  })
})

describe('columnRoles', () => {
  it('names the role of each drawn column', () => {
    const kind = {
      kindId: 1,
      name: 'Bar',
      description: '',
      roles: [
        {
          roleId: 1,
          name: 'Category',
          description: '',
          many: false,
          optional: false,
          acceptsId: 1,
        },
      ],
    }
    const roles = columnRoles(kind, [{ roleId: 1, columns: ['region'], explicit: true }])

    expect(roles.get('region')).toEqual({ name: 'Category', explicit: true })
    expect(roles.get('other')).toBeUndefined()
  })
})

describe('grid helpers', () => {
  const labels = {
    yes: 'Yes',
    no: 'No',
    number: (value: number) => `#${value}`,
    dateTime: (value: string) => `@${value}`,
  }
  const grid = {
    columns: [
      { name: 'amount', ordinal: 1, dataTypeId: dataType.decimal },
      { name: 'flag', ordinal: 2, dataTypeId: dataType.flag },
    ],
    rows: [[10, true], [20]],
  }

  it('keys rows by column name and pads missing cells', () => {
    expect(gridRows(grid)).toEqual([
      { amount: 10, flag: true },
      { amount: 20, flag: null },
    ])
  })

  it('formats numbers and flags through the injected labels', () => {
    const [amount, flag] = gridColumns(grid, labels)

    expect(amount!.format!(10)).toBe('#10')
    expect(amount!.align).toBe('end')
    expect(flag!.badge!(true)).toEqual({ label: 'Yes', tone: 'success' })
    expect(flag!.badge!(0)).toEqual({ label: 'No', tone: 'neutral' })
  })

  it('exposes the card states', () => {
    expect(cardState.ready).toBe(1)
  })
})
