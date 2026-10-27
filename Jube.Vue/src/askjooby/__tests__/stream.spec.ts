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
import { readStreamItems, StreamFailure } from '../stream'
import type { StreamItem } from '../types'

function responseOf(...chunks: string[]): Response {
  const encoder = new TextEncoder()

  return new Response(
    new ReadableStream({
      start(controller) {
        for (const chunk of chunks) controller.enqueue(encoder.encode(chunk))

        controller.close()
      },
    }),
  )
}

async function collect(response: Response): Promise<StreamItem[]> {
  const items: StreamItem[] = []

  for await (const item of readStreamItems(response)) items.push(item)

  return items
}

describe('readStreamItems', () => {
  it('yields each data frame as a typed item', async () => {
    const items = await collect(
      responseOf(
        'event: item\ndata: {"kind":"Text","text":"a"}\n\nevent: item\ndata: {"kind":"Title"}\n\n',
      ),
    )

    expect(items).toEqual([{ kind: 'Text', text: 'a' }, { kind: 'Title' }])
  })

  it('reassembles a frame split across chunks, including CRLF line endings', async () => {
    const items = await collect(
      responseOf('event: item\r\ndata: {"kind":"Te', 'xt","text":"hi"}\r\n\r\n'),
    )

    expect(items).toEqual([{ kind: 'Text', text: 'hi' }])
  })

  it('stops at the complete event and ignores anything after it', async () => {
    const items = await collect(
      responseOf(
        'data: {"kind":"Title"}\n\nevent: complete\ndata: {}\n\ndata: {"kind":"Title"}\n\n',
      ),
    )

    expect(items).toHaveLength(1)
  })

  it('joins multi-line data and skips frames without data', async () => {
    const items = await collect(responseOf(': keep-alive\n\ndata: {"kind":\ndata: "Title"}\n\n'))

    expect(items).toEqual([{ kind: 'Title' }])
  })

  it('raises the server message on an error event', async () => {
    await expect(
      collect(responseOf('event: error\ndata: {"message":"model unavailable"}\n\n')),
    ).rejects.toThrow(new StreamFailure('model unavailable'))
  })

  it('raises the raw text when the error payload is not JSON', async () => {
    await expect(collect(responseOf('event: error\ndata: boom\n\n'))).rejects.toThrow('boom')
  })
})
