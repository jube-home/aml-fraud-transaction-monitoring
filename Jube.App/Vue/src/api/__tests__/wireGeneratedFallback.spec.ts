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

import {describe, expect, it, vi} from 'vitest'
import {adoptGeneratedDecoders, decodeWire, resetWire, setWireSchema} from '../wire'
import {encode} from '@msgpack/msgpack'

vi.mock('../../generated/wire.generated', () => {
  throw new Error('The generated decoders could not be downloaded.')
})

describe('when the generated decoders cannot be downloaded', () => {
  it('falls back to the decoders built from the schema instead of failing', async () => {
    resetWire()

    const schema = {
      hash: 'f',
      typesHash: 'whatever',
      types: {'Jube.Thing': {fields: [{index: 0, name: 'label', type: 'string'}]}},
      routes: [],
    }

    setWireSchema(schema)

    await expect(adoptGeneratedDecoders(schema)).resolves.toBe(false)

    const packed = encode(['hello'])
    const buffer = packed.buffer.slice(
      packed.byteOffset,
      packed.byteOffset + packed.byteLength,
    ) as ArrayBuffer

    expect(decodeWire(buffer, 'Jube.Thing')).toEqual({label: 'hello'})
  })
})
