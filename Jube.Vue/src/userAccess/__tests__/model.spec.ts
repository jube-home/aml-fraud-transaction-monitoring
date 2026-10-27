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
import { accessOf, passwordRequest } from '../model'

describe('accessOf', () => {
  it('reads the saved wire hashing and lock flags', () => {
    expect(accessOf({ wirePasswordHash: true, passwordLocked: true })).toEqual({
      wirePasswordHash: true,
      passwordLocked: true,
    })
    expect(accessOf({ wirePasswordHash: false, passwordLocked: false })).toEqual({
      wirePasswordHash: false,
      passwordLocked: false,
    })
  })

  it('hashes on the wire unless the record says otherwise, and treats a missing lock as open', () => {
    expect(accessOf({})).toEqual({ wirePasswordHash: true, passwordLocked: false })
    expect(accessOf({ wirePasswordHash: null, passwordLocked: null })).toEqual({
      wirePasswordHash: true,
      passwordLocked: false,
    })
    expect(accessOf({ passwordLocked: 'true' }).passwordLocked).toBe(false)
  })
})

describe('passwordRequest', () => {
  it('carries the user id and the saved wire hashing choice', () => {
    expect(passwordRequest(7, { wirePasswordHash: false, passwordLocked: true })).toEqual({
      id: 7,
      wirePasswordHash: false,
    })
  })
})
