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
import { describe, expect, it, afterAll, beforeAll } from 'vitest'
import { allow, fieldState, holds } from '../conditions'
import { groupTree } from '../groups'
import {
  changedFields,
  comparable,
  editableFields,
  fromInput,
  isLocked,
  newRecord,
  payloadOf,
  problemsOf,
  toInput,
} from '../values'
import type { FormCondition, FormField, FormModel } from '../types'

const field = (overrides: Partial<FormField> = {}): FormField => ({
  name: 'name',
  label: 'Name',
  description: '',
  type: 'string',
  nullable: true,
  required: false,
  minLength: null,
  maxLength: null,
  min: null,
  max: null,
  exclusiveMin: false,
  exclusiveMax: false,
  patterns: [],
  options: [],
  group: '',
  order: 0,
  widget: 'default',
  span: 1,
  readOnly: false,
  placeholder: null,
  help: null,
  editor: null,
  lookup: null,
  newDefault: null,
  visibleWhen: [],
  requiredWhen: [],
  enabledWhen: [],
  ...overrides,
})

const condition = (overrides: Partial<FormCondition>): FormCondition => ({
  property: 'mode',
  op: 'equals',
  values: ['a'],
  matchMode: 'all',
  ...overrides,
})

const model: FormModel = {
  area: 'Thing',
  endpoint: '/api/Thing',
  operations: ['list', 'read', 'create', 'update', 'delete'],
  keys: { id: 'id', parent: 'parentId', naturalKey: 'name' },
  approval: null,
  roles: null,
  values: null,
  editors: [],
  children: [],
  lockFields: ['locked'],
  groups: [
    { path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: 'default' },
    { path: 'Advanced', label: 'Advanced', order: 20, collapsed: true, widget: 'default' },
    { path: 'Advanced/Limits', label: 'Limits', order: 21, collapsed: false, widget: 'default' },
  ],
  fields: [
    field({ name: 'name', group: 'Identity' }),
    field({
      name: 'active',
      type: 'boolean',
      widget: 'switch',
      newDefault: true,
      group: 'Identity',
    }),
    field({ name: 'locked', type: 'boolean', widget: 'switch', group: 'Identity' }),
    field({ name: 'limit', type: 'integer', group: 'Advanced/Limits' }),
    field({ name: 'when', type: 'datetime', group: 'Advanced' }),
    field({ name: 'id', type: 'integer', readOnly: true, group: 'Advanced' }),
    field({ name: 'loose' }),
  ],
  listColumns: [],
}

describe('holds', () => {
  it.each([
    [condition({ op: 'equals', values: ['A'] }), { mode: 'a' }, true],
    [condition({ op: 'equals', values: [true] }), { mode: 'True' }, true],
    [condition({ op: 'notEquals', values: ['a'] }), { mode: 'a' }, false],
    [condition({ op: 'in', values: ['a', 'b'] }), { mode: 'b' }, true],
    [condition({ op: 'notIn', values: ['a', 'b'] }), { mode: 'c' }, true],
    [condition({ op: 'greaterThan', values: [5] }), { mode: 6 }, true],
    [condition({ op: 'greaterThan', values: [5] }), { mode: '5' }, false],
    [condition({ op: 'isEmpty', values: [] }), { mode: '' }, true],
    [condition({ op: 'isEmpty', values: [] }), { mode: null }, true],
    [condition({ op: 'isNotEmpty', values: [] }), { mode: 0 }, true],
  ])('%j against %j is %s', (cond, values, expected) => {
    expect(holds(cond, values)).toBe(expected)
  })
})

