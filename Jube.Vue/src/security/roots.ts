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

import type { Me } from '../auth/me'

export type SecurityRootId = 'users' | 'roles' | 'permissions' | 'logins' | 'logouts'

interface Rule {
  id: SecurityRootId
  permissions: number[]
  area?: string
}

export const securityRoles = 34
export const securityUsers = 35
export const securityPermissions = 36

const rules: Rule[] = [
  { id: 'users', permissions: [securityUsers], area: 'UserRegistry' },
  { id: 'roles', permissions: [securityRoles], area: 'RoleRegistry' },
  { id: 'permissions', permissions: [securityPermissions] },
  { id: 'logins', permissions: [securityUsers] },
  { id: 'logouts', permissions: [securityUsers] },
]

export function allowedRoots(me: Me | undefined): { id: SecurityRootId; area?: string }[] {
  if (!me) return []

  return rules
    .filter((rule) => rule.permissions.some((permission) => me.permissions.includes(permission)))
    .map((rule) => ({ id: rule.id, area: rule.area }))
}
