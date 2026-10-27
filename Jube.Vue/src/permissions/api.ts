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
import { useApi } from '../api/useApi'
import type { PermissionSpecification } from './areas'
import type { Change, Grant } from './plan'

export interface RoleOption {
  guid: string
  id: number
  name: string
  active: boolean
}

interface GrantDto extends Grant {
  [key: string]: unknown
}

export const permissionApi = {
  roles: () => useApi<RoleOption[]>('/api/RoleRegistry'),
  specifications: () => useApi<PermissionSpecification[]>('/api/PermissionSpecification'),
  grants: async (roleRegistryId: number): Promise<GrantDto[]> => {
    const all = await useApi<GrantDto[]>('/api/RoleRegistryPermission')

    return all.filter((grant) => grant.roleRegistryId === roleRegistryId)
  },
}

export async function applyChanges(
  roleRegistryId: number,
  grants: GrantDto[],
  changes: Change[],
): Promise<void> {
  await Promise.all(
    changes.map((change) => {
      if (change.kind === 'create') {
        return useApi('/api/RoleRegistryPermission', {
          method: 'POST',
          body: {
            roleRegistryId,
            permissionSpecificationId: change.permissionSpecificationId,
            active: true,
            locked: false,
          },
        })
      }

      if (change.kind === 'remove') {
        return useApi(`/api/RoleRegistryPermission/${change.grantId}`, { method: 'DELETE' })
      }

      const grant = grants.find((candidate) => candidate.id === change.grantId)!

      return useApi('/api/RoleRegistryPermission', {
        method: 'PUT',
        body: { ...grant, active: true },
      })
    }),
  )
}
