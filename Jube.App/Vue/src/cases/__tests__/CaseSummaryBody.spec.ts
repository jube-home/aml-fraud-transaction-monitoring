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
import {mountWith, type Harness} from '../checklist/__tests__/support'
import CaseSummaryBody from '../CaseSummaryBody.vue'
import type {CaseChecklistSummary, CaseSummary} from '../types'
import {row} from './fixtures'

let harness: Harness | undefined

afterEach(() => harness?.unmount())

function summary(checklist: Partial<CaseChecklistSummary> = {}): CaseSummary {
  return {
    case: row(),
    checklist: {
      required: 5,
      done: 3,
      open: 2,
      breached: 0,
      nextDueDate: null,
      complete: false,
      ...checklist,
    },
    contributions: [],
    baseRate: null,
    impact: null,
    riskHistory: [],
    stays: [],
    recentEvents: [],
    notes: [],
    keyHistory: {closed: 0, confirmed: 0, newestClosedDate: null, otherOpenCases: 0},
    since: {
      firstLook: true,
      lastViewedDate: null,
      priorityThen: null,
      priorityNow: 62,
      changes: [],
    },
  }
}

describe('CaseSummaryBody checklist line', () => {
  it('says how many checklist items are done, quietly, when none is overdue', async () => {
    harness = await mountWith(CaseSummaryBody, {caseId: 9041, data: summary(), part: 'brief'})

    const line = harness.byTestId('cases-peek-checklist')!

    expect(line.textContent).toContain('Checklist')
    expect(line.textContent).toContain('3 of 5 done')
    expect(line.textContent).not.toContain('overdue')
    expect(line.className).not.toContain('text-error')
  })

  it('turns red and counts the overdue items when an SLA is breached', async () => {
    harness = await mountWith(CaseSummaryBody, {
      caseId: 9041,
      data: summary({breached: 1}),
      part: 'brief',
    })

    const line = harness.byTestId('cases-peek-checklist')!

    expect(line.textContent).toContain('3 of 5 done, 1 overdue')
    expect(line.className).toContain('text-error')
  })

  it('says nothing when the case has no checklist items showing', async () => {
    harness = await mountWith(CaseSummaryBody, {
      caseId: 9041,
      data: summary({required: 0, done: 0, open: 0, complete: true}),
      part: 'brief',
    })

    expect(harness.byTestId('cases-peek-checklist')).toBeNull()
  })
})
