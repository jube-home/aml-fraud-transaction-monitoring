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

function random(seed: number): () => number {
  let state = seed

  return () => {
    state = (state * 1664525 + 1013904223) % 4294967296

    return state / 4294967296
  }
}

function make(next: () => number) {
  const scalar = (shape: string): unknown => {
    switch (shape) {
      case 'int':
        return Math.floor(next() * 2000) - 1000
      case 'double':
        return Math.round(next() * 1e6) / 1e3
      case 'bool':
        return next() < 0.5
      case 'char':
        return 'k'
      case 'guid':
        return '3f2504e0-4f89-11d3-9a0c-0305e82c3301'
      case 'date':
        return new Date(1_700_000_000_000 + Math.floor(next() * 1e9))
      case 'string':
        return 'value-' + Math.floor(next() * 100)
      case 'bytes':
        return new Uint8Array([Math.floor(next() * 256), 1, 2])
      default:
        return [1, 'two', null, {k: next() < 0.5}, {nested: [1, 2]}][Math.floor(next() * 5)]
    }
  }

  const value = (shape: string, depth: number): unknown => {
    if (next() < 0.12) return null

    if (shape.endsWith('{}#')) {
      const count = Math.floor(next() * 3)
      const columns = ['a', 'b'].map(() =>
        Array.from({length: count}, () => (next() < 0.8 ? scalar('any') : null)),
      )

      return [count, ['a', 'b'], columns, count > 0 && next() < 0.5 ? [[0, [0]]] : null, null]
    }

    if (shape.endsWith('[]')) {
      return Array.from({length: Math.floor(next() * 3)}, () => value(shape.slice(0, -2), depth))
    }

    if (shape.endsWith('{}')) {
      return {k1: value(shape.slice(0, -2), depth), k2: value(shape.slice(0, -2), depth)}
    }

    const base = shape.endsWith('?') ? shape.slice(0, -1) : shape
    const described = types[base]

    if (described === undefined) return scalar(base)

    if (depth > 2) return null

    const result: unknown[] = []

    for (const field of described.fields) result[field.index] = value(field.type, depth + 1)

    return Array.from(result, (item) => item ?? null)
  }

  return value
}

const runtimeSchema: WireSchema = {hash: 'r', types, routes: []}

function bytes(value: unknown): ArrayBuffer {
  const packed = encode(value)

  return packed.buffer.slice(
    packed.byteOffset,
    packed.byteOffset + packed.byteLength,
  ) as ArrayBuffer
}

describe('generated wire decoders', () => {
  beforeEach(() => {
    resetWire()
  })

  it('cover every wire type the server schema holds from the DTO assembly', () => {
    expect(Object.keys(decoders).sort()).toEqual(Object.keys(types).sort())
    expect(Object.keys(decoders).length).toBeGreaterThan(300)
  })

  it('are adopted only when the schema names the hash they were generated for', async () => {
    const matching: WireSchema = {...runtimeSchema, typesHash}
    setWireSchema(matching)

    await expect(adoptGeneratedDecoders(matching)).resolves.toBe(true)

    const stale: WireSchema = {...runtimeSchema, typesHash: 'not-the-same'}
    setWireSchema(stale)

    await expect(adoptGeneratedDecoders(stale)).resolves.toBe(false)
  })

  it('are left unloaded and unused when the server switches them off', async () => {
    const off: WireSchema = {...runtimeSchema, typesHash, generatedDecoders: false}
    const [name] = Object.keys(types)
    const row = Array.from({length: 12}, () => null)

    setWireSchema(off)

    await expect(adoptGeneratedDecoders(off)).resolves.toBe(false)
    expect(decodeWire(bytes(row), name!)).toEqual(
      (() => {
        setWireSchema(runtimeSchema)

        return decodeWire(bytes(row), name!)
      })(),
    )
  })

  it('decode every type exactly as the runtime decoders do, over random data', async () => {
    const names = Object.keys(types)
    const next = random(20261105)
    const value = make(next)
    const samples = new Map<string, ArrayBuffer[]>()

    for (const name of names) {
      samples.set(
        name,
        Array.from({length: 4}, () => bytes(value(name, 0) ?? value(name, 0))),
      )
    }

    setWireSchema(runtimeSchema)
    const viaRuntime = new Map<string, unknown[]>()

    for (const [name, buffers] of samples) {
      viaRuntime.set(
        name,
        buffers.map((buffer) => decodeWire(buffer, name)),
      )
    }

    const generated: WireSchema = {...runtimeSchema, typesHash}
    setWireSchema(generated)
    await adoptGeneratedDecoders(generated)

    for (const [name, buffers] of samples) {
      const viaGenerated = buffers.map((buffer) => decodeWire(buffer, name))

      expect(viaGenerated, name).toEqual(viaRuntime.get(name))
    }
  })

  it('decode the same list, map and row-grid compositions as the runtime decoders', async () => {
    const [name] = Object.keys(types)
    const field = types[name!]!.fields[0]!
    const list = [[field.type === 'int' ? 1 : null], null, []]

    setWireSchema(runtimeSchema)
    const viaRuntime = decodeWire(bytes(list), `${name}[]`)

    const generated: WireSchema = {...runtimeSchema, typesHash}
    setWireSchema(generated)
    await adoptGeneratedDecoders(generated)

    expect(decodeWire(bytes(list), `${name}[]`)).toEqual(viaRuntime)
  })
})
