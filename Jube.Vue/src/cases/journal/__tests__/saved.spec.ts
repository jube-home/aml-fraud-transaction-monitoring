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
import { defaultJournalState } from '../state'
import type { SavedView } from '../types'
import { mergeText, viewPatch } from '../saved'

const saved = (extra: Partial<SavedView>): SavedView => ({
  guid: 'g',
  caseId: 1,
  name: 'Two countries',
  days: 30,
  merge: null,
  filter: null,
  limitKmh: null,
  minimumKilometres: null,
  impossibleOnly: false,
  showAll: false,
  lens: null,
  createdUser: 'me',
  createdDate: '2026-10-26T10:00:00Z',
  ...extra,
})

describe('saved views', () => {
  it('writes the keys in the form the server stores and nothing when there are none', () => {
    expect(mergeText([])).toBeUndefined()
    expect(mergeText([{ key: 'Device', value: 'D-9' }])).toBe('[{"key":"Device","value":"D-9"}]')
  })

  it('recreates the scope, the view and the map options from the settings', () => {
    const group = {
      condition: 'AND',
      rules: [{ id: 'Payload.Channel', operator: 'equal', value: 'CP' }],
    }

    expect(
      viewPatch(
        saved({
          days: 90,
          merge: '[{"key":"Device","value":"D-9"}]',
          filter: JSON.stringify(group),
          limitKmh: 300,
          minimumKilometres: 0,
          impossibleOnly: true,
          showAll: true,
          lens: 'rules',
        }),
      ),
    ).toEqual({
      days: 90,
      merge: [{ key: 'Device', value: 'D-9' }],
      filter: group,
      speed: 300,
      distance: 0,
      improbable: true,
      all: true,
      lens: 'rules',
      transaction: null,
      pinnedOnly: false,
    })
  })

  it('falls back to the defaults for settings it cannot read', () => {
    const expected = {
      days: defaultJournalState.days,
      merge: [],
      filter: null,
      speed: 900,
      distance: 50,
      improbable: false,
      all: false,
      lens: 'payload',
      transaction: null,
      pinnedOnly: false,
    }

    expect(viewPatch(saved({ days: 5, merge: 'nonsense', filter: 'nonsense', lens: 'x' }))).toEqual(
      expected,
    )
    expect(
      viewPatch(
        saved({
          merge: '[{"key":"","value":"x"},5]',
          filter: '{}',
          limitKmh: 10001,
          minimumKilometres: 20001,
        }),
      ),
    ).toEqual({ ...expected, days: 30 })
  })
})
