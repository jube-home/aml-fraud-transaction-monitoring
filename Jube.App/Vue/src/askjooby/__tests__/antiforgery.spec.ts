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
import {resetAntiforgeryToken, setApiHandlers} from '../../api/useApi'
import {resetWire, setWireSchema} from '../../api/wire'
import {joobyApi} from '../api'

function headersOf(call: Parameters<typeof fetch> | undefined): Record<string, string> {
  return (call?.[1]?.headers ?? {}) as Record<string, string>
}

describe('Ask Jooby mutations carry the antiforgery token', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    resetWire()
    setWireSchema({hash: 'h', types: {}, routes: []})
    setApiHandlers({})
    fetchMock.mockImplementation((input) =>
      Promise.resolve(
        String(input).endsWith('/api/AntiforgeryToken')
          ? new Response(JSON.stringify({token: 'tok'}), {status: 200})
          : new Response('{}', {status: 200, headers: {'Content-Type': 'application/json'}}),
      ),
    )
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  const mutations: [string, () => Promise<unknown>, string][] = [
    ['create', () => joobyApi.create(), 'POST'],
    ['remove', () => joobyApi.remove(7), 'DELETE'],
    ['guidance', () => joobyApi.guidance(7, 'steer'), 'POST'],
    ['feedback', () => joobyApi.feedback(7, 3, true), 'POST'],
    ['decideApproval', () => joobyApi.decideApproval(7, 'a', true), 'POST'],
    ['removeFile', () => joobyApi.removeFile(4), 'DELETE'],
    ['upload', () => joobyApi.upload(7, new File(['x'], 'x.csv')), 'POST'],
  ]

  it.each(mutations)('%s sends X-XSRF-TOKEN on a %s', async (_name, call, method) => {
    await call()

    const sent = fetchMock.mock.calls.find((c) => c[1]?.method === method)

    expect(headersOf(sent)['X-XSRF-TOKEN']).toBe('tok')
  })

  it.each(['Start', 'SendMessage'] as const)('streams %s with X-XSRF-TOKEN', async (path) => {
    await joobyApi.stream(path, 7, {}, new AbortController().signal).catch(() => undefined)

    const sent = fetchMock.mock.calls.find((c) => c[1]?.method === 'POST')

    expect(sent?.[0]).toBe(`/api/AskJoobyConversation/7/${path}`)
    expect(headersOf(sent)['X-XSRF-TOKEN']).toBe('tok')
  })
})
