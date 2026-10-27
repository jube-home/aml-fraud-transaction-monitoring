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
  defaultJournalState,
  mergeJournalState,
  parseJournalState,
  serialiseJournalState,
  windowFrom,
  type JournalState,
} from '../state'

describe('journal state', () => {
  it('has the defaults for an empty address and leaves them out when writing', () => {
    expect(parseJournalState({})).toEqual(defaultJournalState)
    expect(serialiseJournalState(defaultJournalState)).toEqual({})
  })

  it('round trips every part of the state through the address', () => {
    const state: JournalState = {
      lens: 'features',
      days: 365,
      pivot: { key: 'Device', value: 'D-9' },
      transaction: 4412,
      activationsOnly: true,
      merge: [
        { key: 'Device', value: 'D-9' },
        { key: 'Ip', value: '2001:db8::1' },
      ],
      pinnedOnly: true,
      filter: {
        condition: 'OR',
        rules: [{ id: 'Amount', operator: 'greater', value: 10 }],
      },
      speed: 300,
      distance: 0,
      improbable: true,
      all: true,
      trails: false,
      heat: true,
    }

    expect(parseJournalState(serialiseJournalState(state))).toEqual(state)
    expect(parseJournalState({ jf: 'not a filter' }).filter).toBeNull()
  })

  it('keeps at most three merged lanes and drops lanes that are malformed', () => {
    const lanes = [
      ['A', '1'],
      ['B', '2'],
      ['C', '3'],
      ['D', '4'],
    ]

    expect(parseJournalState({ jm: JSON.stringify(lanes) }).merge).toHaveLength(3)
    expect(
      parseJournalState({ jm: JSON.stringify([['A', ''], ['B'], [1, 2], 'x']) }).merge,
    ).toEqual([])
    expect(parseJournalState({ jm: '{not json' }).merge).toEqual([])
    expect(parseJournalState({ jm: '{"a":1}' }).merge).toEqual([])
  })

  it('ignores a lens, window or transaction it does not know', () => {
    const state = parseJournalState({ jl: 'everything', jw: '8', jt: '-4', jk: 'Device' })

    expect(state.lens).toBe('payload')
    expect(state.days).toBe(90)
    expect(state.transaction).toBeNull()
    expect(state.pivot).toBeNull()
  })

  it('writes the map options only when they differ from the defaults', () => {
    expect(serialiseJournalState({ ...defaultJournalState, speed: 300 })).toEqual({ js: '300' })
    expect(serialiseJournalState({ ...defaultJournalState, distance: 0 })).toEqual({ jd: '0' })
    expect(serialiseJournalState({ ...defaultJournalState, improbable: true })).toEqual({ ji: '1' })
    expect(serialiseJournalState({ ...defaultJournalState, all: true })).toEqual({ jb: '1' })
    expect(serialiseJournalState({ ...defaultJournalState, trails: false })).toEqual({ jn: '0' })
    expect(serialiseJournalState({ ...defaultJournalState, heat: true })).toEqual({ jh: '1' })
    expect(parseJournalState({ js: '0', jd: '99999', ji: 'yes', jb: 'true' })).toEqual(
      defaultJournalState,
    )
  })

  it('keeps every other address key and rewrites only its own', () => {
    const merged = mergeJournalState(
      { section: 'journal', wf: 'x', jl: 'rules', jt: '9' },
      { ...defaultJournalState, lens: 'models' },
    )

    expect(merged).toEqual({ section: 'journal', wf: 'x', jl: 'models' })
  })

  it('starts a window at midnight UTC the given number of days back', () => {
    expect(windowFrom(30, new Date('2026-10-25T15:30:00Z'))).toBe('2026-09-25T00:00:00.000Z')
  })

  it('reads the first of repeated address values', () => {
    expect(parseJournalState({ jl: ['rules', 'models'] }).lens).toBe('rules')
  })
})
