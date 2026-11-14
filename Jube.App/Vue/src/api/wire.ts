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

import {decode, decodeTimestampExtension, encode, ExtensionCodec} from '@msgpack/msgpack'
import {inflateLz4, lz4Name} from './wireCompression'
import {buildRows, setOwn, type Convert} from './wireRuntime'

export const messagePackType = 'application/x-msgpack'
export const columnarJsonType = 'application/vnd.jube.columnar+json'
export const wireShapeHeader = 'X-Wire-Shape'
export const wireSchemaHeader = 'X-Wire-Schema'

const schemaPath = '/api/WireSchema'
const headPath = '/api/WireSchema/Head'
const shapesPath = '/api/WireSchema/Shapes'
const generatedIdleTimeout = 3000
const defaultGeneratedStagingDelay = 0
const shapeBatchWindow = 4
const maximumShapesPerRequest = 64

function configuredStagingDelay(): number {
  const raw = (import.meta.env as Record<string, string | undefined>)
    .VITE_JUBE_WIRE_STAGING_DELAY_MS
  const parsed = raw === undefined || raw.trim() === '' ? Number.NaN : Number(raw)

  return Number.isFinite(parsed) && parsed >= 0 ? parsed : defaultGeneratedStagingDelay
}

const generatedStagingDelay = configuredStagingDelay()
const timestampExtension = -1
const defaultDate = new Date('0001-01-01T00:00:00.000Z')

export interface WireField {
  index: number
  name: string
  type: string
}

export interface WireType {
  fields: WireField[]
}

export interface WireRoute {
  method: string
  template: string
  shape: string
}

export interface WireSchema {
  hash: string
  mode?: string
  typesHash?: string
  generatedDecoders?: boolean
  encryption?: boolean
  encryptionPublicKey?: string
  types: Record<string, WireType>
  routes: WireRoute[]
}

interface CompiledRoute {
  method: string
  pattern: RegExp
  literals: number
  shape: string
}

interface ShapeBatch {
  shapes: Set<string>
  waiters: { resolve: () => void; reject: (error: unknown) => void }[]
}

