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
import {describe, expect, it} from 'vitest'
import {escapeHtml, inputHtml, isCustomised, type InputText} from '../customInputs'
import type {BuilderOperator, RuleContext} from '../types'

const operators: BuilderOperator[] = [
  {
    type: 'equal',
    label: 'is equal to',
    group: 'Comparison',
    arguments: [{name: 'Value', kind: 'value', many: false}],
    applyTo: ['string', 'integer', 'double'],
    template: ' = ?',
  },
  {
    type: 'has',
    label: 'has',
    group: 'Lists',
    arguments: [{name: 'Value', kind: 'field', many: false}],
    applyTo: ['list'],
    template: '.contains(?)',
  },
  {
    type: 'is_null',
    label: 'is null',
    group: 'Comparison',
    arguments: [],
    applyTo: ['string'],
    template: '.MatchIsNull()',
  },
]

const context: RuleContext = {
  operators,
  fieldTypes: {
    'Payload.Name': 'string',
    'Payload.Count': 'integer',
    'Payload.Total': 'integer',
    'Payload.Other': 'string',
    'List.Risk': 'list',
  },
}

const text: InputText = {
  placeholder: (argument, isDate) => (isDate ? `${argument.name} (date)` : argument.name),
  title: (placeholder, referable) => (referable ? `${placeholder} or a field` : placeholder),
}

describe('escapeHtml', () => {
  it('escapes everything that could close an attribute or open a tag', () => {
    expect(escapeHtml('"><img src=x onerror=alert(1)>&')).toBe(
      '&quot;&gt;&lt;img src=x onerror=alert(1)&gt;&amp;',
    )
  })
})

describe('inputHtml', () => {
  it('keeps numbers left-to-right and text with the direction of what is typed, in a right-to-left page', () => {
    expect(inputHtml(context, text, 'r1', 'Payload.Count', 'equal', 0)).toContain('dir="ltr"')
    expect(inputHtml(context, text, 'r1', 'Payload.Name', 'equal', 0)).toContain('dir="auto"')
  })

  it('offers a list or field argument as a left-to-right select of the fields of the right type', () => {
    const html = inputHtml(context, text, 'r1', 'List.Risk', 'has', 0)

    expect(html).toContain('<select class="select select-sm" dir="ltr"')
    expect(html).toContain('<option value="Payload.Name">Payload.Name</option>')
    expect(html).not.toContain('Payload.Count')
  })

  it('names an input after the rule and the argument and offers fields of its type as a datalist', () => {
    const html = inputHtml(context, text, 'r1', 'Payload.Count', 'equal', 0)

    expect(html).toContain('name="r1_value_0"')
    expect(html).toContain('list="r1_value_0_fields"')
    expect(html).toContain('title="Value or a field"')
    expect(html).toContain('<option value="Payload.Total">')
    expect(html).not.toContain('Payload.Other')
  })

  it('escapes a field name or an argument name that carries markup', () => {
    const hostile: RuleContext = {
      operators: [{...operators[0]!, arguments: [{name: '"><b>', kind: 'value', many: false}]}],
      fieldTypes: {'Payload.a"b': 'integer'},
    }

    const html = inputHtml(hostile, text, 'r1', 'Payload.a"b', 'equal', 0)

    expect(html).not.toContain('<b>')
    expect(html).toContain('Payload.a&quot;b')
  })
})

describe('inputHtml labels', () => {
  it('uses the label the caller gives an argument for the placeholder owner, the aria label and the select', () => {
    const labelled: InputText = {...text, label: () => 'القيمة'}

    expect(inputHtml(context, labelled, 'r1', 'Payload.Count', 'equal', 0)).toContain(
      'aria-label="القيمة"',
    )
    expect(inputHtml(context, labelled, 'r1', 'List.Risk', 'has', 0)).toContain(
      '<option value="">القيمة</option>',
    )
  })
})

describe('isCustomised', () => {
  it('customises an operator that takes values, but not a radio field or an operator with none', () => {
    expect(isCustomised(context, {operator: {type: 'equal'}, filter: {input: 'text'}})).toBe(
      true,
    )
    expect(isCustomised(context, {operator: {type: 'equal'}, filter: {input: 'radio'}})).toBe(
      false,
    )
    expect(
      isCustomised(context, {operator: {type: 'is_null'}, filter: {input: 'text'}}),
    ).toBe(false)
    expect(isCustomised(context, {filter: {input: 'text'}})).toBe(false)
  })
})
