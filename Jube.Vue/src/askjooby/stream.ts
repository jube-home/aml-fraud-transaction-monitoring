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

import type { StreamItem } from './types'

export class StreamFailure extends Error {}

function parseFrame(frame: string): { event: string; data: string } | undefined {
  let event = 'item'
  const data: string[] = []

  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) event = line.slice(6).trim()
    else if (line.startsWith('data:')) data.push(line.slice(5).replace(/^ /, ''))
  }

  return data.length === 0 ? undefined : { event, data: data.join('\n') }
}

export async function* readStreamItems(response: Response): AsyncGenerator<StreamItem> {
  if (!response.body) return

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  try {
    for (;;) {
      const { done, value } = await reader.read()

      if (done) return

      buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n')

      let boundary = buffer.indexOf('\n\n')

      while (boundary !== -1) {
        const frame = parseFrame(buffer.slice(0, boundary))

        buffer = buffer.slice(boundary + 2)
        boundary = buffer.indexOf('\n\n')

        if (!frame) continue
        if (frame.event === 'complete') return

        if (frame.event === 'error') {
          let message = frame.data

          try {
            message = (JSON.parse(frame.data) as { message?: string }).message ?? frame.data
          } catch {
            message = frame.data
          }

          throw new StreamFailure(message)
        }

        yield JSON.parse(frame.data) as StreamItem
      }
    }
  } finally {
    await reader.cancel().catch(() => undefined)
  }
}
