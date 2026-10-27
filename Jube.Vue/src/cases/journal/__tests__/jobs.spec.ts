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
import { isActive, percent, pollDelay } from '../jobs'

describe('journal tag jobs', () => {
  it('is active while it is queued or running and not afterwards', () => {
    expect(isActive({ state: 'Queued' })).toBe(true)
    expect(isActive({ state: 'Running' })).toBe(true)
    expect(isActive({ state: 'Done' })).toBe(false)
    expect(isActive({ state: 'Failed' })).toBe(false)
    expect(isActive({ state: 'Cancelled' })).toBe(false)
  })

  it('works out the progress as a whole percentage that never passes one hundred', () => {
    expect(percent({ processed: 0, total: 0 })).toBe(0)
    expect(percent({ processed: 1, total: 3 })).toBe(33)
    expect(percent({ processed: 3, total: 3 })).toBe(100)
    expect(percent({ processed: 9, total: 3 })).toBe(100)
  })

  it('polls only while there is something to wait for', () => {
    expect(pollDelay(undefined)).toBe(false)
    expect(pollDelay({ state: 'Running' })).toBe(1500)
    expect(pollDelay({ state: 'Done' })).toBe(false)
  })
})
