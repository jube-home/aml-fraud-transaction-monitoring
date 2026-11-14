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

import {describe, expect, it, vi} from 'vitest'
import type {QueryClient} from '@tanstack/vue-query'
import {ApiError} from '../../../api/useApi'
import {checklistKeys} from '../api'
import {checklistRefusal, handleChecklistRefusal, serverMessage} from '../refusal'

const incomplete = new ApiError(409, {
  code: 'ChecklistIncomplete',
  message: 'Complete the checklist first.',
  openItems: ['Contact the customer', 'Check related cases'],
})

describe('checklistRefusal', () => {
  it('reads the open items out of a 409 checklist refusal', () => {
    expect(checklistRefusal(incomplete)).toEqual({
      message: 'Complete the checklist first.',
      openItems: ['Contact the customer', 'Check related cases'],
    })
  })

  it('ignores other conflicts and other errors', () => {
    expect(checklistRefusal(new ApiError(409, 'Held by another user.'))).toBeNull()
    expect(checklistRefusal(new ApiError(409, {code: 'Other'}))).toBeNull()
    expect(checklistRefusal(new ApiError(500, {code: 'ChecklistIncomplete'}))).toBeNull()
    expect(checklistRefusal(new Error('boom'))).toBeNull()
  })
})

describe('serverMessage', () => {
  it('prefers the first validation message', () => {
    const error = new ApiError(400, {
      errors: [{propertyName: 'Item', errorMessage: 'Add a note before ticking this item.'}],
    })

    expect(serverMessage(error)).toBe('Add a note before ticking this item.')
  })

  it('reads a plain conflict message', () => {
    expect(serverMessage(new ApiError(409, 'Take the case first.'))).toBe('Take the case first.')
  })

  it('has nothing for other failures', () => {
    expect(serverMessage(new ApiError(500, null))).toBeNull()
    expect(serverMessage(new Error('boom'))).toBeNull()
  })
})

describe('handleChecklistRefusal', () => {
  it('shows the server message and refetches the checklist', () => {
    const invalidate = vi.fn()
    const error = vi.fn()

    const handled = handleChecklistRefusal(incomplete, {
      client: {invalidateQueries: invalidate} as unknown as QueryClient,
      error,
      fallback: 'fallback',
    })

    expect(handled).toBe(true)
    expect(error).toHaveBeenCalledWith('Complete the checklist first.')
    expect(invalidate).toHaveBeenCalledWith({queryKey: checklistKeys.all})
  })

  it('falls back when the server sent no message', () => {
    const error = vi.fn()

    handleChecklistRefusal(new ApiError(409, {code: 'ChecklistIncomplete'}), {
      client: {invalidateQueries: vi.fn()} as unknown as QueryClient,
      error,
      fallback: 'fallback',
    })

    expect(error).toHaveBeenCalledWith('fallback')
  })

  it('leaves every other error to the caller', () => {
    const invalidate = vi.fn()

    const handled = handleChecklistRefusal(new ApiError(500, null), {
      client: {invalidateQueries: invalidate} as unknown as QueryClient,
      error: vi.fn(),
      fallback: 'fallback',
    })

    expect(handled).toBe(false)
    expect(invalidate).not.toHaveBeenCalled()
  })
})
