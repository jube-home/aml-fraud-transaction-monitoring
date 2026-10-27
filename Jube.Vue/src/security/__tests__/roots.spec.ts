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
import type { Me } from '../../auth/me'
import { allowedRoots } from '../roots'

function me(permissions: number[], landlord = false): Me {
  return {
    userName: 'alex',
    landlord,
    permissions,
    tenantRegistryId: 1,
    canChangePassword: true,
    askJoobyInference: true,
    sessionExpiry: null,
  }
}

const ids = (user: Me | undefined) => allowedRoots(user).map((root) => root.id)

describe('security roots', () => {
  it('offers nothing before the user is known', () => {
    expect(ids(undefined)).toEqual([])
  })

  it('follows the permission of each root', () => {
    expect(ids(me([34]))).toEqual(['roles'])
    expect(ids(me([35]))).toEqual(['users', 'logins', 'logouts'])
    expect(ids(me([36]))).toEqual(['permissions'])
    expect(ids(me([34, 35, 36]))).toEqual(['users', 'roles', 'permissions', 'logins', 'logouts'])
  })

  it('does not offer tenants, which have their own entry', () => {
    expect(ids(me([34, 35, 36], true))).not.toContain('tenants')
    expect(ids(me([], true))).toEqual([])
  })

  it('names the form area of a root that has one', () => {
    expect(allowedRoots(me([34, 35, 36])).map((root) => root.area)).toEqual([
      'UserRegistry',
      'RoleRegistry',
      undefined,
      undefined,
      undefined,
    ])
  })
})
