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
import { filtersFromCompletions, showsBuilder } from '../filters'
import {
  dropUnknownFields,
  emptyRuleText,
  isoDate,
  itemError,
  ruleText,
  validateRuleValue,
} from '../ruleText'
import type { BuilderGroup, BuilderOperator, Completion, RuleContext } from '../types'

const operators: BuilderOperator[] = [
  {
    type: 'equal',
    label: 'is equal to',
    group: 'Comparison',
    arguments: [{ name: 'Value', kind: 'value', many: false }],
    applyTo: ['string', 'integer', 'double', 'datetime', 'boolean'],
    template: ' = ?',
  },
  {
    type: 'in',
    label: 'is in',
    group: 'Comparison',
    arguments: [{ name: 'Values', kind: 'value', many: true }],
    applyTo: ['string', 'integer'],
    template: '.In(?)',
  },
  {
    type: 'between',
    label: 'is between',
    group: 'Comparison',
    arguments: [
      { name: 'From', kind: 'value', many: false },
      { name: 'To', kind: 'value', many: false },
    ],
    applyTo: ['integer', 'double'],
    template: '.Between(?)',
  },
]

const completions: Completion[] = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: 'Name:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
  },
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: 'Amount:double',
    score: 1000,
    dataType: 'double',
    group: 'Payload',
  },
  {
    name: 'Payload.Count',
    value: 'Payload.Count',
    meta: 'Count:integer',
    score: 1000,
    dataType: 'integer',
    group: 'Payload',
  },
  {
    name: 'Payload.At',
    value: 'Payload.At',
    meta: 'At:datetime',
    score: 1000,
    dataType: 'datetime',
    group: 'Payload',
  },
  {
    name: 'Payload.Flag',
    value: 'Payload.Flag',
    meta: 'Flag:boolean',
    score: 1000,
    dataType: 'boolean',
    group: 'Payload',
  },
  {
    name: 'Payload.Blob',
    value: 'Payload.Blob',
    meta: 'Blob:json',
    score: 1000,
    dataType: 'json' as never,
    group: 'Payload',
  },
]

const { filters, context } = filtersFromCompletions(completions, operators)

describe('showsBuilder', () => {
  it.each([
    [1, false],
    [2, true],
    [3, true],
    [4, false],
    [5, true],
    [9, false],
  ])('parse type %i shows the builder: %s', (parseType, expected) => {
    expect(showsBuilder(parseType)).toBe(expected)
  })
})

describe('filtersFromCompletions', () => {
  it('skips fields with no applicable operator and records the types of the rest', () => {
    expect(filters.map((filter) => filter.id)).toEqual([
      'Payload.Name',
      'Payload.Amount',
      'Payload.Count',
      'Payload.At',
      'Payload.Flag',
    ])
    expect(context.fieldTypes['Payload.Flag']).toBe('boolean')
  })

  it('renders booleans as a Yes or No radio defaulting to True', () => {
    const flag = filters.find((filter) => filter.id === 'Payload.Flag')!

    expect(flag.input).toBe('radio')
    expect(flag.default_value).toBe('True')
    expect(flag.values).toEqual({ True: 'Yes', False: 'No' })
  })

  it('keeps only the first of duplicate names', () => {
    const duplicated = filtersFromCompletions([completions[0]!, completions[0]!], operators)

    expect(duplicated.filters).toHaveLength(1)
  })
})

describe('ruleText', () => {
  const group = (rules: BuilderGroup['rules'], condition: 'AND' | 'OR' = 'AND', not = false) =>
    ({ condition, not, rules }) as BuilderGroup

  it('writes a string comparison with a quoted, escaped literal', () => {
    const text = ruleText(
      context,
      group([{ id: 'Payload.Name', operator: 'equal', value: 'a "b"' }]),
    )

    expect(text).toBe('If (Payload.Name = "a ""b""") Then\n  Return True\nEnd If')
  })

  it('writes numbers unquoted and joins several values', () => {
    const text = ruleText(
      context,
      group([
        { id: 'Payload.Amount', operator: 'between', value: ['1', '2.5'] },
        { id: 'Payload.Count', operator: 'in', value: '1, 2,3' },
      ]),
    )

    expect(text).toContain('Payload.Amount.Between(1, 2.5) AND Payload.Count.In(1, 2, 3)')
  })

  it('writes dates through ToIsoDateTime in UTC', () => {
    const text = ruleText(
      context,
      group([{ id: 'Payload.At', operator: 'equal', value: '2026-01-31T00:00:00' }]),
    )

    expect(text).toContain('"2026-01-31T00:00:00Z".ToIsoDateTime()')
  })

  it('writes a field reference without quoting it', () => {
    const text = ruleText(
      context,
      group([{ id: 'Payload.Amount', operator: 'equal', value: 'Payload.Count' }]),
    )

    expect(text).toContain('Payload.Amount = Payload.Count')
  })

  it('nests groups and wraps a negated group in NOT', () => {
    const text = ruleText(
      context,
      group(
        [
          { id: 'Payload.Count', operator: 'equal', value: '1' },
          group([{ id: 'Payload.Count', operator: 'equal', value: '2' }], 'OR', true),
        ],
        'OR',
      ),
    )

    expect(text).toContain('Payload.Count = 1 OR ( NOT ( Payload.Count = 2 ) ) ')
  })

  it('throws on an unknown operator so the caller can fall back', () => {
    expect(() =>
      ruleText(context, group([{ id: 'Payload.Name', operator: 'nope', value: 'x' }])),
    ).toThrow()
    expect(emptyRuleText).toBe('Return False')
  })
})

