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

import { describe, expect, it } from 'vitest'
import { openWatcherStream, watcherStreamPath, type StreamSource } from '../stream'

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
    emit: (type: string, data: string) => listeners.get(type)?.({ data } as MessageEvent<string>),
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
    expect(seen).toEqual([{ key: 'K' }])
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
})
