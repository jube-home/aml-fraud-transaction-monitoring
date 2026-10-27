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
  draftProblems,
  emptyDraft,
  maxDescriptionLength,
  maxNameLength,
  newestFirst,
  payloadOf,
  prefixOf,
  type ApiKey,
} from '../model'

function key(overrides: Partial<ApiKey>): ApiKey {
  return {
    id: 1,
    userRegistryId: 7,
    name: 'Reporting',
    description: null,
    createdDate: '2026-10-20T09:00:00Z',
    apiKeyDisplay: 'abcdefgh',
    ...overrides,
  }
}

describe('draftProblems', () => {
  it('needs a name', () => {
    expect(draftProblems(emptyDraft())).toEqual(['nameRequired'])
    expect(draftProblems({ name: '   ', description: '' })).toEqual(['nameRequired'])
  })

  it('accepts a trimmed name within the limit and no description', () => {
    expect(draftProblems({ name: ' Reporting ', description: '' })).toEqual([])
  })

  it('refuses a name or description over the server limit', () => {
    expect(draftProblems({ name: 'a'.repeat(maxNameLength + 1), description: '' })).toEqual([
      'nameTooLong',
    ])
    expect(
      draftProblems({ name: 'ok', description: 'b'.repeat(maxDescriptionLength + 1) }),
    ).toEqual(['descriptionTooLong'])
    expect(draftProblems({ name: 'a'.repeat(maxNameLength), description: '' })).toEqual([])
  })

  it('reports both problems together', () => {
    expect(draftProblems({ name: '', description: 'b'.repeat(maxDescriptionLength + 1) })).toEqual([
      'nameRequired',
      'descriptionTooLong',
    ])
  })
})

describe('payloadOf', () => {
  it('trims and sends a blank description as null', () => {
    expect(payloadOf(7, { name: ' Reporting ', description: '  ' })).toEqual({
      userRegistryId: 7,
      name: 'Reporting',
      description: null,
    })
    expect(payloadOf(7, { name: 'x', description: ' nightly job ' }).description).toBe(
      'nightly job',
    )
  })
})

describe('prefixOf', () => {
  it('keeps the first eight characters and nothing of a missing key', () => {
    expect(prefixOf('abcdefghijklmnop')).toBe('abcdefgh')
    expect(prefixOf('abc')).toBe('abc')
    expect(prefixOf(null)).toBe('')
  })
})

describe('newestFirst', () => {
  it('orders by creation date, newest first, and by id when dates tie or are missing', () => {
    const rows = newestFirst([
      key({ id: 1, createdDate: '2026-10-01T00:00:00Z' }),
      key({ id: 3, createdDate: '2026-10-03T00:00:00Z' }),
      key({ id: 2, createdDate: '2026-10-03T00:00:00Z' }),
      key({ id: 5, createdDate: null }),
      key({ id: 4, createdDate: null }),
    ])

    expect(rows.map((row) => row.id)).toEqual([3, 2, 1, 5, 4])
  })

  it('does not mutate the input', () => {
    const input = [key({ id: 1 }), key({ id: 2 })]

    newestFirst(input)

    expect(input.map((row) => row.id)).toEqual([1, 2])
  })
})
