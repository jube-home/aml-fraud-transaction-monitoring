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
import { prepareInitialRules } from '../initial'

const known = new Set(['Id', 'Payload.Name'])

describe('prepareInitialRules', () => {
  it('has nothing to restore when nothing was stored', () => {
    expect(prepareInitialRules(undefined, known)).toEqual({
      group: undefined,
      dropped: [],
      failed: false,
    })
    expect(prepareInitialRules('', known).group).toBeUndefined()
  })

  it('reads stored JSON text or an object and keeps the rules that still exist', () => {
    const stored = { condition: 'AND', rules: [{ id: 'Id', operator: 'equal', value: 1 }] }

    expect(prepareInitialRules(JSON.stringify(stored), known).group).toEqual(stored)
    expect(prepareInitialRules(stored as never, known).group).toEqual(stored)
  })

  it('drops rules for fields that no longer exist, at any depth, and says which', () => {
    const result = prepareInitialRules(
      {
        condition: 'AND',
        rules: [
          { id: 'Gone', operator: 'equal', value: 1 },
          {
            condition: 'OR',
            rules: [
              { id: 'Payload.Name', operator: 'equal', value: 'x' },
              { id: 'AlsoGone', operator: 'equal', value: 2 },
            ],
          },
        ],
      } as never,
      known,
    )

    expect(result.dropped).toEqual(['Gone', 'AlsoGone'])
    expect(JSON.stringify(result.group)).not.toContain('Gone')
    expect(result.failed).toBe(false)
  })

  it('reports malformed JSON and shapes that are not rule groups as a failed restore', () => {
    expect(prepareInitialRules('{not json', known).failed).toBe(true)
    expect(prepareInitialRules('{"nothing":1}', known).failed).toBe(true)
    expect(prepareInitialRules('null', known).failed).toBe(true)
  })
})
