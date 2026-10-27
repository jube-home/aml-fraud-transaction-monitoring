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

import { describe, expect, it, vi } from 'vitest'
import { createJoobyController, type ChatSurface, type SurfaceMessage } from '../controller'
import type { JoobyApi } from '../api'
import type { StreamItem } from '../types'

function fakeSurface() {
  const messages: SurfaceMessage[] = []
  const surface: ChatSurface = {
    add: (message) => messages.push({ ...message }) - 1,
    update: (index, change) => Object.assign(messages[index]!, change),
    clear: () => {
      messages.length = 0
    },
    scrollToBottom: () => undefined,
  }

  return { messages, surface }
}

async function* itemsOf(...items: StreamItem[]): AsyncGenerator<StreamItem> {
  for (const item of items) yield item
}

function fakeApi(overrides: Partial<Record<keyof JoobyApi, unknown>> = {}): JoobyApi {
  return {
    list: vi.fn(),
    create: vi.fn().mockResolvedValue({ id: 1, title: null, createdDate: '', updatedDate: null }),
    remove: vi.fn().mockResolvedValue(undefined),
    messages: vi.fn().mockResolvedValue([]),
    startupStats: vi.fn().mockResolvedValue(null),
    guidance: vi.fn().mockResolvedValue(undefined),
    feedback: vi.fn().mockResolvedValue('Noted.'),
    upload: vi.fn().mockResolvedValue({ id: 9, name: 'a.csv' }),
    removeFile: vi.fn().mockResolvedValue(undefined),
    fileText: vi.fn(),
    fileUrl: (id: number) => `/f/${id}`,
    transcriptUrl: (id: number) => `/t/${id}`,
    auditUrl: (id: number) => `/a/${id}`,
    stream: vi.fn().mockResolvedValue(itemsOf()),
    ...overrides,
  } as unknown as JoobyApi
}

function build(api: JoobyApi, extra: { offline?: boolean; greeting?: 'model' | 'static' } = {}) {
  const { messages, surface } = fakeSurface()
  const notify = vi.fn()
  const changed = vi.fn()
  const controller = createJoobyController({
    api,
    offline: extra.offline ?? false,
    greeting: extra.greeting,
    isLandlord: false,
    pageContext: () => 'ctx',
    onConversationsChanged: changed,
    notify,
    now: () => new Date('2026-10-20T10:00:00Z'),
  })

  controller.attach(surface)

  return { controller, messages, notify, changed }
}

const settle = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, 0))

