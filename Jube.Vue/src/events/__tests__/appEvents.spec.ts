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

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { QueryClient } from '@tanstack/vue-query'
import {
  eventStreamStatus,
  onServiceChange,
  operationOf,
  parseServiceChange,
  startAppEvents,
  stopAppEvents,
} from '../appEvents'

class FakeEventSource {
  static instances: FakeEventSource[] = []
  static readonly CLOSED = 2

  readyState = 0
  onopen: (() => void) | null = null
  onerror: (() => void) | null = null
  onmessage: ((event: MessageEvent<string>) => void) | null = null
  closed = false
  readonly listeners = new Map<string, (event: MessageEvent<string>) => void>()

  constructor(readonly url: string) {
    FakeEventSource.instances.push(this)
  }

  addEventListener(type: string, listener: (event: MessageEvent<string>) => void): void {
    this.listeners.set(type, listener)
  }

  close(): void {
    this.closed = true
  }

  emit(type: string, data: unknown): void {
    this.listeners.get(type)?.(new MessageEvent(type, { data: JSON.stringify(data) }))
  }
}

beforeEach(() => {
  FakeEventSource.instances = []
  vi.stubGlobal('EventSource', FakeEventSource)
})

afterEach(() => {
  stopAppEvents()
  vi.unstubAllGlobals()
})

function start() {
  const client = new QueryClient()
  const invalidate = vi.spyOn(client, 'invalidateQueries')
  const onNotification = vi.fn<(payload: unknown) => void>()
  const onRevoked = vi.fn<() => void>()

  startAppEvents({ client, onNotification, onRevoked })

  return { source: FakeEventSource.instances[0]!, invalidate, onNotification, onRevoked }
}

describe('parseServiceChange', () => {
  it('reads the server casing and the camel casing alike', () => {
    expect(parseServiceChange({ Area: 'List', Kind: 'Updated', EntityId: 4 })).toMatchObject({
      area: 'List',
      kind: 'Updated',
      entityId: 4,
    })
    expect(parseServiceChange({ area: 'List' })).toMatchObject({ area: 'List', entityId: null })
  })

  it('rejects a payload with no area', () => {
    expect(parseServiceChange({ kind: 'Updated' })).toBeUndefined()
    expect(parseServiceChange(null)).toBeUndefined()
  })
})

describe('the application event stream', () => {
  it('opens one connection however many times it is started', () => {
    start()
    start()

    expect(FakeEventSource.instances).toHaveLength(1)
    expect(FakeEventSource.instances[0]!.url).toBe('/api/ServiceChange/Stream')
  })

  it('invalidates the queries for the area that changed and nothing else', () => {
    const { source, invalidate } = start()

    source.emit('serviceChange', { Area: 'Dictionary', Kind: 'Updated' })

    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['service', 'Dictionary'] })
  })

  it('tells a listener the change, and stops when it unsubscribes', () => {
    const { source } = start()
    const heard: string[] = []
    const stop = onServiceChange((change) => heard.push(`${change.operation}:${change.actor}`))

    source.emit('serviceChange', { Area: 'Tag', Kind: 'Updated', Actor: 'checker', EntityId: 7 })
    stop()
    source.emit('serviceChange', { Area: 'Tag', Kind: 'Deleted', Actor: 'maker' })

    expect(heard).toEqual(['updated:checker'])
  })

  it('reads the kind of change whether it arrives as a name or a number', () => {
    expect(operationOf('Created')).toBe('created')
    expect(operationOf('deleted')).toBe('deleted')
    expect(operationOf(1)).toBe('updated')
    expect(operationOf(9)).toBe('unknown')
    expect(operationOf(undefined)).toBe('unknown')
  })

  it('ignores a service change it cannot read', () => {
    const { source, invalidate } = start()

    source.emit('serviceChange', 'junk')

    expect(invalidate).not.toHaveBeenCalled()
  })

  it('hands notifications to the notification handler', () => {
    const { source, onNotification } = start()

    source.emit('notification', { id: 1, title: 'Hi' })

    expect(onNotification).toHaveBeenCalledWith({ id: 1, title: 'Hi' })
  })

  it('closes on revocation and tells the app', () => {
    const { source, onRevoked } = start()

    source.emit('revoked', {})

    expect(source.closed).toBe(true)
    expect(onRevoked).toHaveBeenCalledTimes(1)
    expect(eventStreamStatus.value).toBe('idle')
  })

  it('tracks the connection status', () => {
    const { source } = start()

    expect(eventStreamStatus.value).toBe('connecting')
    source.onopen?.()
    expect(eventStreamStatus.value).toBe('open')
    source.readyState = FakeEventSource.CLOSED
    source.onerror?.()
    expect(eventStreamStatus.value).toBe('closed')
  })
})
