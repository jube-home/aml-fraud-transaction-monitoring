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
import { bodyReasons, describeApiError, validationReasons } from '../problems'
import { ApiError } from '../useApi'

const text = { failed: 'Failed.', refused: 'Refused.' }

describe('bodyReasons', () => {
  it('prefers a reasons list, then a message, then the fallback', () => {
    expect(bodyReasons({ reasons: ['a', 'b'], message: 'm' }, 'x')).toEqual(['a', 'b'])
    expect(bodyReasons({ message: 'm' }, 'x')).toEqual(['m'])
    expect(bodyReasons({ reasons: [] }, 'x')).toEqual(['x'])
    expect(bodyReasons(undefined, 'x')).toEqual(['x'])
  })
})

describe('describeApiError', () => {
  it('says refused for a missing permission and failed for anything it does not know', () => {
    expect(describeApiError(new ApiError(403, undefined), text)).toEqual(['Refused.'])
    expect(describeApiError(new ApiError(401, undefined), text)).toEqual(['Refused.'])
    expect(describeApiError(new ApiError(500, undefined), text)).toEqual(['Failed.'])
    expect(describeApiError(new Error('boom'), text)).toEqual(['Failed.'])
  })

  it('falls back to failed for a refusal when no refused text is given', () => {
    expect(describeApiError(new ApiError(403, undefined), { failed: 'Failed.' })).toEqual([
      'Failed.',
    ])
  })

  it('lists the server validation reasons, field errors first', () => {
    const body = { errors: [{ propertyName: 'X', errorMessage: 'Too long.' }] }

    expect(describeApiError(new ApiError(400, body), text)).toEqual(['Too long.'])
    expect(validationReasons(new ApiError(400, { message: 'No.' }), 'f')).toEqual(['No.'])
    expect(validationReasons(new ApiError(400, undefined), 'f')).toEqual(['f'])
  })
})
