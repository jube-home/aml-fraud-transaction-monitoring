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
import type { BuilderGroup } from '../../../ruleBuilder/types'
import { describeFilter, parseFilter, ruleCount, serialiseFilter } from '../filter'

const rule = (id: string, operator: string, value?: unknown) => ({ id, operator, value })

const group = (condition: 'AND' | 'OR', ...rules: BuilderGroup['rules']): BuilderGroup => ({
  condition,
  rules,
})

describe('the archive filter', () => {
  it('counts rules through nested groups', () => {
    expect(ruleCount(null)).toBe(0)
    expect(
      ruleCount(
        group('AND', rule('Payload.A', 'equal', 1), group('OR', rule('Payload.B', 'like', 'x'))),
      ),
    ).toBe(2)
  })

  it('keeps only what the server understands when it writes the filter', () => {
    const text = serialiseFilter({
      condition: 'AND',
      not: true,
      rules: [
        { id: 'Payload.A', operator: 'equal', value: 'x', field: 'sql', type: 'string' } as never,
        group('OR'),
      ],
    })

    expect(text).toBe(
      '{"condition":"AND","rules":[{"id":"Payload.A","operator":"equal","value":"x"}]}',
    )
  })

  it('writes nothing for an empty filter', () => {
    expect(serialiseFilter(null)).toBeUndefined()
    expect(serialiseFilter(group('AND'))).toBeUndefined()
  })

  it('reads back what it wrote', () => {
    const original = group(
      'OR',
      rule('Payload.A', 'greater', 5),
      group('AND', rule('Payload.B', 'not_like', 'x%')),
    )

    expect(parseFilter(serialiseFilter(original))).toEqual(original)
  })

  it.each([
    undefined,
    '',
    'not json',
    '[]',
    '{"condition":"XOR","rules":[]}',
    '{"condition":"AND","rules":[]}',
    '{"condition":"AND","rules":[{"id":1,"operator":"equal"}]}',
    '{"condition":"AND","rules":[{"id":"A"}]}',
  ])('refuses %j', (raw) => {
    expect(parseFilter(raw)).toBeNull()
  })

  it('refuses a filter nested deeper than the server allows', () => {
    let deep = group('AND', rule('Payload.A', 'equal', 1))

    for (let level = 0; level < 8; level++) deep = group('AND', deep)

    expect(parseFilter(JSON.stringify(deep))).toBeNull()
  })

  it('describes a filter in words', () => {
    const text = describeFilter(
      group(
        'AND',
        rule('Payload.Amount', 'greater', 1),
        group('OR', rule('Payload.Name', 'like', 'A%')),
      ),
      (operator) => operator,
      { and: 'AND', or: 'OR' },
    )

    expect(text).toBe('Payload.Amount greater "1" AND (Payload.Name like "A%")')
  })
})
