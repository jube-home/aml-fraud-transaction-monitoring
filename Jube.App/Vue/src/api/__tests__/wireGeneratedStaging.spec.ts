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
import {beforeEach, describe, expect, it, vi} from 'vitest'
import {decodeWire, generatedDecodersInUse, loadWireSchema, resetWire} from '../wire'

vi.mock('../../generated/wire.generated', async (importOriginal) => {
  await new Promise((resolve) => setTimeout(resolve, 150))

  return await importOriginal()
})

describe('staging the generated decoders', () => {
  beforeEach(() => {
    resetWire()
  })

  it('does not hold the first call back, decodes with the schema-built decoders meanwhile, then switches', async () => {
    const original = await vi.importActual<typeof import('../../generated/wire.generated')>(
      '../../generated/wire.generated',
    )

    const schema = {
      hash: 's',
      typesHash: original.typesHash,
      types: {'Jube.Thing': {fields: [{index: 0, name: 'label', type: 'string'}]}},
      routes: [],
    }

    vi.stubGlobal(
      'fetch',
      vi
        .fn<typeof fetch>()
        .mockResolvedValue(new Response(JSON.stringify(schema), {status: 200})),
    )

    const started = performance.now()

    await loadWireSchema()

    expect(performance.now() - started).toBeLessThan(120)
    expect(generatedDecodersInUse()).toBe(false)

    const packed = encode(['hello'])
    const buffer = packed.buffer.slice(
      packed.byteOffset,
      packed.byteOffset + packed.byteLength,
    ) as ArrayBuffer

    expect(decodeWire(buffer, 'Jube.Thing')).toEqual({label: 'hello'})

    await vi.waitFor(() => expect(generatedDecodersInUse()).toBe(true), {timeout: 2000})

    expect(decodeWire(buffer, 'Jube.Thing')).toEqual({label: 'hello'})

    vi.unstubAllGlobals()
  })
})
