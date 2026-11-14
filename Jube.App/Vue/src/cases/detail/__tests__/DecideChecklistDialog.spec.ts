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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {ApiError} from '../../../api/useApi'
import {
  installDialogStubs,
  item,
  mountWith,
  settle,
  view,
  type Harness,
} from '../../checklist/__tests__/support'
import {itemState, slaState} from '../../checklist/types'
import DecideChecklistDialog from '../DecideChecklistDialog.vue'
import type {CaseDetail} from '../types'

const get = vi.fn()
const check = vi.fn()
const uncheck = vi.fn()

vi.mock('../../checklist/api', async (original) => ({
  ...(await original<typeof import('../../checklist/api')>()),
  checklistApi: {
    get: (...args: unknown[]) => get(...args),
    check: (...args: unknown[]) => check(...args),
    uncheck: (...args: unknown[]) => uncheck(...args),
  },
}))

let harness: Harness | undefined

const detail = (patch: Partial<CaseDetail> = {}): CaseDetail =>
  ({
    id: 9041,
    locked: true,
    lockedUser: 'alex',
    closedStatusId: 0,
    ...patch,
  }) as CaseDetail

const items = [
  item({id: 1, name: 'Review the transactions', priority: 1}),
  item({
    id: 2,
    name: 'Record an investigation note',
    priority: 2,
    slaStateId: slaState.onTrack,
    minutesRemaining: 125,
    dueDate: '2026-11-04T11:05:00Z',
  }),
  item({
    id: 3,
    name: 'Contact the customer',
    priority: 3,
    slaStateId: slaState.breached,
    minutesRemaining: -70,
  }),
  item({
    id: 4,
    name: 'Check related cases',
    priority: 4,
    stateId: itemState.done,
    completedUser: 'alex',
    completedDate: '2026-11-04T09:30:00Z',
    slaStateId: slaState.completedLate,
  }),
]

async function mount(patch: Partial<CaseDetail> = {}, userName = 'alex'): Promise<Harness> {
  return mountWith(DecideChecklistDialog, {open: true, detail: detail(patch), userName}, [
    'close',
  ])
}

beforeEach(() => {
  installDialogStubs()
  get.mockReset().mockResolvedValue(view(items))
  check.mockReset()
  uncheck.mockReset()
})

afterEach(() => harness?.unmount())

function box(id: number): HTMLInputElement {
  return harness!.byTestId(`checklist-tick-${id}`) as HTMLInputElement
}

function click(id: number): void {
  const element = box(id)

  element.checked = !element.checked
  element.dispatchEvent(new Event('change', {bubbles: true}))
}

describe('DecideChecklistDialog items', () => {
  it('lists the items in the order the server sent, with done ones ticked', async () => {
    harness = await mount()

    const names = [...harness.root.querySelectorAll('[data-testid^="checklist-item-"]')].map(
      (node) => node.querySelector('label')?.textContent?.trim(),
    )

    expect(names).toEqual([
      'Review the transactions',
      'Record an investigation note',
      'Contact the customer',
      'Check related cases',
    ])
    expect(box(4).checked).toBe(true)
    expect(box(1).checked).toBe(false)
    expect(harness.root.textContent).toContain('1 of 4')
  })

  it('shows the SLA position of each item', async () => {
    harness = await mount()

    expect(harness.byTestId('checklist-sla-1')).toBeNull()
    expect(harness.byTestId('checklist-sla-2')!.textContent).toContain('Due in 2 h 5 min')
    expect(harness.byTestId('checklist-sla-3')!.textContent).toContain('Overdue by 1 h 10 min')
    expect(harness.byTestId('checklist-sla-3')!.getAttribute('data-sla')).toBe('error')
    expect(harness.byTestId('checklist-sla-4')!.textContent).toContain('Done late')
  })

  it('names who ticked an item', async () => {
    harness = await mount()

    expect(harness.byTestId('checklist-by-4')!.textContent).toContain('Done by alex')
  })

  it('lets the holder tick an item and shows the result the server returns', async () => {
    check.mockResolvedValue(
      view(
        items.map((current) =>
          current.id === 1 ? {...current, stateId: itemState.done} : current,
        ),
      ),
    )
    harness = await mount()

    click(1)
    await settle()

    expect(check).toHaveBeenCalledWith(9041, 1)
    expect(box(1).checked).toBe(true)
    expect(harness.root.textContent).toContain('2 of 4')
  })

  it('lets the holder untick a ticked item', async () => {
    uncheck.mockResolvedValue(
      view(items.map((current) => ({...current, stateId: itemState.shown}))),
    )
    harness = await mount()

    click(4)
    await settle()

    expect(uncheck).toHaveBeenCalledWith(9041, 4)
    expect(box(4).checked).toBe(false)
  })

  it('shows the validation message under an item whose tick is refused and leaves it unticked', async () => {
    check.mockRejectedValue(
      new ApiError(400, {
        errors: [{propertyName: 'Item', errorMessage: 'Add at least one note first.'}],
      }),
    )
    harness = await mount()

    click(2)
    await settle()

    expect(harness.byTestId('checklist-refusal-2')!.textContent).toContain(
      'Add at least one note first.',
    )
    expect(box(2).checked).toBe(false)
  })

  it('clears the refusal when the item is tried again', async () => {
    check.mockRejectedValueOnce(
      new ApiError(400, {errors: [{propertyName: 'Item', errorMessage: 'Not yet.'}]}),
    )
    check.mockResolvedValueOnce(view(items))
    harness = await mount()

    click(2)
    await settle()
    expect(harness.byTestId('checklist-refusal-2')).not.toBeNull()

    click(2)
    await settle()

    expect(harness.byTestId('checklist-refusal-2')).toBeNull()
  })
})

describe('DecideChecklistDialog read-only', () => {
  it('cannot be changed by someone who does not hold the case', async () => {
    harness = await mount({lockedUser: 'sam'})

    expect(box(1).disabled).toBe(true)
    expect(harness.byTestId('checklist-readonly')!.textContent).toContain('Held by sam')
  })

  it('cannot be changed on a free case, and says to take it', async () => {
    harness = await mount({locked: false, lockedUser: null})

    expect(box(1).disabled).toBe(true)
    expect(harness.byTestId('checklist-readonly')!.textContent).toContain('Take the case')
  })

  it('cannot be changed on a closed case', async () => {
    harness = await mount({closedStatusId: 3})

    expect(box(1).disabled).toBe(true)
    expect(harness.byTestId('checklist-readonly')!.textContent).toContain('closed')
  })

  it('has no reason shown and live boxes for the holder', async () => {
    harness = await mount()

    expect(box(1).disabled).toBe(false)
    expect(harness.byTestId('checklist-readonly')).toBeNull()
  })

  it('says so when nothing is showing', async () => {
    get.mockResolvedValue(view([]))
    harness = await mount()

    expect(harness.byTestId('checklist-empty')).not.toBeNull()
  })
})
