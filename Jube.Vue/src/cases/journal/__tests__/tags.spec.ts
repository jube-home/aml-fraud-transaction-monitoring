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
import { guidOf, keepSelected, presentTags, tagsOf, toggled } from '../tags'
import type { JournalRow } from '../types'

function row(id: number, guid: string, tag?: unknown): JournalRow {
  return { Id: id, EntityAnalysisModelInstanceEntryGuid: guid, Tag: tag }
}

describe('journal tags', () => {
  it('reads the guid and the tags of a row', () => {
    expect(guidOf(row(1, 'a'))).toBe('a')
    expect(guidOf({ Id: 1 })).toBe('')
    expect(tagsOf(row(1, 'a', ['x', 2]))).toEqual(['x', '2'])
    expect(tagsOf(row(1, 'a', 'x'))).toEqual([])
    expect(tagsOf(row(1, 'a'))).toEqual([])
  })

  it('counts each tag once per selected row, alphabetically, ignoring unselected rows', () => {
    const rows = [
      row(1, 'a', ['beta', 'alpha', 'beta']),
      row(2, 'b', ['alpha']),
      row(3, 'c', ['gamma']),
    ]

    expect(presentTags(rows, new Set(['a', 'b']))).toEqual([
      { name: 'alpha', rows: 2 },
      { name: 'beta', rows: 1 },
    ])
    expect(presentTags(rows, new Set())).toEqual([])
  })

  it('keeps only selections that are still in the rows', () => {
    const kept = keepSelected(new Set(['a', 'gone']), [row(1, 'a'), row(2, 'b')])

    expect([...kept]).toEqual(['a'])
  })

  it('toggles without changing the set it was given', () => {
    const start = new Set(['a'])

    expect([...toggled(start, 'b', true)]).toEqual(['a', 'b'])
    expect([...toggled(start, 'a', false)]).toEqual([])
    expect([...start]).toEqual(['a'])
  })
})
