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

export const watcherStreamPath = '/api/Watcher/Stream'

export type WatcherStreamStatus = 'idle' | 'connecting' | 'open' | 'closed'

export interface WatcherStreamHandlers {
  onActivation: (payload: unknown) => void
  onStatus: (status: WatcherStreamStatus) => void
}

export interface StreamSource {
  addEventListener: (type: string, listener: (event: MessageEvent<string>) => void) => void
  close: () => void
  onopen: ((event: Event) => void) | null
  onerror: ((event: Event) => void) | null
}

export type StreamFactory = (url: string) => StreamSource

export function openWatcherStream(
  handlers: WatcherStreamHandlers,
  factory: StreamFactory = (url) => new EventSource(url, { withCredentials: true }) as StreamSource,
): () => void {
  handlers.onStatus('connecting')

  const source = factory(watcherStreamPath)

  source.onopen = () => handlers.onStatus('open')
  source.onerror = () => handlers.onStatus('connecting')
  source.addEventListener('activation', (event) => {
    try {
      handlers.onActivation(JSON.parse(event.data))
    } catch {
      return
    }
  })
  source.addEventListener('revoked', () => {
    source.close()
    handlers.onStatus('closed')
  })

  return () => {
    source.close()
    handlers.onStatus('closed')
  }
}
