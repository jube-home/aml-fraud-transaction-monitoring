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

import {applyChanges, permissionApi} from '../api'

const stored = {
  id: 3,
  roleRegistryId: 9,
  permissionSpecificationId: 7,
  active: false,
  locked: false,
  version: 4,
}

beforeEach(() => {
  useApi.mockReset()
  useApi.mockResolvedValue({})
})

describe('permissionApi.grants', () => {
  it('keeps only the grants of the requested role from the tenant wide list', async () => {
    useApi.mockResolvedValue([
      {...stored, id: 1, roleRegistryId: 9},
      {...stored, id: 2, roleRegistryId: 10},
    ])

    const grants = await permissionApi.grants(9)

    expect(useApi).toHaveBeenCalledWith('/api/RoleRegistryPermission')
    expect(grants.map((grant) => grant.id)).toEqual([1])
  })
})

describe('applyChanges', () => {
  it('creates an active unlocked grant on the role it was given and nothing else', async () => {
    await applyChanges(9, [], [{kind: 'create', permissionSpecificationId: 12}])

    expect(useApi).toHaveBeenCalledTimes(1)
    expect(useApi).toHaveBeenCalledWith('/api/RoleRegistryPermission', {
      method: 'POST',
      body: {roleRegistryId: 9, permissionSpecificationId: 12, active: true, locked: false},
    })
  })

  it('removes by grant id', async () => {
    await applyChanges(9, [stored], [{kind: 'remove', grantId: 3}])

    expect(useApi).toHaveBeenCalledWith('/api/RoleRegistryPermission/3', {method: 'DELETE'})
  })

  it('activates by sending the stored grant back, version included, with active set', async () => {
    await applyChanges(9, [stored], [{kind: 'activate', grantId: 3}])

    expect(useApi).toHaveBeenCalledWith('/api/RoleRegistryPermission', {
      method: 'PUT',
      body: {...stored, active: true},
    })
  })

  it('rejects when any single request is refused so the caller can report and reload', async () => {
    const refused = new Error('HTTP 403')

    useApi.mockResolvedValueOnce({}).mockRejectedValueOnce(refused)

    await expect(
      applyChanges(
        9,
        [stored],
        [
          {kind: 'create', permissionSpecificationId: 12},
          {kind: 'remove', grantId: 3},
        ],
      ),
    ).rejects.toBe(refused)
  })
})
