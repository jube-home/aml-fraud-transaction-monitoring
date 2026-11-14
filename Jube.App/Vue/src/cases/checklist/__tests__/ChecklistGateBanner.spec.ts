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
import ChecklistGateBanner from '../ChecklistGateBanner.vue'
import type {ChecklistOverride} from '../types'
import {mountWith, type Harness} from './support'

let harness: Harness | undefined

afterEach(() => harness?.unmount())

const override: ChecklistOverride = {
  id: 5,
  caseId: 9041,
  reason: 'Verified by phone.',
  grantedUser: 'sam',
  grantedDate: '2026-11-04T10:00:00Z',
  openItems: [],
}

function props(patch: Record<string, unknown> = {}) {
  return {
    names: ['Contact the customer', 'Check related cases'],
    override: null,
    canOverride: false,
    testid: 'gate',
    ...patch,
  }
}

describe('ChecklistGateBanner', () => {
  it('says how many items are still open and names them', async () => {
    harness = await mountWith(ChecklistGateBanner, props())

    expect(harness.byTestId('gate-count')!.textContent).toContain('2 checklist items still open')
    expect(harness.byTestId('gate-names')!.textContent).toContain('Contact the customer')
    expect(harness.byTestId('gate-names')!.textContent).toContain('Check related cases')
  })

  it('uses the singular for one item', async () => {
    harness = await mountWith(ChecklistGateBanner, props({names: ['Only one']}))

    expect(harness.byTestId('gate-count')!.textContent).toContain('1 checklist item still open')
  })

  it('names at most five and counts the rest', async () => {
    const names = ['a', 'b', 'c', 'd', 'e', 'f', 'g']

    harness = await mountWith(ChecklistGateBanner, props({names}))

    expect(harness.byTestId('gate-names')!.querySelectorAll('li')).toHaveLength(6)
    expect(harness.byTestId('gate-names')!.textContent).toContain('+2 more')
  })

  it('offers to open the checklist', async () => {
    harness = await mountWith(ChecklistGateBanner, props(), ['openChecklist'])

    harness.byTestId('gate-open')!.click()

    expect(harness.mounted.emitted.openChecklist).toHaveLength(1)
  })

  it('offers the override only when allowed', async () => {
    harness = await mountWith(ChecklistGateBanner, props({canOverride: false}))
    expect(harness.byTestId('gate-override-action')).toBeNull()
    harness.unmount()

    harness = await mountWith(ChecklistGateBanner, props({canOverride: true}), ['override'])
    harness.byTestId('gate-override-action')!.click()

    expect(harness.mounted.emitted.override).toHaveLength(1)
  })

  it('says an override is in place instead of listing the items', async () => {
    harness = await mountWith(ChecklistGateBanner, props({override, canOverride: true}))

    expect(harness.byTestId('gate-override')!.textContent).toContain('sam')
    expect(harness.byTestId('gate-override')!.textContent).toContain('Verified by phone.')
    expect(harness.byTestId('gate')).toBeNull()
    expect(harness.byTestId('gate-override-action')).toBeNull()
  })

  it('shows nothing when there is nothing open', async () => {
    harness = await mountWith(ChecklistGateBanner, props({names: []}))

    expect(harness.byTestId('gate')).toBeNull()
  })
})
