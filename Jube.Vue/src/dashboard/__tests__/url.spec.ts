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
import { listedEntries, namedValues, parametersFromQuery, sameValues } from '../url'
import type { DirectoryEntry, ParameterDefinition } from '../types'

const definitions: ParameterDefinition[] = [
  { id: 1, name: 'Region', dataTypeId: 1, defaultValue: 'North', required: true },
  { id: 2, name: null, dataTypeId: 2, defaultValue: '1', required: false },
]

describe('parametersFromQuery', () => {
  it('keeps strings and the first of repeated names, dropping the rest', () => {
    expect(
      parametersFromQuery({ Region: 'West', Since: ['a', 'b'], Empty: null, Gone: undefined }),
    ).toEqual({
      Region: 'West',
      Since: 'a',
    })
  })
})

describe('namedValues', () => {
  it('keys the values by parameter name and skips unnamed parameters', () => {
    expect(namedValues(definitions, { 1: 'South', 2: '5' })).toEqual({ Region: 'South' })
  })

  it('writes a blank for a parameter with no value', () => {
    expect(namedValues(definitions, {})).toEqual({ Region: '' })
  })
})

describe('sameValues', () => {
  it('compares by content', () => {
    expect(sameValues({ a: '1' }, { a: '1' })).toBe(true)
    expect(sameValues({ a: '1' }, { a: '2' })).toBe(false)
    expect(sameValues({ a: '1' }, { a: '1', b: '2' })).toBe(false)
  })
})

describe('listedEntries', () => {
  it('drops entries not shown in the directory and sorts by name', () => {
    const entries: DirectoryEntry[] = [
      { id: 1, guid: 'b', name: 'Beta', contextKindId: 1 },
      { id: 2, guid: 'c', name: 'Hidden', contextKindId: 2 },
      { id: 3, guid: 'a', name: 'Alpha', contextKindId: 1 },
    ]

    expect(listedEntries(entries).map((entry) => entry.guid)).toEqual(['a', 'b'])
  })
})
