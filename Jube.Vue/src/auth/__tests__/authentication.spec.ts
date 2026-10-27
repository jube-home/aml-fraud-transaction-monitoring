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

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { resetAntiforgeryToken, setApiHandlers } from '../../api/useApi'
import {
  readWirePasswordHash,
  sha256Hex,
  signIn,
  signInByNegotiate,
  submitNegotiateMfa,
} from '../authentication'

const fetchMock = vi.fn<typeof fetch>()
const unauthorized = vi.fn<() => void>()

function reply(status: number, body?: unknown): void {
  fetchMock.mockResolvedValueOnce(
    new Response(body === undefined ? null : JSON.stringify(body), { status }),
  )
}

function lastBody(): Record<string, unknown> {
  const init = fetchMock.mock.calls.at(-1)![1]!

  return JSON.parse(init.body as string) as Record<string, unknown>
}

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
  setApiHandlers({ onUnauthorized: unauthorized })
  resetAntiforgeryToken()
})

afterEach(() => {
  fetchMock.mockReset()
  unauthorized.mockReset()
  vi.unstubAllGlobals()
})

describe('sha256Hex', () => {
  it('matches the known digest of "abc"', async () => {
    expect(await sha256Hex('abc')).toBe(
      'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad',
    )
  })
})

describe('signIn', () => {
  const credentials = { userName: 'alex', password: 'pw' }

  it('reads the wire password scheme without an antiforgery token', async () => {
    reply(200, { wirePasswordHash: true })

    expect(await readWirePasswordHash('alex')).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/Authentication/WirePasswordHash')
  })

  it('hashes the password with the user name when the server asks for it', async () => {
    reply(200)

    expect(await signIn(credentials, true, false)).toEqual({ kind: 'signedIn' })
    expect(lastBody().password).toBe(await sha256Hex('pwalex'))
    expect(lastBody().passwordChangeState).toBe(false)
  })

  it('sends the plain password otherwise', async () => {
    reply(200)
    await signIn(credentials, false, false)

    expect(lastBody().password).toBe('pw')
  })

  it('sends the new password pair only for a password change', async () => {
    reply(200)
    await signIn({ ...credentials, newPassword: 'n', repeatNewPassword: 'n' }, false, true)

    expect(lastBody()).toMatchObject({
      newPassword: 'n',
      repeatNewPassword: 'n',
      passwordChangeState: true,
    })
  })

  it('maps a missing multi-factor code on a 400 to mfaRequired', async () => {
    reply(400, { errors: [{ propertyName: 'Mfa', errorMessage: "'Mfa' must not be empty." }] })

    expect(await signIn(credentials, false, false)).toEqual({ kind: 'mfaRequired' })
  })

  it('maps a 202 to mfaRequired', async () => {
    reply(202)

    expect(await signIn(credentials, false, false)).toEqual({ kind: 'mfaRequired' })
  })

  it('maps a 403 to passwordChangeRequired', async () => {
    reply(403)

    expect(await signIn(credentials, false, false)).toEqual({ kind: 'passwordChangeRequired' })
  })

  it('maps a 401 to invalid without triggering the session redirect', async () => {
    reply(401)

    expect(await signIn(credentials, false, false)).toEqual({ kind: 'invalid' })
    expect(unauthorized).not.toHaveBeenCalled()
  })

  it('carries password strength messages from a 400', async () => {
    reply(400, { errors: [{ errorMessage: 'Too short' }] })

    expect(await signIn(credentials, false, true)).toEqual({
      kind: 'rejected',
      messages: ['Too short'],
    })
  })

  it('reports a server failure', async () => {
    reply(500)

    expect((await signIn(credentials, false, false)).kind).toBe('failed')
  })
})

describe('Negotiate', () => {
  it('signs in on a 200, wants a code on a 202 and gives up on anything else', async () => {
    reply(200)
    expect(await signInByNegotiate()).toBe('signedIn')

    reply(202)
    expect(await signInByNegotiate()).toBe('mfaRequired')

    reply(401)
    expect(await signInByNegotiate()).toBe('unavailable')

    fetchMock.mockRejectedValueOnce(new Error('offline'))
    expect(await signInByNegotiate()).toBe('unavailable')
    expect(unauthorized).not.toHaveBeenCalled()
  })

  it('posts only the code and tells a wrong one from a failure', async () => {
    reply(200)
    expect(await submitNegotiateMfa('123456')).toBe('signedIn')
    expect(lastBody()).toEqual({ mfa: '123456' })

    reply(401)
    expect(await submitNegotiateMfa('000000')).toBe('invalid')

    reply(400)
    expect(await submitNegotiateMfa('0')).toBe('invalid')

    reply(500)
    expect(await submitNegotiateMfa('123456')).toBe('failed')
  })
})