describe('allow and fieldState', () => {
  it('allows when there are no conditions', () => {
    expect(allow([], {})).toBe(true)
  })

  it('needs every condition by default and any one when the match mode says so', () => {
    const both = [condition({ values: ['a'] }), condition({ property: 'other', values: ['x'] })]

    expect(allow(both, { mode: 'a', other: 'y' })).toBe(false)
    expect(
      allow(
        both.map((c) => ({ ...c, matchMode: 'any' as const })),
        { mode: 'a', other: 'y' },
      ),
    ).toBe(true)
  })

  it('works out visible, required and enabled independently', () => {
    const state = fieldState(
      field({
        visibleWhen: [condition({ values: ['a'] })],
        requiredWhen: [condition({ values: ['b'] })],
        enabledWhen: [condition({ values: ['c'] })],
      }),
      { mode: 'a' },
    )

    expect(state).toEqual({ visible: true, required: false, enabled: false })
  })

  it('is visible, optional and enabled with no conditions', () => {
    expect(fieldState(field(), {})).toEqual({ visible: true, required: false, enabled: true })
  })
})

describe('values', () => {
  it('starts a new record from the declared defaults, then the type defaults', () => {
    expect(newRecord(model)).toMatchObject({
      name: '',
      active: true,
      locked: false,
      limit: null,
      when: null,
    })
  })

  it('lists the editable fields, not read-only ones or the lock field', () => {
    expect(editableFields(model).map((f) => f.name)).toEqual([
      'name',
      'active',
      'limit',
      'when',
      'loose',
    ])
  })

  it('is locked when any lock field is true', () => {
    expect(isLocked(model, { locked: true })).toBe(true)
    expect(isLocked(model, { locked: false })).toBe(false)
    expect(isLocked(model, {})).toBe(false)
  })

  it('converts between input text and typed values', () => {
    const number = field({ type: 'number' })
    const date = field({ type: 'datetime' })

    expect(fromInput(number, '')).toBeNull()
    expect(fromInput(number, '2.5')).toBe(2.5)
    expect(fromInput(number, 'x')).toBe('x')
    expect(fromInput(date, '2026-10-20T10:30')).toBe(new Date('2026-10-20T10:30').toISOString())
    expect(fromInput(date, '')).toBeNull()
    expect(fromInput(field(), 'text')).toBe('text')
    expect(toInput(date, new Date('2026-10-20T10:30').toISOString())).toBe('2026-10-20T10:30')
    expect(toInput(number, null)).toBe('')
  })

  it('reports a number field holding text', () => {
    expect(problemsOf(model, { limit: 'x' })).toEqual([{ field: 'limit', code: 'number' }])
    expect(problemsOf(model, { limit: 4 })).toEqual([])
    expect(problemsOf(model, { limit: '' })).toEqual([])
  })

  it('builds a payload from the stored record, the edits, the lock field and the parent', () => {
    const body = payloadOf(
      model,
      { name: 'new', active: false, locked: true, id: 99 },
      { id: 7, version: 3, guid: 'g', name: 'old', locked: false },
      12,
    )

    expect(body).toMatchObject({
      id: 7,
      version: 3,
      guid: 'g',
      name: 'new',
      active: false,
      locked: true,
      parentId: 12,
    })
  })

  it('leaves the parent key alone when there is no parent', () => {
    expect(payloadOf(model, { name: 'a' }, undefined, undefined)).not.toHaveProperty('parentId')
  })

  it('compares only editable fields', () => {
    expect(comparable(model, { name: 'a', id: 1 })).toBe(comparable(model, { name: 'a', id: 2 }))
    expect(comparable(model, { name: 'a' })).not.toBe(comparable(model, { name: 'b' }))
  })
})

describe('groupTree and editors', () => {
  it('draws a composite editor once, at its first claimed field, and drops the other claimed fields', () => {
    const tree = groupTree({
      ...model,
      editors: [
        {
          kind: 'ruleBuilder',
          parseType: 2,
          roles: { builder: 'when', coder: 'limit', builderJson: 'name' },
          reads: {},
        },
      ],
    })
    const identity = tree.find((node) => node.path === 'Identity')!
    const advanced = tree.find((node) => node.path === 'Advanced')!

    expect(identity.fields.map((f) => f.name)).toEqual(['name', 'active', 'locked'])
    expect(advanced.fields.map((f) => f.name)).toEqual(['id'])
    expect(advanced.children[0]!.fields).toEqual([])
  })
})

