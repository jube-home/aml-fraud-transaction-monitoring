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

import { ref } from 'vue'
import type { QueryClient } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'

export const eventStreamPath = '/api/ServiceChange/Stream'

export type EventStreamStatus = 'idle' | 'connecting' | 'open' | 'closed'

export type ServiceOperation = 'created' | 'updated' | 'deleted' | 'unknown'

export interface ServiceChange {
  area: string
  kind: string
  operation: ServiceOperation
  at: string | null
  entityId: number | null
  version: number | null
  actor: string
  correlationId: string
}

export interface AppEventHandlers {
  client: QueryClient
  onNotification: (payload: unknown) => void
  onRevoked: () => void
  onOther?: (type: string, payload: unknown) => void
}

export const eventStreamStatus = ref<EventStreamStatus>('idle')

type ServiceChangeListener = (change: ServiceChange) => void

const listeners = new Set<ServiceChangeListener>()

export function onServiceChange(listener: ServiceChangeListener): () => void {
  listeners.add(listener)

  return () => listeners.delete(listener)
}

const operations: ServiceOperation[] = ['created', 'updated', 'deleted']

export function operationOf(kind: unknown): ServiceOperation {
  if (typeof kind === 'number') return operations[kind] ?? 'unknown'

  const text = String(kind ?? '').toLowerCase()

  return operations.find((candidate) => candidate === text) ?? 'unknown'
}

function field<T>(value: Record<string, unknown>, name: string): T | undefined {
  const pascal = name[0]!.toUpperCase() + name.slice(1)

  return (value[name] ?? value[pascal]) as T | undefined
}

export function parseServiceChange(payload: unknown): ServiceChange | undefined {
  if (typeof payload !== 'object' || payload === null) return undefined

  const value = payload as Record<string, unknown>
  const area = field<string>(value, 'area')

  if (typeof area !== 'string') return undefined

  const kind = field(value, 'kind')
  const at = field<string>(value, 'at')

  return {
    area,
    kind: String(kind ?? ''),
    operation: operationOf(kind),
    at: typeof at === 'string' ? at : null,
    entityId: field<number>(value, 'entityId') ?? null,
    version: field<number>(value, 'version') ?? null,
    actor: String(field(value, 'actor') ?? ''),
    correlationId: String(field(value, 'correlationId') ?? ''),
  }
}

function parse(data: string): unknown {
  try {
    return JSON.parse(data)
  } catch {
    return undefined
  }
}

let source: EventSource | undefined

export function startAppEvents(handlers: AppEventHandlers): void {
  if (source || typeof EventSource === 'undefined') return

  eventStreamStatus.value = 'connecting'

  const stream = new EventSource(eventStreamPath, { withCredentials: true })

  source = stream

  stream.onopen = () => {
    eventStreamStatus.value = 'open'
  }

  stream.onerror = () => {
    eventStreamStatus.value = stream.readyState === EventSource.CLOSED ? 'closed' : 'connecting'
  }

  stream.addEventListener('serviceChange', (event) => {
    const change = parseServiceChange(parse((event as MessageEvent<string>).data))

    if (!change) return

    void handlers.client.invalidateQueries({ queryKey: queryKeys.service(change.area) })

    for (const listener of listeners) listener(change)
  })

  stream.addEventListener('notification', (event) => {
    handlers.onNotification(parse((event as MessageEvent<string>).data))
  })

  stream.addEventListener('revoked', () => {
    stopAppEvents()
    handlers.onRevoked()
  })

  stream.onmessage = (event) => {
    handlers.onOther?.('message', parse((event as MessageEvent<string>).data))
  }
}

export function stopAppEvents(): void {
  source?.close()
  source = undefined
  eventStreamStatus.value = 'idle'
}
