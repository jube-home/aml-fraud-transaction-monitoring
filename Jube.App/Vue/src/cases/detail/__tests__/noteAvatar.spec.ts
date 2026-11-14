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
import {avatarOf, firstLetter} from '../noteAvatar'
import type {CaseNoteEntry} from '../types'

function note(over: Partial<CaseNoteEntry>): CaseNoteEntry {
  return {
    id: 1,
    caseId: 1,
    createdDate: '2026-11-02T10:00:00Z',
    createdUser: 'alex',
    note: 'x',
    action: null,
    priority: null,
    ...over,
  }
}

describe('noteAvatar', () => {
  it('takes the first character of the user name, upper-cased', () => {
    expect(firstLetter('alex')).toBe('A')
    expect(firstLetter('  zoe')).toBe('Z')
    expect(firstLetter('élodie')).toBe('É')
  })

  it('keeps a character outside the basic plane whole', () => {
    expect(firstLetter('\u{1F434}jooby')).toBe('\u{1F434}')
  })

  it('shows a question mark when there is no user name', () => {
    expect(firstLetter(null)).toBe('?')
    expect(firstLetter('   ')).toBe('?')
  })

  it('shows a letter for a user note and the full name for its label', () => {
    expect(avatarOf(note({}))).toEqual({kind: 'user', letter: 'A', name: 'alex'})
    expect(avatarOf(note({sourceId: 1, createdUser: 'sam'}))).toMatchObject({letter: 'S'})
  })

  it('shows the AskJooby mark for an AskJooby note, whoever the calling user was', () => {
    expect(avatarOf(note({sourceId: 3, createdUser: 'alex'}))).toEqual({
      kind: 'askJooby',
      letter: '',
      name: null,
    })
  })

  it('shows the system mark for a system note', () => {
    expect(avatarOf(note({sourceId: 2})).kind).toBe('system')
  })

  it('reads a note without a source as a user note', () => {
    expect(avatarOf(note({sourceId: undefined})).kind).toBe('user')
  })
})
