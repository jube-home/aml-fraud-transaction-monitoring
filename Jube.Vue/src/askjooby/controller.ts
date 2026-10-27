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

import { reactive } from 'vue'
import { ApiError } from '../api/useApi'
import { pickOfflineWelcome } from './offlineWelcome'
import { formatTimestamp, joinWithAnd } from './format'
import {
  activityHtml,
  activityRoles,
  attachmentsHtml,
  metaHtml,
  svgDiagramHtml,
  thinkingLabel,
} from './render'
import type { JoobyApi } from './api'
import type {
  ActivityKind,
  ConversationSummary,
  MessageStats,
  PendingAttachment,
  StreamItem,
  TranscriptEntry,
} from './types'

export interface SurfaceMessage {
  role: string
  text?: string
  html?: string
}

export interface ChatSurface {
  add: (message: SurfaceMessage) => number
  update: (index: number, message: { text?: string; html?: string }) => void
  clear: () => void
  scrollToBottom: (force?: boolean) => void
}

export interface JoobyOptions {
  api: JoobyApi
  offline: boolean
  greeting?: 'model' | 'static'
  isLandlord: boolean
  pageContext: () => string
  onConversationsChanged: () => void
  notify: (kind: 'error' | 'info', text: string) => void
  now?: () => Date
}

export interface JoobyState {
  conversationId: number | null
  busy: boolean
  allowStop: boolean
  thinking: boolean
  options: string[]
  hint: string
  selected: string[]
  pending: PendingAttachment[]
  hasExchange: boolean
  empty: boolean
  startedAt: string | null
  updatedAt: string | null
}

interface StreamRun {
  abort: AbortController
}

interface ReplyContext {
  isGreeting: boolean
  assistantIndex: number | null
  assistantText: string
  assistantAt: Date | null
  metaIndex: number | null
  messageId: number | null
  stats: Partial<MessageStats> | undefined
  thinkingPasses: Map<string, { index: number; text: string }>
}

const allowedExtensions = ['.txt', '.md', '.csv', '.json', '.log']

function messageOf(error: unknown): string {
  if (error instanceof ApiError) {
    const errors = (error.body as { errors?: { errorMessage?: string }[] } | undefined)?.errors

    return errors?.[0]?.errorMessage ?? `The request failed with status ${error.status}.`
  }

  return error instanceof Error ? error.message : String(error)
}

