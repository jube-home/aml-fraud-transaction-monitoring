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
  readyState?: number
}

export type StreamFactory = (url: string) => StreamSource

const closedState = 2
const firstRetryMilliseconds = 5000
const maximumRetryMilliseconds = 60000

export function openWatcherStream(
  handlers: WatcherStreamHandlers,
  factory: StreamFactory = (url) => new EventSource(url, {withCredentials: true}) as StreamSource,
): () => void {
  let source: StreamSource | undefined
  let timer: ReturnType<typeof setTimeout> | undefined
  let retry = firstRetryMilliseconds
  let stopped = false

  function open(): void {
    handlers.onStatus('connecting')

    const current = factory(watcherStreamPath)

    source = current
    current.onopen = () => {
      retry = firstRetryMilliseconds
      handlers.onStatus('open')
    }
    current.onerror = () => {
      if (stopped) return

      if (current.readyState === closedState) {
        current.close()
        handlers.onStatus('connecting')
        timer = setTimeout(open, retry)
        retry = Math.min(retry * 2, maximumRetryMilliseconds)

        return
      }

      handlers.onStatus('connecting')
    }
    current.addEventListener('activation', (event) => {
      try {
        handlers.onActivation(JSON.parse(event.data))
      } catch {
        return
      }
    })
    current.addEventListener('revoked', () => {
      stopped = true
      current.close()
      handlers.onStatus('closed')
    })
  }

  open()

  return () => {
    stopped = true
    clearTimeout(timer)
    source?.close()
    handlers.onStatus('closed')
  }
}
