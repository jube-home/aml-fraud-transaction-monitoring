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

import { ApiError, useApi, useApiResult } from '../api/useApi'

export interface Credentials {
  userName: string
  password: string
  newPassword?: string
  repeatNewPassword?: string
  mfa?: string
}

export type SignInOutcome =
  | { kind: 'signedIn' }
  | { kind: 'mfaRequired' }
  | { kind: 'passwordChangeRequired' }
  | { kind: 'invalid' }
  | { kind: 'rejected'; messages: string[] }
  | { kind: 'failed'; error: ApiError }

const basePath = '/api/Authentication'

export async function sha256Hex(input: string): Promise<string> {
  const bytes = new TextEncoder().encode(input)
  const digest = await crypto.subtle.digest('SHA-256', bytes)

  return Array.from(new Uint8Array(digest), (byte) => byte.toString(16).padStart(2, '0')).join('')
}

export async function readWirePasswordHash(userName: string): Promise<boolean> {
  const scheme = await useApi<{ wirePasswordHash: boolean }>(`${basePath}/WirePasswordHash`, {
    method: 'POST',
    body: { userName },
    anonymous: true,
  })

  return scheme.wirePasswordHash
}

export function serverMessages(body: unknown): { general: string[]; fields: Set<string> } {
  const general: string[] = []
  const fields = new Set<string>()
  const errors = (body as { errors?: unknown } | undefined)?.errors

  if (Array.isArray(errors)) {
    for (const entry of errors) {
      if (typeof entry !== 'object' || entry === null) continue

      const { errorMessage, propertyName } = entry as Record<string, unknown>

      if (typeof errorMessage === 'string') general.push(errorMessage)
      if (typeof propertyName === 'string') fields.add(propertyName.toLowerCase())
    }
  }

  return { general, fields }
}

export async function signIn(
  credentials: Credentials,
  wirePasswordHash: boolean,
  passwordChange: boolean,
): Promise<SignInOutcome> {
  const protect = (value: string | undefined): Promise<string | null> =>
    value === undefined || value === ''
      ? Promise.resolve(null)
      : wirePasswordHash
        ? sha256Hex(value + credentials.userName)
        : Promise.resolve(value)

  const [password, newPassword, repeatNewPassword] = await Promise.all([
    protect(credentials.password),
    passwordChange ? protect(credentials.newPassword) : Promise.resolve(null),
    passwordChange ? protect(credentials.repeatNewPassword) : Promise.resolve(null),
  ])

  try {
    const result = await useApiResult(`${basePath}/ByUserNamePassword`, {
      method: 'POST',
      anonymous: true,
      body: {
        userName: credentials.userName,
        password,
        newPassword,
        repeatNewPassword,
        passwordChangeState: passwordChange,
        mfa: credentials.mfa || null,
      },
    })

    return result.status === 202 ? { kind: 'mfaRequired' } : { kind: 'signedIn' }
  } catch (error) {
    if (!(error instanceof ApiError)) throw error

    if (error.status === 401) return { kind: 'invalid' }
    if (error.status === 403) return { kind: 'passwordChangeRequired' }

    if (error.status === 400) {
      const { general, fields } = serverMessages(error.body)

      if (fields.has('mfa') && !passwordChange) return { kind: 'mfaRequired' }

      return { kind: 'rejected', messages: general }
    }

    return { kind: 'failed', error }
  }
}

export type NegotiateOutcome = 'signedIn' | 'mfaRequired' | 'unavailable'

export async function signInByNegotiate(): Promise<NegotiateOutcome> {
  try {
    const result = await useApiResult(`${basePath}/ByNegotiate`, { anonymous: true })

    return result.status === 202 ? 'mfaRequired' : 'signedIn'
  } catch {
    return 'unavailable'
  }
}

export async function submitNegotiateMfa(mfa: string): Promise<'signedIn' | 'invalid' | 'failed'> {
  try {
    await useApiResult(`${basePath}/ByNegotiateMfa`, {
      method: 'POST',
      anonymous: true,
      body: { mfa },
    })

    return 'signedIn'
  } catch (error) {
    if (error instanceof ApiError && (error.status === 400 || error.status === 401)) {
      return 'invalid'
    }

    return 'failed'
  }
}
