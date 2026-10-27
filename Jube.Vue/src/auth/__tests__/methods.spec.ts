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

import { afterEach, describe, expect, it, vi } from 'vitest'
import { noSignInMethods, oauthAddress, readSignInMethods } from '../methods'
import { setApiHandlers } from '../../api/useApi'

describe('readSignInMethods', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('reads the two flags and treats anything else as off', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(
        async () =>
          new Response(JSON.stringify({ negotiate: true, oauth: 'yes' }), { status: 200 }),
      ),
    )
    setApiHandlers({})

    expect(await readSignInMethods()).toEqual({ negotiate: true, oauth: false })
  })

  it('falls back to no single sign-on when the server cannot be read', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('', { status: 500 })),
    )
    setApiHandlers({})

    expect(await readSignInMethods()).toEqual(noSignInMethods)
  })
})

describe('oauthAddress', () => {
  it('carries the return address, encoded', () => {
    expect(oauthAddress('/a b?c=d')).toBe('/api/Authentication/OAuth?redirectUrl=%2Fa%20b%3Fc%3Dd')
  })
})
