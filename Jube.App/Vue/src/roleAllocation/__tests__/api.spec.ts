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
import {beforeEach, describe, expect, it, vi} from 'vitest'

const useApi = vi.hoisted(() => vi.fn())

vi.mock('../../api/useApi', () => ({useApi}))

import {applyRoleChanges, roleAllocationApi} from '../api'

const target = {
  baseEndpoint: '/api/CaseWorkflowStatusRole',
  parentKeyField: 'caseWorkflowStatusGuid',
  guid: '11111111-1111-1111-1111-111111111111',
}

beforeEach(() => {
  useApi.mockReset()
  useApi.mockResolvedValue({})
})

describe('roleAllocationApi', () => {
  it('lists the roles of the tenant and the assignments of one record', async () => {
    await roleAllocationApi.roles()
    await roleAllocationApi.assignments(target)

    expect(useApi).toHaveBeenNthCalledWith(1, '/api/RoleRegistry')
    expect(useApi).toHaveBeenNthCalledWith(
      2,
      '/api/CaseWorkflowStatusRole/ByCaseWorkflowStatusGuid/11111111-1111-1111-1111-111111111111',
    )
  })
})

describe('applyRoleChanges', () => {
  it('posts one assignment per wanted role with the parent key and deletes by assignment id', async () => {
    await applyRoleChanges(target, {assign: ['role-a', 'role-b'], unassign: [40]})

    expect(useApi).toHaveBeenCalledTimes(3)
    expect(useApi).toHaveBeenCalledWith('/api/CaseWorkflowStatusRole', {
      method: 'POST',
      body: {caseWorkflowStatusGuid: target.guid, roleRegistryGuid: 'role-a'},
    })
    expect(useApi).toHaveBeenCalledWith('/api/CaseWorkflowStatusRole', {
      method: 'POST',
      body: {caseWorkflowStatusGuid: target.guid, roleRegistryGuid: 'role-b'},
    })
    expect(useApi).toHaveBeenCalledWith('/api/CaseWorkflowStatusRole/40', {method: 'DELETE'})
  })

  it('sends nothing when nothing changed', async () => {
    await applyRoleChanges(target, {assign: [], unassign: []})

    expect(useApi).not.toHaveBeenCalled()
  })

  it('rejects when a request is refused', async () => {
    const refused = new Error('HTTP 400')

    useApi.mockRejectedValueOnce(refused)

    await expect(applyRoleChanges(target, {assign: ['role-a'], unassign: []})).rejects.toBe(
      refused,
    )
  })
})
