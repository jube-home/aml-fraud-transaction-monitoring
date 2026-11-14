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
import ChecklistTile from '../ChecklistTile.vue'
import {mountWith, type Harness} from './support'

let harness: Harness | undefined

afterEach(() => harness?.unmount())

describe('ChecklistTile', () => {
  it('is absent when the case has no checklist items', async () => {
    harness = await mountWith(ChecklistTile, {done: 0, required: 0, breached: 0})

    expect(harness.byTestId('case-decide-tab-checklist')).toBeNull()
  })

  it('says how many are done and stays neutral on track', async () => {
    harness = await mountWith(ChecklistTile, {done: 3, required: 5, breached: 0})

    const tile = harness.byTestId('case-decide-tab-checklist')!

    expect(tile.textContent).toContain('3 of 5 done')
    expect(tile.className).not.toContain('text-error')
  })

  it('turns red and says how many are overdue when an SLA is breached', async () => {
    harness = await mountWith(ChecklistTile, {done: 3, required: 5, breached: 2})

    const tile = harness.byTestId('case-decide-tab-checklist')!

    expect(tile.textContent).toContain('3 of 5 done, 2 overdue')
    expect(tile.className).toContain('text-error')
  })

  it('asks to open the checklist when selected', async () => {
    harness = await mountWith(ChecklistTile, {done: 1, required: 2, breached: 0}, ['select'])

    harness.byTestId('case-decide-tab-checklist')!.click()

    expect(harness.mounted.emitted.select).toHaveLength(1)
  })
})