export function createJoobyController(options: JoobyOptions) {
  const state = reactive<JoobyState>({
    conversationId: null,
    busy: false,
    allowStop: true,
    thinking: false,
    options: [],
    hint: '',
    selected: [],
    pending: [],
    hasExchange: false,
    empty: false,
    startedAt: null,
    updatedAt: null,
  })

  const runs = new Map<number, StreamRun>()
  const busyConversations = new Set<number>()
  let surface: ChatSurface | undefined
  let creating: Promise<number> | undefined
  let lastAgent: string | null = null
  let pendingKey = 0

  const now = (): Date => options.now?.() ?? new Date()
  const current = (id: number): boolean => state.conversationId === id

  function attach(next: ChatSurface | undefined): void {
    surface = next
  }

  function show(message: SurfaceMessage): number {
    const index = surface?.add(message) ?? -1

    state.updatedAt = now().toISOString()
    surface?.scrollToBottom()

    return index
  }

  function setBusy(id: number, busy: boolean, allowStop = true): void {
    if (busy) busyConversations.add(id)
    else busyConversations.delete(id)

    if (current(id)) {
      state.busy = busy
      state.allowStop = allowStop
    }
  }

  function addActivity(
    kind: ActivityKind,
    text: string,
    detail?: string | null,
    iconId?: number | null,
    timestamp?: string | Date | null,
  ): void {
    const agent = /^\[([A-Za-z0-9 .#_-]{1,48})]\s/.exec(text)?.[1] ?? null

    lastAgent = agent

    if (kind === 'Thought' || kind === 'Thinking') {
      show({ role: activityRoles[kind], text })
      return
    }

    show({
      role: activityRoles[kind],
      html: activityHtml(kind, text, detail, iconId, timestamp ?? now()),
    })
  }

  function addMessage(
    role: 'User' | 'Assistant',
    text: string,
    extras: {
      id?: number | null
      timestamp?: string | Date | null
      stats?: Parameters<typeof metaHtml>[0]['stats']
      attachments?: { id: number; name: string }[] | null
      greeting?: boolean
    } = {},
  ): number {
    state.empty = false

    if (role === 'User') state.hasExchange = true

    const index = show({ role: role === 'User' ? 'user' : 'ai', text })

    if (extras.attachments?.length) {
      void addAttachments(extras.attachments)
    }

    if (role === 'Assistant') {
      show({
        role: 'meta',
        html: metaHtml({
          timestamp: extras.timestamp,
          stats: extras.stats,
          messageId: extras.greeting ? null : (extras.id ?? null),
        }),
      })
    } else if (extras.timestamp) {
      show({ role: 'meta', html: metaHtml({ timestamp: extras.timestamp }) })
    }

    return index
  }

  async function addAttachments(attachments: { id: number; name: string }[]): Promise<void> {
    const images = attachments.filter((attachment) => /\.svg$/i.test(attachment.name))
    const files = attachments.filter((attachment) => !/\.svg$/i.test(attachment.name))

    if (files.length > 0) {
      show({ role: 'files', html: attachmentsHtml(files, options.api.fileUrl) })
    }

    for (const image of images) {
      try {
        const html = svgDiagramHtml(image.name, await options.api.fileText(image.id))

        if (html) show({ role: 'files', html })
      } catch {
        continue
      }
    }
  }

  function renderTranscript(entries: TranscriptEntry[]): void {
    surface?.clear()
    lastAgent = null
    state.hasExchange = false
    state.options = []
    state.selected = []
    state.startedAt = entries[0]?.createdDate ?? null
    state.updatedAt = entries.at(-1)?.createdDate ?? null

    let first = true

    for (const entry of entries) {
      if (entry.kind === 'Message') {
        const greeting = first

        first = false
        addMessage(entry.role, entry.text, {
          id: entry.role === 'Assistant' ? entry.id : null,
          timestamp: entry.createdDate,
          stats: entry,
          attachments: entry.attachments,
          greeting,
        })
      } else {
        addActivity(entry.kind, entry.text, entry.detail, entry.iconId, entry.createdDate)
      }
    }

    surface?.scrollToBottom(true)
  }

  function showSelection(choices: string[] | undefined, hint: string): void {
    state.options = choices ?? []
    state.selected = []
    state.hint = hint
  }

  function showMeta(context: ReplyContext): void {
    const html = metaHtml({
      timestamp: context.assistantAt,
      stats: context.stats,
      messageId: context.messageId,
    })

    if (context.metaIndex === null) context.metaIndex = show({ role: 'meta', html })
    else surface?.update(context.metaIndex, { html })
  }

  function handle(conversationId: number, item: StreamItem, context: ReplyContext): void {
    if (item.kind === 'Title') {
      options.onConversationsChanged()
      return
    }

    if (!current(conversationId)) return

    const closePass = (label: string | undefined): void => {
      context.thinkingPasses.delete(thinkingLabel(label) ?? '')
    }
    const resetAssistant = (): void => {
      context.assistantIndex = null
      context.assistantText = ''
      context.metaIndex = null
      context.messageId = null
      context.stats = undefined
    }

    switch (item.kind) {
      case 'Text': {
        context.thinkingPasses.clear()
        state.thinking = false

        if (item.text) {
          context.assistantText += item.text

          if (context.assistantIndex === null) {
            context.assistantAt = now()
            context.assistantIndex = show({ role: 'ai', text: context.assistantText })
          } else {
            surface?.update(context.assistantIndex, { text: context.assistantText })
          }
        }

        if (item.messageId) {
          context.messageId = context.isGreeting ? null : item.messageId
          showMeta(context)

          if (item.attachments?.length) void addAttachments(item.attachments)
        }

        surface?.scrollToBottom()
        break
      }
      case 'Thinking': {
        resetAssistant()

        const key = thinkingLabel(item.agentLabel) ?? ''
        let pass = context.thinkingPasses.get(key)

        if (!item.text) break

        if (!pass) {
          state.thinking = false

          if (key) show({ role: 'who', html: activityHtml('Thinking', key, null, null, null) })

          pass = { index: show({ role: 'thinking', text: item.text }), text: item.text }
          context.thinkingPasses.set(key, pass)
        } else {
          pass.text += item.text
          surface?.update(pass.index, { text: pass.text })
        }

        surface?.scrollToBottom()
        break
      }
      case 'Step':
        resetAssistant()
        closePass(item.agentLabel)
        addActivity('Step', item.text, item.detail, item.iconId)
        state.thinking = true
        break
      case 'Thought':
        resetAssistant()
        closePass(item.agentLabel)
        addActivity('Thought', item.text)
        state.thinking = true
        break
      case 'Guard':
        resetAssistant()
        addActivity('Guard', item.text, item.detail)
        break
      case 'Delegation':
      case 'Handoff':
      case 'Validation':
      case 'Revision':
        resetAssistant()
        closePass(item.agentLabel)
        addActivity(item.kind, item.text, item.detail)
        state.thinking = true
        break
      case 'Stats':
        if (context.assistantIndex !== null) {
          context.stats = item
          showMeta(context)
        }

        break
      case 'Suggestions':
        showSelection(item.suggestions, '')
        break
      case 'Clarification':
        showSelection(
          item.options,
          item.allowFreeText
            ? 'Tick one or more, or just type your own answer below.'
            : 'Tick one or more.',
        )
        break
      case 'Error':
        resetAssistant()
        context.thinkingPasses.clear()
        addActivity('Error', item.text)
        break
    }
  }

  async function run(
    conversationId: number,
    path: 'Start' | 'SendMessage',
    body: unknown,
    isGreeting: boolean,
    allowStop: boolean,
  ): Promise<void> {
    const abort = new AbortController()
    const context: ReplyContext = {
      isGreeting,
      assistantIndex: null,
      assistantText: '',
      assistantAt: null,
      metaIndex: null,
      messageId: null,
      stats: undefined,
      thinkingPasses: new Map(),
    }

    runs.set(conversationId, { abort })
    state.thinking = current(conversationId)
    setBusy(conversationId, true, allowStop)

    try {
      const items = await options.api.stream(path, conversationId, body, abort.signal)

      for await (const item of items) handle(conversationId, item, context)
    } catch (error) {
      if (
        !(error instanceof DOMException && error.name === 'AbortError') &&
        current(conversationId)
      ) {
        state.thinking = false
        addActivity('Error', `Something went wrong: ${messageOf(error)}`)
      }
    } finally {
      runs.delete(conversationId)
      setBusy(conversationId, false)
      options.onConversationsChanged()

      if (current(conversationId)) state.thinking = false
    }
  }

  async function startConversation(conversationId: number): Promise<void> {
    if (options.offline) {
      const stats = await options.api.startupStats().catch(() => null)

      addMessage('Assistant', pickOfflineWelcome(stats), { timestamp: now(), greeting: true })

      return
    }

    await run(conversationId, 'Start', { pageContext: options.pageContext() }, true, false)
  }

  async function openConversation(id: number, allowGreeting = true): Promise<void> {
    state.conversationId = id
    state.pending = state.pending.filter((attachment) => attachment.uploading)
    state.busy = busyConversations.has(id)
    state.thinking = false

    const entries = await options.api.messages(id)

    if (!current(id)) return

    renderTranscript(entries)

    state.empty = entries.length === 0 && !runs.has(id)

    if (allowGreeting && entries.length === 0 && options.greeting !== 'static') {
      state.empty = false
      void startConversation(id)
    } else if (runs.has(id)) {
      state.thinking = true
      surface?.scrollToBottom(true)
    }
  }

  async function newConversation(allowGreeting = true): Promise<number> {
    const conversation: ConversationSummary = await options.api.create()

    options.onConversationsChanged()
    await openConversation(conversation.id, allowGreeting)

    return conversation.id
  }

  async function createQuietly(): Promise<number> {
    const conversation: ConversationSummary = await options.api.create()

    state.conversationId = conversation.id
    options.onConversationsChanged()

    return conversation.id
  }

  async function ensureConversation(): Promise<number> {
    if (state.conversationId !== null) return state.conversationId

    creating ??= createQuietly().finally(() => {
      creating = undefined
    })

    return creating
  }

  async function removeConversation(id: number): Promise<void> {
    await options.api.remove(id)
    runs.get(id)?.abort.abort()
    runs.delete(id)
    busyConversations.delete(id)
    options.onConversationsChanged()

    if (current(id)) {
      state.conversationId = null
      state.busy = false
      state.thinking = false
      state.hasExchange = false
      state.empty = false
      surface?.clear()
    }
  }

  function canSend(typed: string): boolean {
    return (
      !options.offline &&
      (typed.trim() !== '' ||
        state.pending.some((attachment) => !attachment.uploading) ||
        state.selected.length > 0)
    )
  }

  async function send(typed: string, displayed: boolean): Promise<void> {
    const trimmed = typed.trim()

    if (!canSend(trimmed)) return

    const selectedPart = joinWithAnd(state.selected)
    const text = selectedPart ? (trimmed ? `${selectedPart} -- ${trimmed}` : selectedPart) : trimmed
    const ready = state.pending.filter((attachment) => !attachment.uploading)
    const attachmentIds = ready.map((attachment) => attachment.id!)

    state.options = []
    state.selected = []
    state.hint = ''
    state.empty = false
    state.pending = state.pending.filter((attachment) => attachment.uploading)

    if (!displayed) addMessage('User', text, { timestamp: now() })
    else state.hasExchange = true

    if (ready.length > 0) {
      await addAttachments(
        ready.map((attachment) => ({ id: attachment.id!, name: attachment.name })),
      )
    }

    let conversationId: number

    try {
      conversationId = await ensureConversation()
    } catch (error) {
      addActivity('Error', `Could not start a conversation: ${messageOf(error)}`)
      return
    }

    if (busyConversations.has(conversationId)) {
      try {
        await options.api.guidance(conversationId, text)
      } catch {
        addActivity('Error', "That didn't reach Jooby -- try sending it again in a moment.")
      }

      return
    }

    void run(
      conversationId,
      'SendMessage',
      { text, pageContext: options.pageContext(), attachmentFileIds: attachmentIds },
      false,
      true,
    )
  }

  function stop(): void {
    const id = state.conversationId

    if (id === null) return

    runs.get(id)?.abort.abort()
    state.thinking = false
    setBusy(id, false)
    addActivity('Step', 'Stopped.')
  }

  async function attachFile(file: File): Promise<void> {
    const extension = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`

    if (!allowedExtensions.includes(extension)) {
      addActivity('Error', 'Only .txt, .md, .csv, .json or .log files can be attached.')
      return
    }

    const key = `pending-${(pendingKey += 1)}`

    state.pending.push({ key, name: file.name, uploading: true })

    try {
      const conversationId = await ensureConversation()
      const uploaded = await options.api.upload(conversationId, file)
      const entry = state.pending.find((attachment) => attachment.key === key)

      if (entry) {
        entry.id = uploaded.id
        entry.name = uploaded.name ?? file.name
        entry.uploading = false
      }
    } catch {
      state.pending = state.pending.filter((attachment) => attachment.key !== key)
      addActivity('Error', `"${file.name}" could not be attached.`)
    }
  }

  function removeAttachment(key: string): void {
    const entry = state.pending.find((attachment) => attachment.key === key)

    state.pending = state.pending.filter((attachment) => attachment.key !== key)

    if (entry?.id !== undefined) void options.api.removeFile(entry.id).catch(() => undefined)
  }

  async function submitFeedback(
    messageId: number,
    isPositive: boolean,
  ): Promise<string | undefined> {
    const conversationId = state.conversationId

    if (conversationId === null) return undefined

    try {
      const confirmation = await options.api.feedback(conversationId, messageId, isPositive)

      addActivity('Step', confirmation)

      return confirmation
    } catch (error) {
      options.notify('error', `Couldn't save feedback: ${messageOf(error)}`)

      return undefined
    }
  }

  function toggleOption(option: string): void {
    state.selected = state.selected.includes(option)
      ? state.selected.filter((value) => value !== option)
      : [...state.selected, option]
  }

  return {
    state,
    attach,
    openConversation,
    newConversation,
    removeConversation,
    send,
    stop,
    canSend,
    attachFile,
    removeAttachment,
    submitFeedback,
    toggleOption,
    startedLabel: () => formatTimestamp(state.startedAt),
    updatedLabel: () => formatTimestamp(state.updatedAt),
  }
}

export type JoobyController = ReturnType<typeof createJoobyController>
