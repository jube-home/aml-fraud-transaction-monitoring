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
import {beforeEach, describe, expect, it} from 'vitest'
import {decoders, typesHash} from '../../generated/wire.generated'
import typesFile from '../../generated/wireTypes.generated.json'
import {
  adoptGeneratedDecoders,
  decodeWire,
  resetWire,
  setWireSchema,
  type WireSchema,
} from '../wire'

interface Field {
  index: number
  name: string
  type: string
}

const types = typesFile.types as unknown as Record<string, { fields: Field[] }>
const primitives = new Set([
  'string',
  'char',
  'int',
  'double',
  'bool',
  'guid',
  'date',
  'any',
  'bytes',
])
const runtimeSchema: WireSchema = {hash: 'g', types, routes: []}
const generics = Object.keys(types).filter((name) => name.includes('`'))

function baseOf(shape: string): string {
  let base = shape

  for (; ;) {
    if (base.endsWith('{}#')) base = base.slice(0, -3)
    else if (base.endsWith('[]') || base.endsWith('{}')) base = base.slice(0, -2)
    else if (base.endsWith('?')) base = base.slice(0, -1)
    else return base
  }
}

function bytes(value: unknown): ArrayBuffer {
  const packed = encode(value)

  return packed.buffer.slice(
    packed.byteOffset,
    packed.byteOffset + packed.byteLength,
  ) as ArrayBuffer
}

function sample(shape: string, seed: number): unknown {
  const base = baseOf(shape)

  if (shape.endsWith('{}#')) return [1, ['a'], [[seed]], null, null]
  if (shape.endsWith('[]')) return [sample(shape.slice(0, -2), seed), null]
  if (shape.endsWith('{}')) return {k: sample(shape.slice(0, -2), seed)}

  const described = types[base]

  if (described === undefined) {
    switch (base) {
      case 'int':
        return seed
      case 'double':
        return seed + 0.5
      case 'bool':
        return seed % 2 === 0
      case 'date':
        return new Date(1_700_000_000_000 + seed)
      case 'guid':
        return '3f2504e0-4f89-11d3-9a0c-0305e82c3301'
      case 'bytes':
        return new Uint8Array([seed, 1, 2])
      default:
        return `v${seed}`
    }
  }

  const result: unknown[] = []

  for (const field of described.fields) {
    result[field.index] = seed > 3 ? null : sample(field.type, seed + 1)
  }

  return Array.from(result, (item) => item ?? null)
}

describe('generated decoders for closed generic response types', () => {
  beforeEach(() => {
    resetWire()
  })

  it('include the closed generic types the server answers with', () => {
    expect(generics.length).toBeGreaterThanOrEqual(45)

    for (const name of generics) expect(decoders[name], name).toBeTypeOf('function')
  })

  it('leave no member of any generated type on the fallback path', () => {
    const uncovered: string[] = []

    for (const [name, type] of Object.entries(types)) {
      if (decoders[name] === undefined) uncovered.push(name)

      for (const field of type.fields) {
        const base = baseOf(field.type)

        if (!primitives.has(base) && decoders[base] === undefined)
          uncovered.push(`${name}.${field.name}`)
      }
    }

    expect(uncovered).toEqual([])
  })

  it('decode lists, maps and single values of them as the runtime fallback does', async () => {
    const shapes = generics.flatMap((name) => [name, `${name}[]`, `${name}{}`, `${name}?`])
    const samples = shapes.map((shape) => [shape, bytes(sample(shape, 1))] as const)

    setWireSchema(runtimeSchema)
    const viaRuntime = samples.map(([shape, buffer]) => decodeWire(buffer, shape))

    const generated: WireSchema = {...runtimeSchema, typesHash}
    setWireSchema(generated)
    await expect(adoptGeneratedDecoders(generated)).resolves.toBe(true)

    const viaGenerated = samples.map(([shape, buffer]) => decodeWire(buffer, shape))

    expect(viaGenerated).toEqual(viaRuntime)
    expect(viaGenerated.some((value) => value !== null)).toBe(true)
  })
})
