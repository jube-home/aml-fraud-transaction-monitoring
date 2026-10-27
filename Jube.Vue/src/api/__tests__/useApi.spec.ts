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
import { ApiError, resetAntiforgeryToken, setApiHandlers, useApi } from '../useApi'

function respond(status: number, body?: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), { status })
}

describe('useApi', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    setApiHandlers({})
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('returns parsed JSON and sends same-origin credentials without an antiforgery header on reads', async () => {
    fetchMock.mockResolvedValueOnce(respond(200, { value: 1 }))

    const result = await useApi<{ value: number }>('/api/Thing')

    expect(result).toEqual({ value: 1 })
    expect(fetchMock).toHaveBeenCalledTimes(1)

    const [url, init] = fetchMock.mock.calls[0]!
    const headers = init?.headers as Record<string, string>
    expect(url).toBe('/api/Thing')
    expect(init?.credentials).toBe('same-origin')
    expect(headers['X-XSRF-TOKEN']).toBeUndefined()
  })

  it('appends defined query parameters only', async () => {
    fetchMock.mockResolvedValueOnce(respond(200, []))

    await useApi('/api/Thing', { query: { page: 2, q: undefined, active: true } })

    expect(fetchMock.mock.calls[0]![0]).toBe('/api/Thing?page=2&active=true')
  })

  it('fetches an antiforgery token once and sends it on every write', async () => {
    fetchMock
      .mockResolvedValueOnce(respond(200, { token: 'tok' }))
      .mockResolvedValueOnce(respond(200, { ok: true }))
      .mockResolvedValueOnce(respond(200, { ok: true }))

    await useApi('/api/Thing', { method: 'POST', body: { a: 1 } })
    await useApi('/api/Thing', { method: 'PUT', body: { a: 2 } })

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/AntiforgeryToken')

    for (const call of fetchMock.mock.calls.slice(1)) {
      const headers = call[1]?.headers as Record<string, string>
      expect(headers['X-XSRF-TOKEN']).toBe('tok')
      expect(headers['Content-Type']).toBe('application/json')
    }

    expect(fetchMock.mock.calls[1]![1]?.body).toBe('{"a":1}')
  })

  it('returns undefined for 204 responses', async () => {
    fetchMock.mockResolvedValueOnce(respond(204))

    expect(await useApi('/api/Thing')).toBeUndefined()
  })

  it.each([
    [401, 'unauthorized'],
    [403, 'forbidden'],
    [404, 'notFound'],
    [409, 'conflict'],
    [412, 'conflict'],
    [400, 'validation'],
    [422, 'validation'],
    [500, 'server'],
    [503, 'server'],
  ])('maps HTTP %i to the %s kind', async (status, kind) => {
    fetchMock.mockResolvedValueOnce(respond(status, {}))

    const error = await useApi('/api/Thing').catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).kind).toBe(kind)
    expect((error as ApiError).status).toBe(status)
  })

  it('maps FluentValidation errors onto field names', async () => {
    fetchMock.mockResolvedValueOnce(
      respond(400, {
        isValid: false,
        errors: [
          { propertyName: 'Name', errorMessage: 'Required' },
          { propertyName: 'Name', errorMessage: 'Too short' },
          { propertyName: 'Age', errorMessage: 'Must be positive' },
        ],
      }),
    )

    const error = (await useApi('/api/Thing').catch((caught: unknown) => caught)) as ApiError

    expect(error.fieldErrors).toEqual({
      Name: ['Required', 'Too short'],
      Age: ['Must be positive'],
    })
  })

  it('maps ASP.NET problem-details errors onto field names', async () => {
    fetchMock.mockResolvedValueOnce(respond(422, { errors: { Email: ['Invalid'] } }))

    const error = (await useApi('/api/Thing').catch((caught: unknown) => caught)) as ApiError

    expect(error.fieldErrors).toEqual({ Email: ['Invalid'] })
  })

  it('leaves field errors empty for non-validation failures', async () => {
    fetchMock.mockResolvedValueOnce(respond(500, { errors: { Email: ['Invalid'] } }))

    const error = (await useApi('/api/Thing').catch((caught: unknown) => caught)) as ApiError

    expect(error.fieldErrors).toEqual({})
  })

  it('calls the unauthorized handler on 401 and forgets the antiforgery token', async () => {
    const onUnauthorized = vi.fn<() => void>()
    setApiHandlers({ onUnauthorized })
    fetchMock
      .mockResolvedValueOnce(respond(200, { token: 'one' }))
      .mockResolvedValueOnce(respond(401))
      .mockResolvedValueOnce(respond(200, { token: 'two' }))
      .mockResolvedValueOnce(respond(200, {}))

    await useApi('/api/Thing', { method: 'POST' }).catch(() => undefined)
    expect(onUnauthorized).toHaveBeenCalledTimes(1)

    await useApi('/api/Thing', { method: 'POST' })

    expect(fetchMock.mock.calls[3]![1]?.headers).toMatchObject({ 'X-XSRF-TOKEN': 'two' })
  })

  it('does not call the unauthorized handler on 403', async () => {
    const onUnauthorized = vi.fn<() => void>()
    setApiHandlers({ onUnauthorized })
    fetchMock.mockResolvedValueOnce(respond(403))

    await useApi('/api/Thing').catch(() => undefined)

    expect(onUnauthorized).not.toHaveBeenCalled()
  })

  it('reports a network failure as status 0', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))

    const error = (await useApi('/api/Thing').catch((caught: unknown) => caught)) as ApiError

    expect(error.kind).toBe('network')
    expect(error.status).toBe(0)
  })

  it('lets an abort through unchanged', async () => {
    fetchMock.mockRejectedValueOnce(new DOMException('Aborted', 'AbortError'))

    const error = await useApi('/api/Thing').catch((caught: unknown) => caught)

    expect(error).toBeInstanceOf(DOMException)
  })

  it('retries fetching the antiforgery token after it failed', async () => {
    fetchMock
      .mockResolvedValueOnce(respond(500))
      .mockResolvedValueOnce(respond(200, { token: 'again' }))
      .mockResolvedValueOnce(respond(200, {}))

    await useApi('/api/Thing', { method: 'POST' }).catch(() => undefined)
    await useApi('/api/Thing', { method: 'POST' })

    expect(fetchMock.mock.calls[2]![1]?.headers).toMatchObject({ 'X-XSRF-TOKEN': 'again' })
  })
})
