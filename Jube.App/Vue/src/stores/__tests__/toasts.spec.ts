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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {createPinia, setActivePinia} from 'pinia'
import {ApiError} from '../../api/useApi'
import {refusalMessage, useToastStore} from '../toasts'

beforeEach(() => {
  setActivePinia(createPinia())
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('the toast store', () => {
  it('shows a repeated message once', () => {
    const store = useToastStore()

    const first = store.push('error', 'Same')
    const second = store.push('error', 'Same')

    expect(second).toBe(first)
    expect(store.items).toHaveLength(1)
  })

  it('keeps only the four newest', () => {
    const store = useToastStore()

    for (let n = 0; n < 6; n += 1) store.push('info', `n${n}`)

    expect(store.items.map((toast) => toast.text)).toEqual(['n2', 'n3', 'n4', 'n5'])
  })

  it('removes a toast after its lifetime, errors lasting longer', () => {
    const store = useToastStore()

    store.push('info', 'quick')
    store.push('error', 'slow')
    vi.advanceTimersByTime(6000)
    expect(store.items.map((toast) => toast.text)).toEqual(['slow'])
    vi.advanceTimersByTime(4000)
    expect(store.items).toEqual([])
  })

  it('dismisses on request', () => {
    const store = useToastStore()
    const id = store.push('success', 'done')

    store.dismiss(id)

    expect(store.items).toEqual([])
  })
})

describe('the wording of a refusal', () => {
  it('is the reason the server gave with a conflict', () => {
    const error = new ApiError(409, ' This case is locked by another user. ')

    expect(refusalMessage(error)).toBe('This case is locked by another user.')
  })

  it('is read from the message of a coded refusal such as a locked case', () => {
    const error = new ApiError(409, {code: 'CaseLocked', message: 'The case is locked by another user.'})

    expect(refusalMessage(error)).toBe('The case is locked by another user.')
  })

  it('is read from the message of a case update refused for its lock', () => {
    const error = new ApiError(409, {code: 'CaseConflict', message: 'The case is locked by another user.'})

    expect(refusalMessage(error)).toBe('The case is locked by another user.')
  })

  it('leaves a version conflict to the general wording', () => {
    const error = new ApiError(409, {code: 'VersionConflict', message: 'Version 3 was supplied.'})

    expect(refusalMessage(error)).toBeUndefined()
  })

  it('falls back to the status wording when the server gave no reason of its own', () => {
    expect(refusalMessage(new ApiError(409, null))).toBeUndefined()
    expect(refusalMessage(new ApiError(409, ''))).toBeUndefined()
    expect(refusalMessage(new ApiError(409, {code: 'VersionConflict'}))).toBeUndefined()
    expect(refusalMessage(new ApiError(409, 'x'.repeat(301)))).toBeUndefined()
  })

  it('is never taken from any other kind of failure', () => {
    expect(refusalMessage(new ApiError(500, 'Something internal'))).toBeUndefined()
    expect(refusalMessage(new ApiError(403, 'Refused'))).toBeUndefined()
  })
})
