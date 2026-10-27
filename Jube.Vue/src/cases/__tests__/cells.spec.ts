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
import { caseCell, dueCell, lockCell, riskCell, riskText, valueText, whyCell } from '../cells'
import type { CellContext } from '../cells'
import { row } from './fixtures'

const context: CellContext = {
  t: (key, params) => (params ? `${key}|${Object.values(params).join('|')}` : key),
  percent: (value) => `${Math.round(value * 100)}%`,
  number: (value) => String(value),
  nowMs: Date.parse('2026-10-23T12:00:00Z'),
}

describe('case cells', () => {
  it('shows the lane as a word, tinted only for the urgent lanes, with the risk beneath', () => {
    const standard = riskCell(row(), context)
    const urgent = riskCell(row({ risk: { ...row().risk, lane: 1 } }), context)

    expect(standard.textContent).toContain('cases.lane.standard')
    expect(standard.querySelector('.badge-error')).toBeNull()
    expect(urgent.querySelector('.badge-error')?.textContent).toBe('cases.lane.liveMoney')
    expect(standard.textContent).toContain('cases.likelihood.likely|56%|cases.impact.high|62')
    expect(standard.querySelector('[data-band="3"]')).not.toBeNull()
  })

  it('says a case has not been assessed instead of showing zeros', () => {
    const unassessed = row({ risk: { ...row().risk, assessed: false, priority: 0, likelihood: 0 } })

    expect(riskText(unassessed, context)).toBe('cases.risk.notAssessed')
    expect(riskCell(unassessed, context).textContent).not.toContain('0%')
  })

  it('names the case, its key and its age', () => {
    const text = caseCell(row(), context).textContent

    expect(text).toContain('C-9041')
    expect(text).toContain('AccountId · ACC-441')
    expect(text).toContain('cases.age|cases.time.days|')
  })

  it('leads with the top driver and flags a returned case with the reason', () => {
    const quiet = whyCell(row(), context)
    const returned = whyCell(
      row({
        attention: {
          needed: true,
          reasons: [{ code: 1, text: 'Reopened since you last looked' }],
          lastViewedDate: null,
          changesSince: 0,
          priorityRise: 0,
        },
      }),
      context,
    )

    expect(quiet.textContent).toBe('Structuring 9d')
    expect(returned.textContent).toContain('Structuring 9d')
    expect(returned.textContent).toContain('cases.attention.badge')
    expect(returned.textContent).toContain('Reopened since you last looked')
    expect(returned.querySelector('.badge-info')?.getAttribute('title')).toContain('Reopened')
  })

  it('falls back to the lane reason and then to saying there is nothing', () => {
    const noDriver = row({ risk: { ...row().risk, topDriver: null } })
    const nothing = row({ risk: { ...row().risk, topDriver: null, laneReason: null } })

    expect(whyCell(noDriver, context).textContent).toBe('No deadline applies.')
    expect(whyCell(nothing, context).textContent).toBe('cases.why.none')
  })

  it('formats the value or says there is none', () => {
    expect(valueText(row(), context)).toBe('45000')
    expect(valueText(row({ value: null }), context)).toBe('cases.value.none')
  })

  it('shows the worst service level with its colour and a countdown, or the next deadline', () => {
    const breached = row({
      serviceLevel: {
        state: 4,
        stateName: 'Breached',
        worst: {
          dimension: 1,
          dimensionName: 'First view',
          state: 4,
          stateName: 'Breached',
          dueDate: '2026-10-23T11:00:00Z',
          elapsedFraction: 2,
          actualMinutes: null,
          targetMinutes: 30,
        },
        measures: [],
      },
    })

    const cell = dueCell(breached, context)

    expect(cell.querySelector('.badge-error')?.textContent).toBe('cases.sla.breached')
    expect(cell.textContent).toContain('cases.sla.firstView')
    expect(cell.textContent).toContain('cases.time.overdue')

    const deadline = dueCell(
      row({ risk: { ...row().risk, nextDeadline: '2026-10-23T14:00:00Z' } }),
      context,
    )

    expect(deadline.textContent).toContain('cases.time.in')
    expect(dueCell(row(), context).textContent).toBe('cases.sla.noneDue')
  })

  it('does not shout about a case that is on track', () => {
    const onTrack = row({
      serviceLevel: {
        state: 1,
        stateName: 'Met',
        worst: null,
        measures: [],
      },
    })

    expect(dueCell(onTrack, context).querySelector('.badge-error, .badge-warning')).toBeNull()
  })

  it('says who holds a case, and whether it is free or yours', () => {
    expect(lockCell(row(), context).textContent).toContain('cases.lock.free')
    expect(
      lockCell(row({ locked: true, mine: true, lockedUser: 'me' }), context).textContent,
    ).toContain('cases.lock.you')
    expect(lockCell(row({ locked: true, lockedUser: 'sam' }), context).textContent).toContain(
      'cases.lock.heldBy|sam',
    )
  })

  it('flags a case picked for quality review and says why it is there', () => {
    const sampled = whyCell(row({ qaSample: true }), context)
    const plain = whyCell(row(), context)

    expect(sampled.querySelector('.badge')?.textContent).toBe('cases.qa.badge')
    expect(sampled.textContent).toContain('cases.qa.hint')
    expect(plain.textContent).not.toContain('cases.qa')
  })

  it('names a regulatory deadline or payment hold beside its countdown and leaves a review diary plain', () => {
    const due = '2026-10-25T12:00:00Z'
    const risk = { ...row().risk, nextDeadline: due }
    const regulatory = dueCell(row({ diary: true, diaryTypeId: 3, risk }), context)
    const hold = dueCell(row({ diary: true, diaryTypeId: 4, risk }), context)
    const customer = dueCell(row({ diary: true, diaryTypeId: 2, risk }), context)
    const review = dueCell(row({ diary: true, diaryTypeId: 1, risk }), context)
    const off = dueCell(row({ diary: false, diaryTypeId: 3, risk }), context)

    expect(regulatory.querySelector('.badge-error')?.textContent).toBe(
      'cases.diary.type.regulatoryDeadline',
    )
    expect(hold.querySelector('.badge-error')?.textContent).toBe('cases.diary.type.paymentHold')
    expect(customer.querySelector('.badge')?.textContent).toBe('cases.diary.type.customerResponse')
    expect(customer.querySelector('.badge-error')).toBeNull()
    expect(review.querySelector('.badge')).toBeNull()
    expect(off.querySelector('.badge')).toBeNull()
    expect(regulatory.textContent).toContain('cases.time.in')
  })
})
