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
import ChecklistOverrideDialog from '../ChecklistOverrideDialog.vue'
import {installDialogStubs, mountWith, settle, type Harness} from './support'

const override = vi.fn()

vi.mock('../api', async (original) => ({
  ...(await original<typeof import('../api')>()),
  checklistApi: {override: (...args: unknown[]) => override(...args)},
}))

let harness: Harness | undefined

beforeEach(() => {
  installDialogStubs()
  override.mockReset()
})

afterEach(() => harness?.unmount())

function type(text: string): void {
  const area = harness!.byTestId('checklist-override-reason') as HTMLTextAreaElement

  area.value = text
  area.dispatchEvent(new Event('input', {bubbles: true}))
}

describe('ChecklistOverrideDialog', () => {
  it('needs a reason before it can be submitted', async () => {
    harness = await mountWith(ChecklistOverrideDialog, {open: true, caseId: 9041}, ['close'])

    const submit = harness.byTestId('checklist-override-submit') as HTMLButtonElement

    expect(submit.disabled).toBe(true)

    type('   ')
    await settle()
    expect(submit.disabled).toBe(true)

    type('Verified by phone.')
    await settle()
    expect(submit.disabled).toBe(false)
  })

  it('sends the trimmed reason for the case and closes', async () => {
    override.mockResolvedValue({id: 1})
    harness = await mountWith(ChecklistOverrideDialog, {open: true, caseId: 9041}, ['close'])

    type('  Verified by phone.  ')
    await settle()
    harness.byTestId('checklist-override-submit')!.click()
    await settle()

    expect(override).toHaveBeenCalledWith(9041, 'Verified by phone.')
    expect(harness.mounted.emitted.close).toHaveLength(1)
  })

  it('shows the server refusal inline and stays open', async () => {
    override.mockRejectedValue(new ApiError(409, 'The holder of the case cannot override it.'))
    harness = await mountWith(ChecklistOverrideDialog, {open: true, caseId: 9041}, ['close'])

    type('Verified by phone.')
    await settle()
    harness.byTestId('checklist-override-submit')!.click()
    await settle()

    expect(harness.byTestId('checklist-override-error')!.textContent).toContain(
      'The holder of the case cannot override it.',
    )
    expect(harness.mounted.emitted.close).toBeUndefined()
  })
})
