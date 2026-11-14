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
import {resetAntiforgeryToken, setApiHandlers, useApi} from '../useApi'
import {
  decodeWire,
  resetWire,
  setWireSchema,
  wireSchemaHeader,
  wireShapeHeader,
  messagePackType,
} from '../wire'
import {inflateLz4, wireCompressionHeader} from '../wireCompression'

const serverCompressed =
  'C8012363D20000053FF91ADC002893A3474554AC6170692F456E746974792F30AE4A7562652E44746F2E5468696E6793A4504F5322001C3122000C43001C3221000D43001C3322000C43001C3421000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D43000DA8000C43000DA7000D430007A800505468696E67'

function bytesOf(hex: string): ArrayBuffer {
  return Uint8Array.from(hex.match(/../g)!.map((pair) => parseInt(pair, 16))).buffer
}

const schema = {
  hash: 'z',
  types: {
    'Jube.Dto.Query.WireSchema.WireRouteDto': {
      fields: [
        {index: 0, name: 'method', type: 'string'},
        {index: 1, name: 'template', type: 'string'},
        {index: 2, name: 'shape', type: 'string'},
      ],
    },
  },
  routes: [],
}

const shape = 'Jube.Dto.Query.WireSchema.WireRouteDto[]'

describe('wire LZ4 compression', () => {
  beforeEach(() => {
    resetWire()
    setWireSchema(schema)
  })

  it('inflates the block the server serializer writes back to its original bytes', () => {
    const inflated = inflateLz4(bytesOf(serverCompressed))

    expect(inflated.length).toBe(1343)
  })

  it('decodes a compressed body written by the server into the JSON-shaped rows', () => {
    const rows = decodeWire(bytesOf(serverCompressed), shape, 'lz4') as Record<string, string>[]

    expect(rows).toHaveLength(40)
    expect(rows[0]).toEqual({method: 'GET', template: 'api/Entity/0', shape: 'Jube.Dto.Thing'})
    expect(rows[39]).toEqual({method: 'POST', template: 'api/Entity/4', shape: 'Jube.Dto.Thing'})
  })

  it('refuses a compression name it does not know', () => {
    expect(() => decodeWire(bytesOf(serverCompressed), shape, 'zstd')).toThrow(
      'Unknown wire compression',
    )
  })

  it('refuses a body that is not an LZ4 extension, one cut short and one declaring an absurd size', () => {
    expect(() => inflateLz4(new Uint8Array([0x91, 0x01]).buffer)).toThrow()
    expect(() => inflateLz4(bytesOf(serverCompressed.slice(0, 120)))).toThrow()

    const huge = new Uint8Array(bytesOf(serverCompressed))
    huge.set([0x7f, 0xff, 0xff, 0xff], 5)

    expect(() => inflateLz4(huge.buffer)).toThrow('unreasonable size')
  })

  it('refuses a body whose block does not inflate to the size it declares', () => {
    const lying = new Uint8Array(bytesOf(serverCompressed))
    lying.set([0x00, 0x00, 0x05, 0x40], 5)

    expect(() => inflateLz4(lying.buffer)).toThrow()
  })
})

describe('useApi with LZ4 compressed responses', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    resetWire()
    setApiHandlers({})
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('decodes a response that names its compression in the header', async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(JSON.stringify(schema), {status: 200}))
      .mockResolvedValueOnce(
        new Response(bytesOf(serverCompressed), {
          status: 200,
          headers: {
            'Content-Type': messagePackType,
            [wireShapeHeader]: shape,
            [wireSchemaHeader]: 'z',
            [wireCompressionHeader]: 'lz4',
          },
        }),
      )

    const rows = await useApi<unknown[]>('/api/Routes')

    expect(rows).toHaveLength(40)
  })
})
