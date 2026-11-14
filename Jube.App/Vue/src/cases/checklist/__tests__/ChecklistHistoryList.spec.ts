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

import {afterEach, describe, expect, it} from 'vitest'
import ChecklistHistoryList from '../ChecklistHistoryList.vue'
import {historyActionKey} from '../historyText'
import {historyAction} from '../types'
import {mountWith, type Harness} from './support'

let harness: Harness | undefined

const entries = [
  {
    id: 12,
    actionId: historyAction.checked,
    itemName: 'Check related cases',
    actor: 'alex',
    createdDate: '2026-11-04T09:30:00Z',
    detail: null,
  },
  {
    id: 11,
    actionId: historyAction.gateBlocked,
    itemName: null,
    actor: null,
    createdDate: '2026-11-04T09:10:00Z',
    detail: 'The move was refused while 2 item(s) were still open.',
  },
]

describe('ChecklistHistoryList', () => {
  afterEach(() => {
    harness?.unmount()
    harness = undefined
  })

  it('titles the grid and says nothing is wrong when it has loaded', async () => {
    harness = await mountWith(ChecklistHistoryList, {entries, loading: false, failed: false})

    expect(harness.byTestId('checklist-history')!.textContent).toContain('Checklist history')
    expect(harness.byTestId('checklist-history-error')).toBeNull()
  })

  it('shows an alert when loading failed', async () => {
    harness = await mountWith(ChecklistHistoryList, {entries: [], loading: false, failed: true})

    expect(harness.byTestId('checklist-history-error')).not.toBeNull()
  })

  it('names every recorded action and falls back for an unknown one', () => {
    expect(historyActionKey(historyAction.checked)).toBe(
      'caseDetail.checklist.history.action.checked',
    )
    expect(historyActionKey(historyAction.slaBreached)).toBe(
      'caseDetail.checklist.history.action.slaBreached',
    )
    expect(historyActionKey(99)).toBe('caseDetail.checklist.history.action.unknown')
  })
})