export class WireSchemaError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Wire schema unavailable (HTTP ${status}).`)
    this.name = 'WireSchemaError'
    this.status = status
  }
}

const codec = new ExtensionCodec()

codec.register({
  type: timestampExtension,
  encode: () => null,
  decode: (data: Uint8Array) => {
    return decodeTimestampExtension(data).toISOString()
  },
})

let schema: WireSchema | undefined
let loading: Promise<WireSchema> | undefined
let headLoading: Promise<WireSchema> | undefined
let complete = false
const shapeLoading = new Map<string, Promise<void>>()
let known: Record<string, WireType> = {}
let knownHash: string | undefined
const shapeBatches = new Map<string, ShapeBatch>()
const readyShapes = new Set<string>()
let routes: CompiledRoute[] = []
let generatedDecoders: Record<string, (value: unknown) => unknown> | undefined
const walkers = new Map<string, Convert>()
let decoders = new Map<string, Convert>()
let encoders = new Map<string, Convert>()

export function resetWire(): void {
  schema = undefined
  loading = undefined
  headLoading = undefined
  complete = false
  shapeLoading.clear()
  shapeBatches.clear()
  known = {}
  knownHash = undefined
  readyShapes.clear()
  routes = []
  decoders = new Map()
  encoders = new Map()
  walkers.clear()
  generatedDecoders = undefined
}

export function setWireSchema(next: WireSchema): void {
  applySchema(next, true)
}

function applySchema(next: WireSchema, whole: boolean): void {
  schema = next
  complete = whole
  shapeLoading.clear()

  if (knownHash !== next.hash) {
    known = {}
    knownHash = undefined
  }

  readyShapes.clear()
  decoders = new Map()
  encoders = new Map()
  walkers.clear()
  routes = next.routes
    .map((route) => compileRoute(route))
    .sort((left, right) => right.literals - left.literals)
}

let generatedModule: Promise<typeof import('../generated/wire.generated')> | undefined

function stageGeneratedDecoders(): Promise<typeof import('../generated/wire.generated')> {
  generatedModule ??= import('../generated/wire.generated')

  return generatedModule
}

export async function adoptGeneratedDecoders(next: WireSchema): Promise<boolean> {
  if (next.generatedDecoders === false) {
    generatedDecoders = undefined
    decoders = new Map()

    return false
  }

  let generated: Awaited<ReturnType<typeof stageGeneratedDecoders>>

  try {
    generated = await stageGeneratedDecoders()
  } catch {
    generatedModule = undefined
    generatedDecoders = undefined
    decoders = new Map()

    return false
  }

  if (schema !== next) return false

  generatedDecoders = next.typesHash === generated.typesHash ? generated.decoders : undefined
  decoders = new Map()

  return generatedDecoders !== undefined
}

function compileRoute(route: WireRoute): CompiledRoute {
  const segments = route.template.split('/').filter((segment) => segment.length > 0)
  let literals = 0

  const pattern = segments
    .map((segment) => {
      if (!segment.startsWith('{')) {
        literals += 1

        return escapePattern(segment)
      }

      return segment.startsWith('{*') ? '.+' : '[^/]+'
    })
    .join('/')

  return {
    method: route.method,
    pattern: new RegExp('^/?' + pattern + '/?$', 'i'),
    literals,
    shape: route.shape,
  }
}

function escapePattern(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

export async function loadWireSchema(hash?: string): Promise<WireSchema> {
  if (schema && (hash === undefined || schema.hash === hash)) return schema

  if (loading === undefined) stageGeneratedDecoders().catch(() => undefined)

  loading ??= fetch(hash === undefined ? schemaPath : `${schemaPath}?hash=${hash}`, {
    headers: {Accept: 'application/json'},
    credentials: 'same-origin',
    cache: hash === undefined ? 'no-cache' : 'default',
  })
    .then(async (response) => {
      if (!response.ok) throw new WireSchemaError(response.status)

      const next = (await response.json()) as WireSchema

      setWireSchema(next)
      void adoptGeneratedDecoders(next)

      return next
    })
    .finally(() => {
      loading = undefined
    })

  return loading
}

function scheduleGeneratedStaging(next: WireSchema): void {
  if (generatedStagingDelay === 0) {
    void adoptGeneratedDecoders(next)

    return
  }

  const stage = () => {
    if (schema !== next) return

    const idle = (
      globalThis as {
        requestIdleCallback?: (callback: () => void, options?: { timeout: number }) => number
      }
    ).requestIdleCallback

    const adopt = () => {
      if (schema === next) void adoptGeneratedDecoders(next)
    }

    if (typeof idle === 'function') idle(adopt, {timeout: generatedIdleTimeout})
    else adopt()
  }

  setTimeout(stage, generatedStagingDelay)
}

export async function loadWireHead(hash?: string): Promise<WireSchema> {
  if (schema && (hash === undefined || schema.hash === hash)) return schema

  if (headLoading === undefined) stageGeneratedDecoders().catch(() => undefined)

  headLoading ??= fetch(headPath, {
    headers: {Accept: 'application/json'},
    credentials: 'same-origin',
    cache: 'no-cache',
  })
    .then(async (response) => {
      if (!response.ok) throw new WireSchemaError(response.status)

      const next = (await response.json()) as WireSchema

      next.types ??= {}
      applySchema(next, false)
      scheduleGeneratedStaging(next)

      return next
    })
    .finally(() => {
      headLoading = undefined
    })

  return headLoading
}

export type WireShapePurpose = 'decode' | 'walk' | 'encode'

function shapeBase(shape: string): string {
  let base = shape

  for (; ;) {
    if (base.endsWith(columnarSuffix)) base = base.slice(0, -columnarSuffix.length)
    else if (base.endsWith('[]') || base.endsWith('{}')) base = base.slice(0, -2)
    else if (base.endsWith('?')) base = base.slice(0, -1)
    else return base
  }
}

function typesAvailable(shape: string, purpose: WireShapePurpose, visited: Set<string>): boolean {
  const base = shapeBase(shape)

  if (primitiveTags.has(base) || visited.has(base)) return true

  visited.add(base)

  if (purpose === 'decode' && generatedDecoders?.[base] !== undefined) return true

  const described = schema?.types[base] ?? known[base]

  if (described === undefined) return false

  return described.fields.every((field) => typesAvailable(field.type, purpose, visited))
}

async function fetchShapes(shapes: string[], hash: string): Promise<WireSchema> {
  const query = new URLSearchParams()

  for (const shape of shapes) query.append('shape', shape)

  query.set('hash', hash)

  const response = await fetch(`${shapesPath}?${query.toString()}`, {
    headers: {Accept: 'application/json'},
    credentials: 'same-origin',
    cache: 'default',
  })

  if (!response.ok) throw new WireSchemaError(response.status)

  return (await response.json()) as WireSchema
}

function absorbShapes(next: WireSchema): void {
  if (knownHash !== next.hash) {
    known = {}
    knownHash = next.hash
    decoders = new Map()
    encoders = new Map()
    walkers.clear()
    readyShapes.clear()
  }

  Object.assign(known, next.types)
}

function flushShapes(hash: string): void {
  const batch = shapeBatches.get(hash)

  if (batch === undefined) return

  shapeBatches.delete(hash)

  const wanted = [...batch.shapes]
  const groups: string[][] = []

  for (let start = 0; start < wanted.length; start += maximumShapesPerRequest)
    groups.push(wanted.slice(start, start + maximumShapesPerRequest))

  Promise.all(groups.map((group) => fetchShapes(group, hash)))
    .then((results) => {
      for (const next of results) absorbShapes(next)

      for (const waiter of batch.waiters) waiter.resolve()
    })
    .catch((error: unknown) => {
      for (const waiter of batch.waiters) waiter.reject(error)
    })
}

function queueShape(shape: string, hash: string): Promise<void> {
  const key = `${hash}|${shape}`
  const pending = shapeLoading.get(key)

  if (pending !== undefined) return pending

  const created = new Promise<void>((resolve, reject) => {
    let batch = shapeBatches.get(hash)

    if (batch === undefined) {
      batch = {shapes: new Set(), waiters: []}
      shapeBatches.set(hash, batch)
      setTimeout(() => flushShapes(hash), shapeBatchWindow)
    }

    batch.shapes.add(shape)
    batch.waiters.push({resolve, reject})
  }).finally(() => {
    shapeLoading.delete(key)
  })

  shapeLoading.set(key, created)

  return created
}

export async function ensureWireShape(
  shape: string,
  purpose: WireShapePurpose,
  hash?: string,
): Promise<void> {
  if (hash === undefined || (schema !== undefined && schema.hash !== hash)) await loadWireHead(hash)

  if (complete) return

  const target = hash ?? schema?.hash

  if (target === undefined) return

  const key = `${purpose}|${shape}`

  if (readyShapes.has(key) || typesAvailable(shape, purpose, new Set())) {
    readyShapes.add(key)

    return
  }

  await queueShape(shape, target)

  if (typesAvailable(shape, purpose, new Set())) readyShapes.add(key)
}

export function generatedDecodersInUse(): boolean {
  return generatedDecoders !== undefined
}

export function wireEncryptionKey(): string | undefined {
  return schema?.encryption === true && schema.encryptionPublicKey
    ? schema.encryptionPublicKey
    : undefined
}

export function requestShape(method: string, path: string): string | undefined {
  if (schema?.mode !== undefined && schema.mode !== 'MessagePack') return undefined

  const pathOnly = path.split('?')[0] ?? path
  const upper = method.toUpperCase()

  return routes.find((route) => route.method === upper && route.pattern.test(pathOnly))?.shape
}

const columnarSuffix = '{}#'

function splitShape(shape: string): {
  base: string
  many: boolean
  map: boolean
  nullable: boolean
} {
  let base = shape
  let many = false
  let map = false

  if (base.endsWith('[]')) {
    many = true
    base = base.slice(0, -2)
  } else if (base.endsWith('{}')) {
    map = true
    base = base.slice(0, -2)
  }

  const nullable = base.endsWith('?')

  if (nullable) base = base.slice(0, -1)

  return {base, many, map, nullable}
}

const primitiveTags = new Set([
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

function decoderFor(shape: string): Convert {
  if (decoders.has(shape)) return decoders.get(shape) ?? null

  const {base, many, map} = splitShape(shape)
  let compiled: Convert

  if (shape.endsWith(columnarSuffix)) {
    const inner = decoderFor(shape.slice(0, -columnarSuffix.length))

    compiled = (value) => {
      if (value == null) return value

      const [rowCount, keys, columns, sparse, nullRows] = value as [
        number,
        string[],
        unknown[][],
          [number, number[]][] | null,
          number[] | null,
      ]

      return buildRows(rowCount, keys, columns, sparse, nullRows, inner)
    }
  } else if (many || map) {
    const inner = decoderFor(shape.slice(0, -2))

    if (inner === null) {
      compiled = null
    } else if (many) {
      compiled = (value) => (value == null ? value : (value as unknown[]).map(inner))
    } else {
      compiled = (value) => {
        if (value == null) return value

        const result: Record<string, unknown> = {}

        for (const [key, item] of Object.entries(value as Record<string, unknown>)) {
          setOwn(result, key, inner(item))
        }

        return result
      }
    }
  } else if (primitiveTags.has(base)) {
    compiled = null
  } else if (generatedDecoders?.[base] !== undefined) {
    compiled = generatedDecoders[base]!
  } else {
    let fields: { name: string; index: number; convert: Convert }[] | undefined

    compiled = (value) => {
      if (value == null) return value

      fields ??= fieldsOf(base).map((field) => ({
        name: field.name,
        index: field.index,
        convert: decoderFor(field.type),
      }))

      const source = value as unknown[]
      const result: Record<string, unknown> = {}

      for (const field of fields) {
        const item = source[field.index] ?? null

        result[field.name] = field.convert === null ? item : field.convert(item)
      }

      return result
    }
  }

  decoders.set(shape, compiled)

  return compiled
}

interface ColumnarJsonRows {
  count: number
  keys: string[]
  columns: unknown[][]
  missing?: [number, number[]][] | null
  nulls?: number[] | null
}

function walkerFor(shape: string, visiting: Set<string> = new Set()): Convert {
  if (walkers.has(shape)) return walkers.get(shape) ?? null

  const {base, many, map} = splitShape(shape)
  let compiled: Convert = null

  if (shape.endsWith(columnarSuffix)) {
    compiled = (value) => {
      if (value == null) return value

      const rows = value as ColumnarJsonRows

      return buildRows(rows.count, rows.keys, rows.columns, rows.missing, rows.nulls, null)
    }
  } else if (many || map) {
    const inner = walkerFor(shape.slice(0, -2), visiting)

    if (inner === null) {
      compiled = null
    } else if (many) {
      compiled = (value) => {
        if (value == null) return value

        const list = value as unknown[]

        for (let index = 0; index < list.length; index += 1) list[index] = inner(list[index])

        return list
      }
    } else {
      compiled = (value) => {
        if (value == null) return value

        const record = value as Record<string, unknown>

        for (const key of Object.keys(record)) record[key] = inner(record[key])

        return record
      }
    }
  } else if (!primitiveTags.has(base) && !visiting.has(base)) {
    visiting.add(base)

    const fields = fieldsOf(base)
      .map((field) => ({name: field.name, convert: walkerFor(field.type, visiting)}))
      .filter((field) => field.convert !== null)

    visiting.delete(base)

    if (fields.length > 0) {
      compiled = (value) => {
        if (value == null) return value

        const record = value as Record<string, unknown>

        for (const field of fields) {
          if (field.name in record) record[field.name] = field.convert!(record[field.name])
        }

        return record
      }
    }
  }

  walkers.set(shape, compiled)

  return compiled
}

export function decodeColumnarJson(value: unknown, shape: string): unknown {
  const convert = walkerFor(shape)

  return convert === null ? value : convert(value)
}

function defaultFor(base: string): unknown {
  switch (base) {
    case 'int':
    case 'double':
      return 0
    case 'bool':
      return false
    case 'char':
      return '\u0000'
    case 'guid':
      return '00000000-0000-0000-0000-000000000000'
    case 'date':
      return defaultDate
    default:
      return null
  }
}

function encoderFor(shape: string): Convert {
  if (encoders.has(shape)) return encoders.get(shape) ?? null

  const {base, many, map, nullable} = splitShape(shape)
  let compiled: Convert

  if (shape.endsWith(columnarSuffix)) {
    const inner = encoderFor(shape.slice(0, -columnarSuffix.length))

    compiled = (value) => {
      if (value == null) return value

      const rows = value as (Record<string, unknown> | null)[]
      const keys: string[] = []
      const seen = new Set<string>()
      const nullRows: number[] = []

      rows.forEach((row, index) => {
        if (row == null) {
          nullRows.push(index)
          return
        }

        for (const key of Object.keys(row)) {
          if (!seen.has(key)) {
            seen.add(key)
            keys.push(key)
          }
        }
      })

      const sparse: [number, number[]][] = []
      let anySparse = false

      const columns = keys.map((key, column) =>
        rows.map((row, index) => {
          if (row == null) return null

          if (!Object.prototype.hasOwnProperty.call(row, key)) {
            const last = sparse[sparse.length - 1]

            if (last !== undefined && last[0] === column) last[1].push(index)
            else sparse.push([column, [index]])

            anySparse = true

            return null
          }

          const cell = row[key]

          return inner === null ? (cell ?? null) : inner(cell)
        }),
      )

      return [
        rows.length,
        keys,
        columns,
        anySparse ? sparse : null,
        nullRows.length > 0 ? nullRows : null,
      ]
    }
  } else if (many || map) {
    const inner = encoderFor(shape.slice(0, -2))

    if (inner === null) {
      compiled = null
    } else if (many) {
      compiled = (value) => (value == null ? value : (value as unknown[]).map(inner))
    } else {
      compiled = (value) => {
        if (value == null) return value

        const result: Record<string, unknown> = {}

        for (const [key, item] of Object.entries(value as Record<string, unknown>)) {
          setOwn(result, key, inner(item))
        }

        return result
      }
    }
  } else if (primitiveTags.has(base)) {
    const fallback = nullable ? null : defaultFor(base)

    compiled = (value) => {
      if (value === undefined || value === null) return fallback

      return base === 'date' && typeof value === 'string' ? new Date(value) : value
    }
  } else {
    let fields: { name: string; index: number; convert: Convert }[] | undefined
    let length = 0

    compiled = (value) => {
      if (value == null) return null

      if (fields === undefined) {
        const described = fieldsOf(base)

        fields = described.map((field) => ({
          name: field.name,
          index: field.index,
          convert: encoderFor(field.type),
        }))
        length = described.reduce((highest, field) => Math.max(highest, field.index + 1), 0)
      }

      const source = value as Record<string, unknown>
      const result: unknown[] = new Array<unknown>(length).fill(null)

      for (const field of fields) {
        const item = source[field.name]

        result[field.index] = field.convert === null ? (item ?? null) : field.convert(item)
      }

      return result
    }
  }

  encoders.set(shape, compiled)

  return compiled
}

function fieldsOf(type: string): WireField[] {
  const described = schema?.types[type] ?? known[type]

  if (described === undefined) throw new Error(`Wire type ${type} is not in the loaded schema.`)

  return described.fields
}

export function decodeWire(
  buffer: ArrayBuffer,
  shape: string,
  compression?: string | null,
): unknown {
  if (compression !== undefined && compression !== null && compression !== lz4Name)
    throw new Error(`Unknown wire compression ${compression}.`)

  const body = compression === lz4Name ? inflateLz4(buffer) : buffer
  const value = decode(body, {extensionCodec: codec})
  const convert = decoderFor(shape)

  return convert === null ? value : convert(value)
}

export function encodeWire(shape: string, body: unknown): ArrayBuffer {
  const convert = encoderFor(shape)
  const packed = encode(convert === null ? body : convert(body))

  return packed.buffer.slice(
    packed.byteOffset,
    packed.byteOffset + packed.byteLength,
  ) as ArrayBuffer
}
