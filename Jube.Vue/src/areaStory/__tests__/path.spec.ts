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
import { decodeAreaAddress, emptyAddress, encodeAreaAddress } from '../path'

const roots = ['users', 'roles', 'permissions']

describe('area story address', () => {
  it('is empty before a root is chosen', () => {
    expect(encodeAreaAddress(emptyAddress)).toBe('')
    expect(decodeAreaAddress(undefined, roots)).toEqual(emptyAddress)
    expect(decodeAreaAddress('', roots)).toEqual(emptyAddress)
  })

  it('round trips a bare root', () => {
    const address = { ...emptyAddress, rootId: 'permissions' }

    expect(encodeAreaAddress(address)).toBe('permissions')
    expect(decodeAreaAddress('permissions', roots)).toEqual(address)
  })

  it('round trips a record with a view and a drill down', () => {
    const address = {
      rootId: 'roles',
      recordId: 3,
      view: 'permissions',
      steps: [{ area: 'UserRegistry', id: 9 }],
    }

    expect(encodeAreaAddress(address)).toBe('roles:3,~permissions,UserRegistry:9')
    expect(decodeAreaAddress('roles:3,~permissions,UserRegistry:9', roots)).toEqual(address)
  })

  it('ignores unknown roots and malformed steps', () => {
    expect(decodeAreaAddress('nothing:3', roots)).toEqual(emptyAddress)
    expect(decodeAreaAddress('users:x', roots).recordId).toBeUndefined()
    expect(decodeAreaAddress('users:2,Bad:0,Good:4,Nope', roots).steps).toEqual([
      { area: 'Good', id: 4 },
    ])
  })

  it('drops steps when no record is chosen', () => {
    expect(decodeAreaAddress('users,UserRegistry:2', roots).steps).toEqual([])
  })
})
