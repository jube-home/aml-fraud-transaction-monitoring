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

const gate = vi.hoisted(() => {
  let release: () => void = () => undefined
  const opened = new Promise<void>((resolve) => {
    release = resolve
  })

  return {opened, release: () => release()}
})

vi.mock('../../generated/wire.generated', async (importOriginal) => {
  await gate.opened

  return await importOriginal()
})

const generatedType = 'Jube.App.Endpoints.ExhaustiveSearchInstanceSetActiveRequest'

describe('adopting the generated decoders once they arrive', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    resetWire()
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('decodes identically before and after, and then needs no further shape requests', async () => {
    const original = await vi.importActual<typeof import('../../generated/wire.generated')>(
      '../../generated/wire.generated',
    )

    const head = {
      hash: 'g1',
      mode: 'MessagePack',
      typesHash: original.typesHash,
      generatedDecoders: true,
      types: {},
      routes: [],
    }

    const shapes = {
      hash: 'g1',
      types: {[generatedType]: {fields: [{index: 0, name: 'active', type: 'bool'}]}},
      routes: [],
    }

    fetchMock.mockImplementation((input) =>
      Promise.resolve(
        new Response(JSON.stringify(String(input).includes('/Shapes') ? shapes : head), {
          status: 200,
        }),
      ),
    )

    const packed = encode([true])
    const buffer = packed.buffer.slice(
      packed.byteOffset,
      packed.byteOffset + packed.byteLength,
    ) as ArrayBuffer

    vi.useFakeTimers({toFake: ['setTimeout']})

    const headed = loadWireHead()
    const shaped = ensureWireShape(generatedType, 'decode', 'g1')

    await vi.advanceTimersByTimeAsync(10)
    await Promise.all([headed, shaped])

    expect(generatedDecodersInUse()).toBe(false)

    const before = decodeWire(buffer, generatedType)

    expect(before).toEqual({active: true})

    expect(generatedDecodersInUse()).toBe(false)

    gate.release()
    await vi.advanceTimersByTimeAsync(5000)
    await vi.waitFor(() => expect(generatedDecodersInUse()).toBe(true))

    expect(decodeWire(buffer, generatedType)).toEqual(before)

    const calls = fetchMock.mock.calls.length

    await ensureWireShape(`${generatedType}[]`, 'decode', 'g1')

    expect(fetchMock).toHaveBeenCalledTimes(calls)
  })
})
