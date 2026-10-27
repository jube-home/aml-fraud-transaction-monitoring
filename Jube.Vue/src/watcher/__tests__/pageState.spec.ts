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
import { defaultPageState, parsePageState, sameState, serialisePageState } from '../pageState'

describe('watcher page state', () => {
  it('reads nothing as the defaults', () => {
    expect(parsePageState({})).toEqual(defaultPageState)
  })

  it('writes nothing for the defaults', () => {
    expect(serialisePageState(defaultPageState)).toEqual({})
  })

  it('round trips every field', () => {
    const state = {
      key: 'AccountId',
      keyValue: 'ACC-1',
      live: false,
      mode: 'all' as const,
      window: 15,
      fade: 0,
      trails: false,
      heat: true,
      from: '2026-10-21T10:00',
      to: '2026-10-21T11:00',
    }

    expect(parsePageState(serialisePageState(state))).toEqual(state)
  })

  it('ignores values outside the choices the controls offer', () => {
    const state = parsePageState({ window: '7', fade: 'x', mode: 'nonsense', live: 'maybe' })

    expect(state.window).toBe(0)
    expect(state.fade).toBe(15)
    expect(state.mode).toBe('latest')
    expect(state.live).toBe(true)
  })

  it('keeps the replay range only when both ends are valid', () => {
    expect(parsePageState({ from: '2026-10-21T10:00' }).from).toBe('')
    expect(parsePageState({ from: 'soon', to: '2026-10-21T11:00' }).to).toBe('')
    expect(parsePageState({ from: '2026-10-21T10:00', to: '2026-10-21T11:00' }).to).toBe(
      '2026-10-21T11:00',
    )
  })

  it('treats array and missing query values as empty', () => {
    expect(parsePageState({ key: ['a', 'b'] }).key).toBe('')
  })

  it('compares states by what they write to the address', () => {
    expect(sameState(defaultPageState, { ...defaultPageState })).toBe(true)
    expect(sameState(defaultPageState, { ...defaultPageState, heat: true })).toBe(false)
  })
})
