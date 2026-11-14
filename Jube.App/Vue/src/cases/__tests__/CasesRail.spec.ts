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
import CasesRail from '../CasesRail.vue'
import type {CaseCount} from '../types'

let harness: Harness | undefined

afterEach(() => harness?.unmount())

const counts: CaseCount[] = [
  {kind: 'View', key: '6', name: 'All', count: 120, mine: false, failed: false},
  {kind: 'View', key: '8', name: 'x', count: 4, mine: false, failed: false},
  {kind: 'View', key: '9', name: 'y', count: 11, mine: false, failed: false},
]

describe('CasesRail checklist views', () => {
  it('lists the two checklist views with their words and counts', async () => {
    harness = await mountWith(CasesRail, {counts, source: {kind: 'view', view: 6}}, ['source'])

    const filters = harness.byTestId('cases-filters')!
    const buttons = [...filters.querySelectorAll('button')].map(
      (button) => button.textContent ?? '',
    )

    expect(buttons.some((text) => text.includes('Checklist overdue') && text.includes('4'))).toBe(
      true,
    )
    expect(
      buttons.some((text) => text.includes('Checklist items open') && text.includes('11')),
    ).toBe(true)
  })

  it.each([
    ['Checklist overdue', 8],
    ['Checklist items open', 9],
  ])('picks view %s as numeric view %i', async (name, view) => {
    harness = await mountWith(CasesRail, {counts, source: {kind: 'view', view: 6}}, ['source'])

    const button = [...harness.byTestId('cases-filters')!.querySelectorAll('button')].find(
      (candidate) => candidate.textContent?.includes(name),
    )!

    button.click()

    expect(harness.mounted.emitted.source?.[0]).toEqual([{kind: 'view', view}])
  })

  it('marks the chosen checklist view as the current one', async () => {
    harness = await mountWith(CasesRail, {counts, source: {kind: 'view', view: 8}}, ['source'])

    const current = harness.byTestId('cases-filters')!.querySelector('[aria-current="true"]')

    expect(current?.textContent).toContain('Checklist overdue')
  })
})
