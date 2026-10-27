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
import { changeCount, lookupPath, planRoleChanges } from '../plan'

describe('planRoleChanges', () => {
  it('assigns roles that are wanted but not stored', () => {
    expect(planRoleChanges([], new Set(['a', 'b']))).toEqual({ assign: ['a', 'b'], unassign: [] })
  })

  it('unassigns by assignment id when a stored role is no longer wanted', () => {
    expect(planRoleChanges([{ id: 7, roleRegistryGuid: 'a' }], new Set())).toEqual({
      assign: [],
      unassign: [7],
    })
  })

  it('does nothing when nothing changed', () => {
    const changes = planRoleChanges([{ id: 7, roleRegistryGuid: 'a' }], new Set(['a']))

    expect(changeCount(changes)).toBe(0)
  })

  it('removes duplicate assignments of one role', () => {
    expect(
      planRoleChanges(
        [
          { id: 7, roleRegistryGuid: 'a' },
          { id: 8, roleRegistryGuid: 'a' },
        ],
        new Set(['a']),
      ),
    ).toEqual({ assign: [], unassign: [8] })
  })
})

describe('lookupPath', () => {
  it('capitalises the parent key to name the By endpoint', () => {
    expect(lookupPath('/api/CaseWorkflowStatusRole', 'caseWorkflowStatusGuid', 'x')).toBe(
      '/api/CaseWorkflowStatusRole/ByCaseWorkflowStatusGuid/x',
    )
  })
})