describe('isoDate', () => {
  it.each([
    ['2026-01-31T00:00:00', '2026-01-31T00:00:00Z'],
    ['2026-01-31', '2026-01-31T00:00:00Z'],
    ['2026-01-31T10:30:00+01:00', '2026-01-31T09:30:00Z'],
  ])('normalises %s', (input, expected) => {
    expect(isoDate(input)).toBe(expected)
  })

  it('rejects text that is not a date', () => {
    expect(isoDate('tomorrow')).toBeNull()
  })
})

describe('itemError', () => {
  const ctx: RuleContext = { operators, fieldTypes: { 'Payload.Count': 'integer' } }

  it.each([
    ['integer', '12', null],
    ['integer', '1.5', 'integer'],
    ['integer', 'Payload.Count', null],
    ['number', 'abc', 'number'],
    ['number', '1e3', null],
    ['date', 'nonsense', 'date'],
    ['text', 'line\nbreak', 'controlCharacters'],
    ['text', 'fine', null],
    ['integer', '  ', 'needed'],
  ] as const)('%s %j gives %s', (kind, item, expected) => {
    expect(itemError(ctx, kind, undefined, item)).toBe(expected)
  })
})

describe('validateRuleValue', () => {
  const between = operators[2]!

  it('names the argument that is missing or wrong', () => {
    expect(validateRuleValue(context, between, 'Payload.Amount', ['1', ''])).toEqual({
      argument: 'To',
      code: 'needed',
    })
    expect(validateRuleValue(context, between, 'Payload.Amount', ['x', '2'])).toEqual({
      argument: 'From',
      code: 'number',
    })
  })

  it('accepts a complete value', () => {
    expect(validateRuleValue(context, between, 'Payload.Amount', ['1', '2'])).toBeNull()
  })
})

describe('dropUnknownFields', () => {
  it('removes rules for fields that no longer exist, at any depth, and lists them', () => {
    const stored: BuilderGroup = {
      condition: 'AND',
      rules: [
        { id: 'Payload.Gone', operator: 'equal', value: '1' },
        {
          condition: 'OR',
          rules: [
            { id: 'Payload.Count', operator: 'equal', value: '1' },
            { id: 'Payload.AlsoGone', operator: 'equal', value: '2' },
          ],
        },
      ],
    }

    const result = dropUnknownFields(stored, (id) => id === 'Payload.Count')

    expect(result.dropped).toEqual(['Payload.Gone', 'Payload.AlsoGone'])
    expect(JSON.stringify(result.group)).not.toContain('Gone')
    expect(JSON.stringify(result.group)).toContain('Payload.Count')
  })
})

describe('lenient rule text', () => {
  const lenientGroup = (rules: BuilderGroup['rules']) =>
    ({ condition: 'AND', rules }) as BuilderGroup

  it('writes a rule with a missing value as far as it goes so the compiler can fail it', () => {
    const text = ruleText(
      context,
      lenientGroup([{ id: 'Payload.Amount', operator: 'between', value: ['1', ''] }]),
      true,
    )

    expect(text).toContain('Payload.Amount.Between(1, )')
  })

  it('leaves a rule with no value at all empty rather than writing undefined', () => {
    const text = ruleText(context, lenientGroup([{ id: 'Payload.Count', operator: 'equal' }]), true)

    expect(text).toContain('Payload.Count = ')
    expect(text).not.toContain('undefined')
  })

  it('skips rules with no field or an unknown operator and groups left empty', () => {
    const text = ruleText(
      context,
      lenientGroup([
        { id: '', operator: 'equal', value: 'x' },
        { id: 'Payload.Name', operator: 'nope', value: 'x' },
        lenientGroup([]),
        { id: 'Payload.Count', operator: 'equal', value: '2' },
      ]),
      true,
    )

    expect(text).toBe('If (Payload.Count = 2) Then\n  Return True\nEnd If')
  })

  it('is empty, not Return False, when nothing has been defined', () => {
    expect(ruleText(context, lenientGroup([]), true)).toBe('')
    expect(ruleText(context, lenientGroup([{ id: '', operator: '' }]), true)).toBe('')
  })

  it('still refuses an unknown operator when strict', () => {
    expect(() =>
      ruleText(context, lenientGroup([{ id: 'Payload.Name', operator: 'nope', value: 'x' }])),
    ).toThrow()
  })
})
