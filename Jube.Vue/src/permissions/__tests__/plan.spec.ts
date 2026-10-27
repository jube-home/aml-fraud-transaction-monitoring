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
import { areaOf, groupPermissions } from '../areas'
import { grantedIds, planChanges, type Grant } from '../plan'

const grant = (id: number, permission: number, active = true, locked = false): Grant => ({
  id,
  roleRegistryId: 1,
  permissionSpecificationId: permission,
  active,
  locked,
})

describe('grantedIds', () => {
  it('lists only active grants', () => {
    expect([...grantedIds([grant(1, 5), grant(2, 6, false)])]).toEqual([5])
  })
})

describe('planChanges', () => {
  it('creates a grant that does not exist', () => {
    expect(planChanges([], new Set([7]))).toEqual([
      { kind: 'create', permissionSpecificationId: 7 },
    ])
  })

  it('reactivates an inactive grant rather than creating a duplicate', () => {
    expect(planChanges([grant(3, 7, false)], new Set([7]))).toEqual([
      { kind: 'activate', grantId: 3 },
    ])
  })

  it('removes a grant that is no longer wanted', () => {
    expect(planChanges([grant(3, 7)], new Set())).toEqual([{ kind: 'remove', grantId: 3 }])
  })

  it('leaves an inactive grant alone when it is not wanted', () => {
    expect(planChanges([grant(3, 7, false)], new Set())).toEqual([])
  })

  it('does nothing when the selection is unchanged', () => {
    expect(planChanges([grant(3, 7)], new Set([7]))).toEqual([])
  })

  it('leaves locked grants alone whatever is wanted', () => {
    expect(planChanges([grant(3, 7, true, true)], new Set())).toEqual([])
  })

  it('removes duplicate rows for the same permission', () => {
    expect(planChanges([grant(3, 7), grant(4, 7)], new Set([7]))).toEqual([
      { kind: 'remove', grantId: 4 },
    ])
  })
})

describe('areas', () => {
  it('puts unknown permissions in other so none disappear', () => {
    expect(areaOf(9999)).toBe('other')
  })

  it('groups in a fixed order, sorted by id, and drops empty groups', () => {
    const groups = groupPermissions([
      { id: 34, name: 'Role' },
      { id: 2, name: 'Suppression' },
      { id: 1, name: 'Case' },
    ])

    expect(groups.map((group) => group.area)).toEqual(['cases', 'security'])
    expect(groups[0]!.items.map((item) => item.id)).toEqual([1, 2])
  })
})
