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
import {apiLanguage, setApiLanguage} from '../language'
import {ApiError, resetAntiforgeryToken, setApiHandlers, useApi, useApiStream} from '../useApi'
import {resetWire, setWireSchema} from '../wire'

function headersOf(call: Parameters<typeof fetch> | undefined): Record<string, string> {
  return (call?.[1]?.headers ?? {}) as Record<string, string>
}

describe('Accept-Language', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    resetWire()
    setWireSchema({hash: 'h', types: {}, routes: []})
    setApiHandlers({})
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
    setApiLanguage('en')
  })

  it('defaults to English', () => {
    expect(apiLanguage.value).toBe('en')
  })

  it.each(['en', 'pl', 'ar', 'ka', 'id', 'sv', 'ur', 'hi'])(
    'is sent as %s on a JSON read',
    async (locale) => {
      setApiLanguage(locale)
      fetchMock.mockResolvedValueOnce(new Response('{}', {status: 200}))

      await useApi('/api/Thing')

      expect(headersOf(fetchMock.mock.calls[0])['Accept-Language']).toBe(locale)
    },
  )

  it('follows a change of language between calls and also reaches anonymous and streamed calls', async () => {
    setApiLanguage('ka')
    fetchMock
      .mockResolvedValueOnce(new Response('{}', {status: 200}))
      .mockResolvedValueOnce(new Response('{}', {status: 200}))

    await useApi('/api/Thing', {anonymous: true})
    await useApiStream('/api/Stream', {anonymous: true})

    expect(headersOf(fetchMock.mock.calls[0])['Accept-Language']).toBe('ka')
    expect(headersOf(fetchMock.mock.calls[1])['Accept-Language']).toBe('ka')
  })
})

describe('ApiError kinds', () => {
  it.each([
    [0, 'network'],
    [401, 'unauthorized'],
    [403, 'forbidden'],
    [404, 'notFound'],
    [409, 'conflict'],
    [412, 'conflict'],
    [400, 'validation'],
    [422, 'validation'],
    [500, 'server'],
    [503, 'server'],
  ])('maps HTTP %i to %s', (status, kind) => {
    expect(new ApiError(status, undefined).kind).toBe(kind)
  })

  it('takes field errors only from a 400 or 422 and ignores hostile shapes', () => {
    expect(new ApiError(500, {errors: {A: ['x']}}).fieldErrors).toEqual({})
    expect(new ApiError(400, {errors: 'text'}).fieldErrors).toEqual({})
    expect(new ApiError(400, {errors: [null, 1, {propertyName: 1}]}).fieldErrors).toEqual({})
    expect(new ApiError(422, null).fieldErrors).toEqual({})
  })
})

describe('useApiStream', () => {
  const fetchMock = vi.fn<typeof fetch>()
  const unauthorized = vi.fn()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    setApiHandlers({onUnauthorized: unauthorized})
  })

  afterEach(() => {
    fetchMock.mockReset()
    unauthorized.mockReset()
    vi.unstubAllGlobals()
    setApiHandlers({})
  })

  it('sends the antiforgery token with a POST and returns the raw response', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(JSON.stringify({token: 'tok'}), {status: 200}))
      .mockResolvedValueOnce(new Response('data: x\n\n', {status: 200}))

    const response = await useApiStream('/api/Stream', {body: {a: 1}})

    expect(await response.text()).toBe('data: x\n\n')
    expect(fetchMock.mock.calls[1]![1]?.method).toBe('POST')
    expect(headersOf(fetchMock.mock.calls[1])['X-XSRF-TOKEN']).toBe('tok')
    expect(headersOf(fetchMock.mock.calls[1])['Accept']).toBe('text/event-stream')
  })

  it('raises an unauthorized ApiError and tells the handler on a 401', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(JSON.stringify({token: 'tok'}), {status: 200}))
      .mockResolvedValueOnce(new Response(null, {status: 401}))

    await expect(useApiStream('/api/Stream')).rejects.toMatchObject({kind: 'unauthorized'})
    expect(unauthorized).toHaveBeenCalledTimes(1)
  })

  it('reports a network failure as status 0 and lets an abort through', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('offline'))

    await expect(useApiStream('/api/Stream', {anonymous: true})).rejects.toMatchObject({
      status: 0,
    })

    fetchMock.mockRejectedValueOnce(new DOMException('stop', 'AbortError'))

    await expect(useApiStream('/api/Stream', {anonymous: true})).rejects.toMatchObject({
      name: 'AbortError',
    })
  })
})
