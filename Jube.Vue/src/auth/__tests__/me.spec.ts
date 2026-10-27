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
import { hasPermission, type Me } from '../me'

const me = (permissions: number[]): Me => ({
  userName: 'alex',
  landlord: false,
  permissions,
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: false,
  sessionExpiry: null,
})

describe('hasPermission', () => {
  it('is true when the user holds any of the permissions asked about', () => {
    expect(hasPermission(me([5, 60]), 5)).toBe(true)
    expect(hasPermission(me([60]), 5, 60)).toBe(true)
  })

  it('is false when none is held, and while the user is not yet known', () => {
    expect(hasPermission(me([60]), 5)).toBe(false)
    expect(hasPermission(me([]), 5)).toBe(false)
    expect(hasPermission(undefined, 5)).toBe(false)
  })
})
