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
import { createQueryClient, shouldRetryRead } from '../queryClient'
import { ApiError } from '../useApi'

describe('read retry policy', () => {
  it('retries server and network failures up to three times', () => {
    expect(shouldRetryRead(0, new ApiError(500, undefined))).toBe(true)
    expect(shouldRetryRead(2, new ApiError(0, undefined))).toBe(true)
    expect(shouldRetryRead(3, new ApiError(500, undefined))).toBe(false)
  })

  it.each([401, 403, 404, 409, 422])('never retries a %i', (status) => {
    expect(shouldRetryRead(0, new ApiError(status, undefined))).toBe(false)
  })

  it('never retries writes', () => {
    expect(createQueryClient().getDefaultOptions().mutations?.retry).toBe(false)
  })
})
