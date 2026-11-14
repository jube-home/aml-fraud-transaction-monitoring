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

import {noteSessionExpiry} from '../auth/sessionExpiry'
import {apiLanguage} from './language'
import {
  columnarJsonType,
  decodeColumnarJson,
  decodeWire,
  encodeWire,
  ensureWireShape,
  loadWireHead,
  messagePackType,
  requestShape,
  WireSchemaError,
  wireEncryptionKey,
  wireSchemaHeader,
  wireShapeHeader,
} from './wire'
import {wireCompressionHeader} from './wireCompression'
import {encryptedType, sealRequest, type SealedRequest} from './wireCrypto'

export type FieldErrors = Record<string, string[]>

export class ApiError extends Error {
  readonly status: number
  readonly body: unknown
  readonly fieldErrors: FieldErrors

  constructor(status: number, body: unknown) {
    super(`HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.body = body
    this.fieldErrors = status === 400 || status === 422 ? mapFieldErrors(body) : {}
  }

  get kind():
    'unauthorized' | 'forbidden' | 'notFound' | 'conflict' | 'validation' | 'server' | 'network' {
    if (this.status === 0) return 'network'
    if (this.status === 401) return 'unauthorized'
    if (this.status === 403) return 'forbidden'
    if (this.status === 404) return 'notFound'
    if (this.status === 409 || this.status === 412) return 'conflict'
    if (this.status === 400 || this.status === 422) return 'validation'
    return 'server'
  }
}

export interface ApiHandlers {
  onUnauthorized?: () => void
}

export interface ApiOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  query?: Record<string, string | number | boolean | undefined>
  body?: unknown
  formData?: FormData
  signal?: AbortSignal
  anonymous?: boolean
  accept?: string
}

export interface ApiResult<T> {
  status: number
  body: T
}

const antiforgeryHeaderName = 'X-XSRF-TOKEN'
const sessionExpiryHeaderName = 'X-Session-Expiry'
const antiforgeryPath = '/api/AntiforgeryToken'

let handlers: ApiHandlers = {}
let antiforgeryToken: Promise<string> | undefined

export function setApiHandlers(next: ApiHandlers): void {
  handlers = next
}

export function resetAntiforgeryToken(): void {
  antiforgeryToken = undefined
}

function mapFieldErrors(body: unknown): FieldErrors {
  const result: FieldErrors = {}

  if (typeof body !== 'object' || body === null) return result

  const record = body as Record<string, unknown>

  if (Array.isArray(record.errors)) {
    for (const entry of record.errors) {
      if (typeof entry !== 'object' || entry === null) continue
      const {propertyName, errorMessage} = entry as Record<string, unknown>
      if (typeof propertyName !== 'string' || typeof errorMessage !== 'string') continue
        ;
      (result[propertyName] ??= []).push(errorMessage)
    }
  } else if (typeof record.errors === 'object' && record.errors !== null) {
    for (const [field, messages] of Object.entries(record.errors)) {
      if (Array.isArray(messages)) result[field] = messages.map(String)
    }
  }

  return result
}

function jsonReplacer(_key: string, value: unknown): unknown {
  if (!(value instanceof Uint8Array)) return value

  let text = ''

  for (let index = 0; index < value.length; index += 0x8000)
    text += String.fromCharCode(...value.subarray(index, index + 0x8000))

  return btoa(text)
}

function buildUrl(path: string, query: ApiOptions['query']): string {
  if (!query) return path

  const params = new URLSearchParams()

  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined) params.set(key, String(value))
  }

  const text = params.toString()

  return text ? `${path}?${text}` : path
}

async function readBody(response: Response, sealed?: SealedRequest): Promise<unknown> {
  if (response.status === 204) return undefined

  let contentType = response.headers.get('Content-Type') ?? ''
  let opened: ArrayBuffer | undefined

  if (sealed !== undefined && contentType.startsWith(encryptedType)) {
    const result = await sealed.open(response)

    contentType = result.inner
    opened = result.bytes.buffer.slice(
      result.bytes.byteOffset,
      result.bytes.byteOffset + result.bytes.byteLength,
    ) as ArrayBuffer
  }

  if (contentType.startsWith(messagePackType)) {
    const shape = response.headers.get(wireShapeHeader)
    const hash = response.headers.get(wireSchemaHeader)

    if (shape === null || hash === null)
      throw new Error('A MessagePack response named no wire shape.')

    const [, bytes] = await Promise.all([
      ensureWireShape(shape, 'decode', hash),
      opened ?? response.arrayBuffer(),
    ])

    return decodeWire(bytes, shape, response.headers.get(wireCompressionHeader))
  }

  const text = opened === undefined ? await response.text() : new TextDecoder().decode(opened)

  if (!text) return undefined

  let parsed: unknown

  try {
    parsed = JSON.parse(text)
  } catch {
    return text
  }

  if (contentType.startsWith(columnarJsonType)) {
    const shape = response.headers.get(wireShapeHeader)
    const hash = response.headers.get(wireSchemaHeader)

    if (shape === null || hash === null)
      throw new Error('A columnar JSON response named no wire shape.')

    await ensureWireShape(shape, 'walk', hash)

    return decodeColumnarJson(parsed, shape)
  }

  return parsed
}

async function getAntiforgeryToken(): Promise<string> {
  antiforgeryToken ??= fetch(antiforgeryPath, {credentials: 'same-origin'})
    .then(async (response) => {
      if (!response.ok) throw new ApiError(response.status, await readBody(response))

      const body = (await response.json()) as { token: string }

      return body.token
    })
    .catch((error: unknown) => {
      antiforgeryToken = undefined
      throw error
    })

  return antiforgeryToken
}

function unauthorised(error: ApiError, anonymous: boolean | undefined): void {
  if (error.kind === 'unauthorized' && !anonymous) {
    resetAntiforgeryToken()
    handlers.onUnauthorized?.()
  }
}

export async function useApiResult<T = unknown>(
  path: string,
  options: ApiOptions = {},
): Promise<ApiResult<T>> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = {
    Accept: options.anonymous
      ? 'application/json'
      : `${messagePackType}, ${columnarJsonType};q=0.95, application/json;q=0.9`,
    'Accept-Language': apiLanguage.value,
  }

  if (method !== 'GET' && !options.anonymous) {
    headers[antiforgeryHeaderName] = await getAntiforgeryToken()
  }

  const wireFailure = (error: unknown): unknown => {
    if (!(error instanceof WireSchemaError)) return error

    const failure = new ApiError(error.status, undefined)

    unauthorised(failure, options.anonymous)

    return failure
  }

  if (!options.anonymous) {
    try {
      await loadWireHead()
    } catch (error) {
      throw wireFailure(error)
    }
  }

  let packed: ArrayBuffer | undefined

  if (options.body !== undefined && options.formData === undefined) {
    if (!options.anonymous) {
      const shape = requestShape(method, path)

      if (shape !== undefined) {
        try {
          await ensureWireShape(shape, 'encode')
        } catch (error) {
          throw wireFailure(error)
        }

        packed = encodeWire(shape, options.body)
      }
    }

    headers['Content-Type'] = packed === undefined ? 'application/json' : messagePackType
  }

  const url = buildUrl(path, options.query)
  const publicKey =
    options.anonymous || options.formData !== undefined ? undefined : wireEncryptionKey()

  let sealed: SealedRequest | undefined
  let payload: BodyInit | undefined =
    options.formData ??
    packed ??
    (options.body === undefined ? undefined : JSON.stringify(options.body, jsonReplacer))

  if (publicKey !== undefined) {
    const inner = packed === undefined ? 'application/json' : messagePackType
    const plaintext =
      packed !== undefined
        ? new Uint8Array(packed)
        : typeof payload === 'string'
          ? new TextEncoder().encode(payload)
          : undefined

    sealed = await sealRequest(publicKey, method, url, inner, plaintext)
    Object.assign(headers, sealed.headers)
    payload = sealed.body
  }

  let response: Response

  try {
    response = await fetch(url, {
      method,
      headers,
      credentials: 'same-origin',
      body: payload,
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error

    throw new ApiError(0, undefined)
  }

  const body = await readBody(response, sealed)

  noteSessionExpiry(response.headers.get(sessionExpiryHeaderName))

  if (response.ok) return {status: response.status, body: body as T}

  const error = new ApiError(response.status, body)

  unauthorised(error, options.anonymous)

  throw error
}

export async function useApi<T = unknown>(path: string, options: ApiOptions = {}): Promise<T> {
  return (await useApiResult<T>(path, options)).body
}

export async function useApiStream(path: string, options: ApiOptions = {}): Promise<Response> {
  const headers: Record<string, string> = {
    Accept: options.accept ?? 'text/event-stream',
    'Accept-Language': apiLanguage.value,
    'Content-Type': 'application/json',
  }

  if (!options.anonymous) headers[antiforgeryHeaderName] = await getAntiforgeryToken()

  let response: Response

  try {
    response = await fetch(buildUrl(path, options.query), {
      method: options.method ?? 'POST',
      headers,
      credentials: 'same-origin',
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error

    throw new ApiError(0, undefined)
  }

  noteSessionExpiry(response.headers.get(sessionExpiryHeaderName))

  if (response.ok) return response

  const error = new ApiError(response.status, await readBody(response))

  if (error.kind === 'unauthorized' && !options.anonymous) {
    resetAntiforgeryToken()
    handlers.onUnauthorized?.()
  }

  throw error
}

export interface ApiDownload {
  blob: Blob
  disposition: string | null
}

export async function useApiDownload(path: string, options: ApiOptions = {}): Promise<ApiDownload> {
  const response = await useApiStream(path, {...options, accept: options.accept ?? '*/*'})

  return {blob: await response.blob(), disposition: response.headers.get('Content-Disposition')}
}
