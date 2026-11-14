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
import {isOwnNote, narrativeChains, safeAddress, timelineNotes} from '../noteGroups'
import type {CaseNoteEntry} from '../types'

function note(over: Partial<CaseNoteEntry>): CaseNoteEntry {
  return {
    id: 1,
    caseId: 10,
    createdDate: '2026-11-02T10:00:00Z',
    createdUser: 'sam',
    note: 'text',
    action: null,
    priority: null,
    ...over,
  }
}

describe('noteGroups', () => {
  it('reads a note without the new fields as an ordinary analyst note', () => {
    expect(isOwnNote(note({}), 'sam')).toBe(true)
    expect(timelineNotes([note({})])).toHaveLength(1)
  })

  it('never lets anyone own a note written by AskJooby or the system', () => {
    expect(isOwnNote(note({sourceId: 3}), 'sam')).toBe(false)
    expect(isOwnNote(note({sourceId: 2}), 'sam')).toBe(false)
    expect(isOwnNote(note({createdUser: 'ann'}), 'sam')).toBe(false)
    expect(isOwnNote(note({}), undefined)).toBe(false)
  })

  it('shows the latest narrative of each case with the earlier versions behind it', () => {
    const chains = narrativeChains([
      note({id: 1, kindId: 2, sourceId: 3, version: 1}),
      note({id: 3, kindId: 2, sourceId: 3, version: 3}),
      note({id: 2, kindId: 2, sourceId: 3, version: 2}),
      note({
        id: 9,
        kindId: 2,
        sourceId: 3,
        version: 1,
        caseId: 11,
        createdDate: '2026-11-03T10:00:00Z',
      }),
    ])

    expect(chains.map((c) => c.latest.id)).toEqual([9, 3])
    expect(chains[1]!.earlier.map((n) => n.id)).toEqual([2, 1])
  })

  it('keeps narratives out of the timeline and everything else in it', () => {
    const timeline = timelineNotes([
      note({id: 1, kindId: 2}),
      note({id: 3, kindId: 4}),
      note({id: 4}),
    ])

    expect(timeline.map((n) => n.id)).toEqual([3, 4])
  })

  it('links only web addresses', () => {
    expect(safeAddress('https://example.org/a')).toBe('https://example.org/a')
    expect(safeAddress('javascript:alert(1)')).toBeNull()
    expect(safeAddress('data:text/html,x')).toBeNull()
    expect(safeAddress('not a url')).toBeNull()
  })
})
