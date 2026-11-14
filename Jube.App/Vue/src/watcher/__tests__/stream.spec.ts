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
import {openWatcherStream, watcherStreamPath, type StreamSource} from '../stream'

function fake() {
  const listeners = new Map<string, (event: MessageEvent<string>) => void>()
  let closed = 0
  let url = ''

  const source: StreamSource = {
    addEventListener: (type, listener) => listeners.set(type, listener),
    close: () => {
      closed += 1
    },
    onopen: null,
    onerror: null,
  }

  return {
    source,
    factory: (path: string) => {
      url = path

      return source
    },
    emit: (type: string, data: string) => listeners.get(type)?.({data} as MessageEvent<string>),
    closed: () => closed,
    url: () => url,
  }
}

describe('openWatcherStream', () => {
  it('opens the watcher stream, reports its status and hands on each activation', () => {
    const stream = fake()
    const seen: unknown[] = []
    const statuses: string[] = []

    const close = openWatcherStream(
      {
        onActivation: (payload) => seen.push(payload),
        onStatus: (status) => statuses.push(status),
      },
      stream.factory,
    )

    stream.source.onopen?.(new Event('open'))
    stream.emit('activation', '{"key":"K"}')
    stream.source.onerror?.(new Event('error'))
    close()

    expect(stream.url()).toBe(watcherStreamPath)
    expect(seen).toEqual([{key: 'K'}])
    expect(statuses).toEqual(['connecting', 'open', 'connecting', 'closed'])
    expect(stream.closed()).toBe(1)
  })

  it('ignores a message that is not JSON and closes for good when revoked', () => {
    const stream = fake()
    const seen: unknown[] = []
    const statuses: string[] = []

    openWatcherStream(
      {
        onActivation: (payload) => seen.push(payload),
        onStatus: (status) => statuses.push(status),
      },
      stream.factory,
    )
    stream.emit('activation', 'not json')
    stream.emit('revoked', '')

    expect(seen).toEqual([])
    expect(statuses.at(-1)).toBe('closed')
    expect(stream.closed()).toBe(1)
  })

  it('reopens a stream the browser gave up on, backing off, and stops once revoked', () => {
    vi.useFakeTimers()

    const sources: ReturnType<typeof fake>[] = []
    const statuses: string[] = []

    const close = openWatcherStream(
      {onActivation: () => undefined, onStatus: (status) => statuses.push(status)},
      (path) => {
        const next = fake()

        next.source.readyState = 2
        sources.push(next)

        return next.factory(path)
      },
    )

    sources[0]!.source.onerror?.(new Event('error'))
    expect(sources).toHaveLength(1)
    vi.advanceTimersByTime(5000)
    expect(sources).toHaveLength(2)
    sources[1]!.source.onerror?.(new Event('error'))
    vi.advanceTimersByTime(5000)
    expect(sources).toHaveLength(2)
    vi.advanceTimersByTime(5000)
    expect(sources).toHaveLength(3)

    sources[2]!.emit('revoked', '')
    sources[2]!.source.onerror?.(new Event('error'))
    vi.advanceTimersByTime(120000)
    expect(sources).toHaveLength(3)
    expect(statuses.at(-1)).toBe('closed')

    close()
    vi.useRealTimers()
  })

  it('cancels a pending reconnect when the caller closes the stream', () => {
    vi.useFakeTimers()

    const sources: ReturnType<typeof fake>[] = []

    const close = openWatcherStream(
      {onActivation: () => undefined, onStatus: () => undefined},
      (path) => {
        const next = fake()

        next.source.readyState = 2
        sources.push(next)

        return next.factory(path)
      },
    )

    sources[0]!.source.onerror?.(new Event('error'))
    close()
    vi.advanceTimersByTime(120000)
    expect(sources).toHaveLength(1)
    vi.useRealTimers()
  })
})
