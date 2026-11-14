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
import {remainingChoices, sameTags, tagNames, withTag, withoutTag} from '../caseTags'
import type {CaseTag} from '../types'

const tag = (id: number, name: string | null): CaseTag => ({
  id,
  caseId: 9041,
  name,
  createdUser: 'Administrator',
  createdDate: '2026-11-01T09:00:00Z',
})

describe('case tags', () => {
  it('reads the distinct, non-empty names in order', () => {
    expect(
      tagNames([tag(1, 'Mule'), tag(2, ''), tag(3, null), tag(4, 'Mule'), tag(5, 'Victim')]),
    ).toEqual(['Mule', 'Victim'])
  })

  it('offers only the vocabulary names not yet on the case', () => {
    expect(remainingChoices(['Mule', 'Victim', '', 'Reviewed'], ['Victim'])).toEqual([
      'Mule',
      'Reviewed',
    ])
  })

  it('adds a name once and removes it without touching the others', () => {
    expect(withTag(['Mule'], 'Victim')).toEqual(['Mule', 'Victim'])
    expect(withTag(['Mule'], 'Mule')).toEqual(['Mule'])
    expect(withTag(['Mule'], '')).toEqual(['Mule'])
    expect(withoutTag(['Mule', 'Victim'], 'Mule')).toEqual(['Victim'])
  })

  it('compares tag sets regardless of order', () => {
    expect(sameTags(['Mule', 'Victim'], ['Victim', 'Mule'])).toBe(true)
    expect(sameTags(['Mule'], ['Mule', 'Victim'])).toBe(false)
    expect(sameTags([], [])).toBe(true)
  })
})