describe('the Jooby controller', () => {
  it('greets a new conversation and streams the text into one growing message', async () => {
    const api = fakeApi({
      stream: vi
        .fn()
        .mockResolvedValue(
          itemsOf(
            { kind: 'Text', text: 'Hel' },
            { kind: 'Text', text: 'lo', messageId: 5 },
            { kind: 'Stats', inputTokens: 3, outputTokens: 4 },
          ),
        ),
    })
    const { controller, messages } = build(api)

    await controller.newConversation(true)
    await settle()

    expect(messages.filter((message) => message.role === 'ai')).toEqual([
      { role: 'ai', text: 'Hello' },
    ])

    const meta = messages.filter((message) => message.role === 'meta')

    expect(meta).toHaveLength(1)
    expect(meta[0]!.html).toContain('↑3')
    expect(meta[0]!.html).not.toContain('jooby-feedback')
  })

  it('opens a new conversation empty, without calling the model, when the greeting is static', async () => {
    const api = fakeApi()
    const { controller, messages } = build(api, { greeting: 'static' })

    await controller.newConversation(true)
    await settle()

    expect(api.stream).not.toHaveBeenCalled()
    expect(messages).toEqual([])
    expect(controller.state.empty).toBe(true)

    await controller.send('first question', true)

    expect(controller.state.empty).toBe(false)
    expect(api.stream).toHaveBeenCalledTimes(1)
  })

  it('does not mark a conversation that already has messages as empty', async () => {
    const api = fakeApi({
      messages: vi.fn().mockResolvedValue([
        {
          kind: 'Message',
          id: 2,
          role: 'User',
          text: 'Q',
          createdDate: '2026-10-20T09:01:00Z',
          attachments: null,
        },
      ]),
    })
    const { controller } = build(api, { greeting: 'static' })

    await controller.openConversation(1)

    expect(controller.state.empty).toBe(false)
  })

  it('never adds an empty message and offers feedback on real replies', async () => {
    const api = fakeApi({
      stream: vi
        .fn()
        .mockResolvedValue(
          itemsOf({ kind: 'Text', text: '' }, { kind: 'Text', text: 'Answer', messageId: 8 }),
        ),
    })
    const { controller, messages } = build(api)

    await controller.newConversation(false)
    await controller.send('question', true)
    await settle()

    expect(messages.some((message) => message.role === 'ai' && message.text === '')).toBe(false)
    expect(messages.find((message) => message.role === 'meta')!.html).toContain(
      'data-message-id="8"',
    )
  })

  it('shows tool steps, thinking and errors as their own rows', async () => {
    const api = fakeApi({
      stream: vi
        .fn()
        .mockResolvedValue(
          itemsOf(
            { kind: 'Thinking', text: 'hmm ', agentLabel: 'Planner' },
            { kind: 'Thinking', text: 'right' },
            { kind: 'Step', text: 'Looked it up', detail: 'q=1', iconId: 3 },
            { kind: 'Error', text: 'Bad news' },
          ),
        ),
    })
    const { controller, messages } = build(api)

    await controller.newConversation(false)
    await controller.send('go', true)
    await settle()

    expect(messages.find((message) => message.role === 'thinking')!.text).toBe('hmm ')
    expect(messages.some((message) => message.role === 'activity')).toBe(true)
    expect(messages.find((message) => message.role === 'failure')!.html).toContain('Bad news')
  })

  it('sends the typed text with the page context and the uploaded file ids', async () => {
    const api = fakeApi()
    const { controller } = build(api)

    await controller.newConversation(false)
    await controller.attachFile(new File(['x'], 'a.csv'))
    await controller.send('look at this', true)

    expect(api.stream).toHaveBeenLastCalledWith(
      'SendMessage',
      1,
      { text: 'look at this', pageContext: 'ctx', attachmentFileIds: [9] },
      expect.any(AbortSignal),
    )
    expect(controller.state.pending).toEqual([])
  })

  it('sends ticked options, joined, and clears the choices', async () => {
    const { controller } = build(fakeApi())

    await controller.newConversation(false)
    controller.state.options = ['A', 'B', 'C']
    controller.toggleOption('A')
    controller.toggleOption('C')
    expect(controller.canSend('')).toBe(true)

    await controller.send('', false)

    expect(controller.state.options).toEqual([])
    expect(controller.state.selected).toEqual([])
  })

  it('routes a message sent mid-reply to the guidance endpoint', async () => {
    let release: () => void = () => undefined
    const gate = new Promise<void>((resolve) => {
      release = resolve
    })
    const api = fakeApi({
      stream: vi.fn().mockImplementation(async () =>
        (async function* () {
          await gate
          yield { kind: 'Text', text: 'late' } as StreamItem
        })(),
      ),
    })
    const { controller } = build(api)

    await controller.newConversation(false)
    await controller.send('first', true)
    await settle()

    expect(controller.state.busy).toBe(true)

    await controller.send('second', true)

    expect(api.guidance).toHaveBeenCalledWith(1, 'second')
    expect(api.stream).toHaveBeenCalledTimes(1)
    release()
    await settle()
  })

  it('stops a running reply and says so', async () => {
    const api = fakeApi({
      stream: vi.fn().mockImplementation((_p, _id, _b, signal: AbortSignal) =>
        Promise.resolve(
          (async function* () {
            await new Promise((_resolve, reject) => {
              signal.addEventListener('abort', () =>
                reject(new DOMException('aborted', 'AbortError')),
              )
            })
            yield { kind: 'Title' } as StreamItem
          })(),
        ),
      ),
    })
    const { controller, messages } = build(api)

    await controller.newConversation(false)
    await controller.send('slow', true)
    await settle()
    controller.stop()
    await settle()

    expect(controller.state.busy).toBe(false)
    expect(messages.some((message) => message.html?.includes('Stopped.'))).toBe(true)
  })

  it('reports a stream failure as an error row and frees the composer', async () => {
    const api = fakeApi({ stream: vi.fn().mockRejectedValue(new Error('offline')) })
    const { controller, messages } = build(api)

    await controller.newConversation(false)
    await controller.send('go', true)
    await settle()

    expect(controller.state.busy).toBe(false)
    expect(messages.find((message) => message.role === 'failure')!.html).toContain('offline')
  })

  it('refuses unsupported attachment types without calling the server', async () => {
    const api = fakeApi()
    const { controller, messages } = build(api)

    await controller.attachFile(new File(['x'], 'photo.png'))

    expect(api.upload).not.toHaveBeenCalled()
    expect(messages[0]!.html).toContain('Only .txt')
  })

  it('shows the offline welcome in the transcript for the model-greeting widget', async () => {
    const api = fakeApi({
      startupStats: vi
        .fn()
        .mockResolvedValue({ bites: 1, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 }),
    })
    const { controller, messages } = build(api, { offline: true })

    await controller.newConversation(true)
    await settle()

    expect(api.stream).not.toHaveBeenCalled()
    expect(messages.find((message) => message.role === 'ai')!.text).toContain('DigitalOcean')
    expect(controller.canSend('hello')).toBe(false)
  })

  it('renders a stored transcript, marking the first message as the greeting', async () => {
    const api = fakeApi({
      messages: vi.fn().mockResolvedValue([
        {
          kind: 'Message',
          id: 1,
          role: 'Assistant',
          text: 'Hi',
          createdDate: '2026-10-20T09:00:00Z',
          attachments: null,
        },
        {
          kind: 'Message',
          id: 2,
          role: 'User',
          text: 'Q',
          createdDate: '2026-10-20T09:01:00Z',
          attachments: null,
        },
        {
          kind: 'Step',
          text: 'tool',
          detail: null,
          iconId: null,
          createdDate: '2026-10-20T09:02:00Z',
        },
        {
          kind: 'Message',
          id: 3,
          role: 'Assistant',
          text: 'A',
          createdDate: '2026-10-20T09:03:00Z',
          attachments: null,
        },
      ]),
    })
    const { controller, messages } = build(api)

    await controller.openConversation(1)

    const feedback = messages.filter((message) => message.html?.includes('jooby-feedback'))

    expect(feedback).toHaveLength(1)
    expect(feedback[0]!.html).toContain('data-message-id="3"')
    expect(controller.state.hasExchange).toBe(true)
  })

  it('creates a conversation once when two things ask for one at the same time', async () => {
    const api = fakeApi()
    const { controller } = build(api)

    await Promise.all([
      controller.attachFile(new File(['x'], 'a.csv')),
      controller.attachFile(new File(['y'], 'b.csv')),
    ])

    expect(api.create).toHaveBeenCalledTimes(1)
  })

  it('removes the open conversation and clears the transcript', async () => {
    const api = fakeApi()
    const { controller, messages } = build(api)

    await controller.newConversation(false)
    await controller.send('hi', false)
    await controller.removeConversation(1)

    expect(controller.state.conversationId).toBeNull()
    expect(messages).toEqual([])
  })

  it('reports feedback confirmation as a step and failure as a notification', async () => {
    const api = fakeApi({
      feedback: vi.fn().mockRejectedValueOnce(new Error('nope')).mockResolvedValueOnce('Saved.'),
    })
    const { controller, messages, notify } = build(api)

    await controller.newConversation(false)

    expect(await controller.submitFeedback(5, true)).toBeUndefined()
    expect(notify).toHaveBeenCalledWith('error', expect.stringContaining('nope'))
    expect(await controller.submitFeedback(5, true)).toBe('Saved.')
    expect(messages.some((message) => message.html?.includes('Saved.'))).toBe(true)
  })
})
