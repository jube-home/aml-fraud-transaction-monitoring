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
import {
  emptySource,
  isBlank,
  isValidAddress,
  maximumSources,
  sourcesValid,
  toCitations,
  today,
} from '../researchSources'

describe('researchSources', () => {
  it('starts a source dated today', () => {
    expect(emptySource('2026-11-03')).toMatchObject({title: '', url: '', retrieved: '2026-11-03'})
    expect(today(new Date('2026-11-03T23:30:00Z'))).toBe('2026-11-03')
    expect(emptySource().key).not.toBe(emptySource().key)
  })

  it('accepts only http and https addresses', () => {
    expect(isValidAddress('https://registry.example.org/acme')).toBe(true)
    expect(isValidAddress('http://example.com')).toBe(true)
    expect(isValidAddress('javascript:alert(1)')).toBe(false)
    expect(isValidAddress('data:text/html;base64,AAAA')).toBe(false)
    expect(isValidAddress('file:///etc/passwd')).toBe(false)
    expect(isValidAddress('ftp://example.org/a')).toBe(false)
    expect(isValidAddress('https://user:pass@example.org/')).toBe(false)
    expect(isValidAddress('https://exa mple.org/')).toBe(false)
    expect(isValidAddress('https://example.org/<script>')).toBe(false)
    expect(isValidAddress('')).toBe(false)
    expect(isValidAddress(`https://example.org/${'a'.repeat(2000)}`)).toBe(false)
  })

  it('ignores a blank row and refuses a half-filled one', () => {
    const blank = emptySource()
    const half = {...emptySource(), title: 'Registry'}

    expect(isBlank(blank)).toBe(true)
    expect(sourcesValid([blank])).toBe(true)
    expect(sourcesValid([blank, half])).toBe(false)
    expect(sourcesValid([{...half, url: 'https://example.org/a'}])).toBe(true)
  })

  it('sends only the filled rows, trimmed, as dated citations', () => {
    const citations = toCitations([
      {key: 1, title: '  Registry ', url: ' https://example.org/a ', retrieved: '2026-11-01'},
      emptySource(),
      {key: 3, title: '', url: 'https://example.org/b', retrieved: '2026-11-02'},
    ])

    expect(citations).toEqual([
      {
        title: 'Registry',
        url: 'https://example.org/a',
        retrievedDate: '2026-11-01T00:00:00.000Z',
      },
      {title: null, url: 'https://example.org/b', retrievedDate: '2026-11-02T00:00:00.000Z'},
    ])
  })

  it('allows ten sources', () => {
    expect(maximumSources).toBe(10)
  })
})
