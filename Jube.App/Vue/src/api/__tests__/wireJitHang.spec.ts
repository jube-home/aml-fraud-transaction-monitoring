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

import {encode} from '@msgpack/msgpack'
import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {
  decodeWire,
  ensureWireShape,
  generatedDecodersInUse,
  loadWireHead,
  resetWire,
} from '../wire'

vi.mock('../../generated/wire.generated', () => new Promise(() => undefined))

const head = {
  hash: 'h1',
  mode: 'MessagePack',
  typesHash: 'whatever',
  generatedDecoders: true,
  encryption: false,
  types: {},
  routes: [],
}

const shapes = {
  hash: 'h1',
  types: {'Jube.Thing': {fields: [{index: 0, name: 'label', type: 'string'}]}},
  routes: [],
}

function body(): ArrayBuffer {
  const packed = encode(['hello'])

  return packed.buffer.slice(
    packed.byteOffset,
    packed.byteOffset + packed.byteLength,
  ) as ArrayBuffer
}

describe('decoding while the generated decoders never arrive', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    resetWire()
    fetchMock.mockReset()
    fetchMock.mockImplementation((input) =>
      Promise.resolve(
        new Response(JSON.stringify(String(input).includes('/Shapes') ? shapes : head), {
          status: 200,
        }),
      ),
    )
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('decodes with the per-shape path although the generated module never resolves', async () => {
    await ensureWireShape('Jube.Thing', 'decode', 'h1')

    expect(decodeWire(body(), 'Jube.Thing')).toEqual({label: 'hello'})
    expect(generatedDecodersInUse()).toBe(false)
  })

  it('fetches only the one shape needed, without waiting on the head or the whole catalogue', async () => {
    await ensureWireShape('Jube.Thing[]', 'decode', 'h1')

    const urls = fetchMock.mock.calls.map((call) => String(call[0]))

    expect(urls).toEqual(['/api/WireSchema/Shapes?shape=Jube.Thing%5B%5D&hash=h1'])
  })

  it('does not wait on a head that is still in flight', async () => {
    fetchMock.mockImplementation((input) =>
      String(input).includes('/Shapes')
        ? Promise.resolve(new Response(JSON.stringify(shapes), {status: 200}))
        : new Promise<Response>(() => undefined),
    )

    void loadWireHead()

    await ensureWireShape('Jube.Thing', 'decode', 'h1')

    expect(decodeWire(body(), 'Jube.Thing')).toEqual({label: 'hello'})
  })

  it('batches shapes requested together into a single request', async () => {
    await Promise.all([
      ensureWireShape('Jube.Thing', 'decode', 'h1'),
      ensureWireShape('Jube.Thing[]', 'decode', 'h1'),
      ensureWireShape('Jube.Thing?', 'decode', 'h1'),
    ])

    const urls = fetchMock.mock.calls.map((call) => String(call[0]))

    expect(urls).toHaveLength(1)
    expect(urls[0]).toBe(
      '/api/WireSchema/Shapes?shape=Jube.Thing&shape=Jube.Thing%5B%5D&shape=Jube.Thing%3F&hash=h1',
    )
  })

  it('asks once for the head and once for the shape however many callers are waiting', async () => {
    await Promise.all([
      loadWireHead(),
      loadWireHead(),
      ensureWireShape('Jube.Thing', 'decode', 'h1'),
      ensureWireShape('Jube.Thing', 'decode', 'h1'),
      ensureWireShape('Jube.Thing', 'decode'),
      ensureWireShape('Jube.Thing', 'encode'),
    ])

    const urls = fetchMock.mock.calls.map((call) => String(call[0]))

    expect(urls.filter((url) => url.includes('/Head'))).toHaveLength(1)
    expect(urls.filter((url) => url.includes('/Shapes'))).toHaveLength(1)

    await ensureWireShape('Jube.Thing', 'decode', 'h1')

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('resolves a generic type whose name ends in brackets', async () => {
    const generic = 'Jube.Dto.Payload.PayloadResult`1[[Jube.Thing, Jube.Dto, Version=1.0.0.0]]'

    fetchMock.mockImplementation((input) =>
      Promise.resolve(
        new Response(
          JSON.stringify(
            String(input).includes('/Shapes')
              ? {...shapes, types: {[generic]: shapes.types['Jube.Thing']}}
              : head,
          ),
          {status: 200},
        ),
      ),
    )

    await ensureWireShape(`${generic}[]`, 'decode', 'h1')

    expect(decodeWire(encode([['hello']]).slice().buffer as ArrayBuffer, `${generic}[]`)).toEqual([
      {label: 'hello'},
    ])
  })
})
