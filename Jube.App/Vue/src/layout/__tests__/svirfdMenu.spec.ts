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

import {describe, expect, it} from 'vitest'
import type {Me} from '../../auth/me'
import {visibleMenu} from '../menu'

function me(permissions: number[]): Me {
  return {
    userName: 'alex',
    landlord: false,
    permissions,
    tenantRegistryId: 1,
    canChangePassword: true,
    askJoobyInference: true,
    svirfd: true,
    sessionExpiry: null,
  }
}

const hasSvirfd = (permissions: number[]): boolean =>
  visibleMenu(me(permissions)).some((node) => node.key === 'svirfd')

describe('the Svirfd menu entry', () => {
  it('needs View Svirfd (72) and nothing else grants it', () => {
    expect(hasSvirfd([72])).toBe(true)
    expect(hasSvirfd([])).toBe(false)
    expect(hasSvirfd([15])).toBe(false)
    expect(hasSvirfd([60])).toBe(false)
    expect(hasSvirfd([71, 73])).toBe(false)
  })
})
