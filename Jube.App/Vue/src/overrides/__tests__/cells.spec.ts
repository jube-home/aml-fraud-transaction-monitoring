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

import {describe, expect, it, vi} from 'vitest'
import {approvalBadgeCell, expiryCell, switchCell} from '../cells'
import {forceTargets, isGoverned} from '../forceApproval'

describe('switchCell', () => {
  it('is enabled by default and disabled for a Force override whose removal is pending', () => {
    const change = vi.fn(() => Promise.resolve(true))

    const enabled = switchCell(true, 'Override: x', change) as HTMLInputElement
    const pending = switchCell(true, 'Override: x', change, true) as HTMLInputElement

    expect(enabled.disabled).toBe(false)
    expect(pending.disabled).toBe(true)
    expect(pending.checked).toBe(true)

    pending.dispatchEvent(new Event('change'))
    expect(change).not.toHaveBeenCalledWith(false)
  })
})

describe('expiryCell', () => {
  it('can be disabled while a removal awaits approval', () => {
    const cell = expiryCell(null, true, 'Expiry: x', vi.fn(), () => Promise.resolve(true))

    expect((cell as HTMLInputElement).disabled).toBe(true)
  })
})

describe('approvalBadgeCell', () => {
  it('draws nothing without a state and a toned badge with one', () => {
    expect(approvalBadgeCell(undefined, 'neutral', 'note').textContent).toBe('')
    expect(approvalBadgeCell(undefined, 'neutral', 'note').className).toBe('')

    const badge = approvalBadgeCell('Deletion awaiting approval', 'warning', 'note')

    expect(badge.textContent).toBe('Deletion awaiting approval')
    expect(badge.className).toContain('badge-warning')
  })
})

describe('a Force override whose removal is pending', () => {
  const row = {overrideKind: 1 as const, overrideId: 4, hasOverride: true, pendingRemoval: true}

  it('is still listed as present and still offered for review', () => {
    expect(isGoverned(row)).toBe(true)
    expect(
      forceTargets([row], {endpoint: '/x', approval: {kind: 22, historyRoute: '/h'}}),
    ).toEqual([{kind: 22, id: 4}])
  })
})
