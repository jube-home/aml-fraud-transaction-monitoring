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
import { queryKeys } from '../api/queryKeys'

export interface Me {
  userName: string
  landlord: boolean
  permissions: number[]
  tenantRegistryId: number
  canChangePassword: boolean
  askJoobyInference: boolean
  sessionExpiry: string | null
}

export const meQueryOptions = {
  queryKey: queryKeys.me(),
  queryFn: () => useApi<Me>('/api/Me', { anonymous: true }),
  staleTime: 5 * 60_000,
}

export function hasPermission(me: Me | undefined, ...permissions: number[]): boolean {
  return me !== undefined && permissions.some((permission) => me.permissions.includes(permission))
}
