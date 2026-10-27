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

import { noteSessionExpiry } from '../auth/sessionExpiry'
import { apiLanguage } from './language'

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
      const { propertyName, errorMessage } = entry as Record<string, unknown>
      if (typeof propertyName !== 'string' || typeof errorMessage !== 'string') continue
      ;(result[propertyName] ??= []).push(errorMessage)
    }
  } else if (typeof record.errors === 'object' && record.errors !== null) {
    for (const [field, messages] of Object.entries(record.errors)) {
      if (Array.isArray(messages)) result[field] = messages.map(String)
    }
  }

  return result
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

async function readBody(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined

  const text = await response.text()

  if (!text) return undefined

  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

async function getAntiforgeryToken(): Promise<string> {
  antiforgeryToken ??= fetch(antiforgeryPath, { credentials: 'same-origin' })
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

export async function useApiResult<T = unknown>(
  path: string,
  options: ApiOptions = {},
): Promise<ApiResult<T>> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = {
    Accept: 'application/json',
    'Accept-Language': apiLanguage.value,
  }

  if (method !== 'GET' && !options.anonymous) {
    headers[antiforgeryHeaderName] = await getAntiforgeryToken()
  }

  if (options.body !== undefined && options.formData === undefined) {
    headers['Content-Type'] = 'application/json'
  }

  let response: Response

  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      credentials: 'same-origin',
      body:
        options.formData ?? (options.body === undefined ? undefined : JSON.stringify(options.body)),
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error

    throw new ApiError(0, undefined)
  }

  const body = await readBody(response)

  noteSessionExpiry(response.headers.get(sessionExpiryHeaderName))

  if (response.ok) return { status: response.status, body: body as T }

  const error = new ApiError(response.status, body)

  if (error.kind === 'unauthorized' && !options.anonymous) {
    resetAntiforgeryToken()
    handlers.onUnauthorized?.()
  }

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
  const response = await useApiStream(path, { ...options, accept: options.accept ?? '*/*' })

  return { blob: await response.blob(), disposition: response.headers.get('Content-Disposition') }
}