describe('groupTree', () => {
  it('nests a path under its parent and keeps loose fields in an unnamed first group', () => {
    const tree = groupTree(model)

    expect(tree.map((node) => node.path)).toEqual(['', 'Identity', 'Advanced'])
    expect(tree[0]!.fields.map((f) => f.name)).toEqual(['loose'])
    expect(tree[2]!.collapsed).toBe(true)
    expect(tree[2]!.children.map((node) => node.path)).toEqual(['Advanced/Limits'])
    expect(tree[2]!.children[0]!.fields.map((f) => f.name)).toEqual(['limit'])
  })

  it('invents a group for a field that names one the model never declared', () => {
    const tree = groupTree({ ...model, groups: [], fields: [field({ name: 'a', group: 'Odd' })] })

    expect(tree.map((node) => node.label)).toEqual(['Odd'])
  })
})

describe('wire values', () => {
  const guid = field({ name: 'workflowGuid', type: 'guid', nullable: false })

  it('sends the empty guid, zero or empty text for a blank value the contract cannot take as null', async () => {
    const { wireValue } = await import('../values')

    expect(wireValue(guid, '')).toBe('00000000-0000-0000-0000-000000000000')
    expect(wireValue(guid, null)).toBe('00000000-0000-0000-0000-000000000000')
    expect(wireValue(field({ type: 'integer', nullable: false }), null)).toBe(0)
    expect(wireValue(field({ type: 'number', nullable: false }), '')).toBe(0)
    expect(wireValue(field({ type: 'string', nullable: false }), null)).toBe('')
  })

  it('keeps a real value and leaves a nullable field null', async () => {
    const { wireValue } = await import('../values')

    expect(wireValue(guid, 'aaaa')).toBe('aaaa')
    expect(wireValue(field({ type: 'integer', nullable: false }), 7)).toBe(7)
    expect(wireValue(field({ type: 'guid', nullable: true }), '')).toBeNull()
    expect(wireValue(field({ type: 'string', nullable: true }), '')).toBe('')
  })
})

describe('payload of a record with blank non-nullable values', () => {
  it('sends the empty guid and zero, not null, so the server can bind the body', () => {
    const subject = {
      keys: { id: 'id', parent: null },
      lockFields: [],
      fields: [
        field({ name: 'workflowGuid', type: 'guid', nullable: false }),
        field({ name: 'priority', type: 'number', nullable: false }),
        field({ name: 'note', type: 'string', nullable: true }),
      ],
    } as unknown as FormModel

    expect(
      payloadOf(subject, { workflowGuid: '', priority: null, note: null }, undefined, undefined),
    ).toEqual({
      workflowGuid: '00000000-0000-0000-0000-000000000000',
      priority: 0,
      note: null,
    })
  })
})

describe('changed fields', () => {
  const subject = {
    fields: [field({ name: 'name' }), field({ name: 'limit' }), field({ name: 'note' })],
  } as unknown as FormModel

  it('names the fields whose value differs, treating missing and null alike', () => {
    expect(
      changedFields(subject, { name: 'a', limit: 1, note: null }, { name: 'b', limit: 1 }),
    ).toEqual(['name'])
    expect(changedFields(subject, { name: 'a' }, { name: 'a' })).toEqual([])
    expect(changedFields(subject, { limit: 1 }, { limit: 2, note: 'x' })).toEqual(['limit', 'note'])
  })
})

describe('datetime fields in a zone away from UTC', () => {
  const zone = process.env.TZ
  const date = field({ type: 'datetime' })

  beforeAll(() => {
    process.env.TZ = 'America/New_York'
  })

  afterAll(() => {
    if (zone === undefined) delete process.env.TZ
    else process.env.TZ = zone
  })

  it('shows the stored instant in local time, not as UTC', () => {
    expect(toInput(date, '2026-10-20T14:30:00Z')).toBe('2026-10-20T10:30')
  })

  it('stores what was typed as the instant it means locally', () => {
    expect(fromInput(date, '2026-10-20T10:30')).toBe('2026-10-20T14:30:00.000Z')
  })
})
