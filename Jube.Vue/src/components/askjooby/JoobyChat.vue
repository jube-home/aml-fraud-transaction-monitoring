<!-- Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Paperclip, Square, X } from 'lucide-vue-next'
import 'deep-chat'
import type { DeepChat } from 'deep-chat'
import { joobyApi } from '../../askjooby/api'
import type { JoobyController } from '../../askjooby/controller'
import {
  auxiliaryStyle,
  avatars,
  chatStyle,
  inputAreaStyle,
  messageStyles,
  textInputStyles,
} from '../../askjooby/deepChatTheme'
import { createDeepChatSurface } from '../../askjooby/deepChatSurface'

const props = withDefaults(
  defineProps<{
    controller: JoobyController
    landlord: boolean
    offline: boolean
    attachments: boolean
    welcome?: string
  }>(),
  { welcome: '' },
)

const { t } = useI18n()
const chat = ref<DeepChat>()
const fileInput = ref<HTMLInputElement>()
const seconds = ref(0)

let timer: ReturnType<typeof setInterval> | undefined
let observer: MutationObserver | undefined

const state = props.controller.state

function stopTimer(): void {
  clearInterval(timer)
  timer = undefined
}

watch(
  () => state.thinking,
  (thinking) => {
    stopTimer()
    seconds.value = 0

    if (!thinking) return

    const started = Date.now()

    timer = setInterval(() => {
      seconds.value = Math.round((Date.now() - started) / 1000)
    }, 1000)
  },
)

function submittedText(body: unknown): string {
  const messages = (body as { messages?: { role?: string; text?: string }[] } | undefined)?.messages

  return [...(messages ?? [])].reverse().find((message) => message.role === 'user')?.text ?? ''
}

async function onFeedback(event: Partial<MouseEvent>): Promise<void> {
  const button = (event.target as Element | null)?.closest<HTMLButtonElement>(
    'button.jooby-feedback',
  )

  if (!button || button.disabled) return

  const group = button.parentElement!.querySelectorAll<HTMLButtonElement>('button.jooby-feedback')

  group.forEach((each) => {
    each.disabled = true
  })
  button.classList.add('jooby-feedback-chosen')

  const saved = await props.controller.submitFeedback(
    Number(button.dataset.messageId),
    button.dataset.positive === 'true',
  )

  if (saved === undefined) {
    group.forEach((each) => {
      each.disabled = false
    })
    button.classList.remove('jooby-feedback-chosen')
  }
}

onMounted(() => {
  const element = chat.value!

  Object.assign(element, {
    connect: {
      handler: (
        body: unknown,
        signals: { onResponse: (response: never[]) => Promise<void>; onClose: () => void },
      ) => {
        void props.controller.send(submittedText(body), true)
        void signals.onResponse([])
        signals.onClose()
      },
    },
    displayLoadingBubble: false,
    chatStyle,
    inputAreaStyle,
    messageStyles,
    avatars,
    auxiliaryStyle,
    textInput: {
      styles: textInputStyles,
      placeholder: { text: t('askJooby.placeholder') },
      disabled: props.offline,
    },
    htmlClassUtilities: { 'jooby-feedback': { events: { click: onFeedback } } },
  })

  props.controller.attach(createDeepChatSurface(element))

  const name = (): void => {
    element.shadowRoot
      ?.querySelectorAll<HTMLElement>('[role="button"]:not([aria-label])')
      .forEach((button) => button.setAttribute('aria-label', t('askJooby.send')))
  }

  observer = new MutationObserver(name)
  requestAnimationFrame(() => {
    if (!element.shadowRoot) return

    name()
    observer?.observe(element.shadowRoot, { childList: true, subtree: true })
  })
})

onBeforeUnmount(() => {
  observer?.disconnect()
  stopTimer()
  props.controller.attach(undefined)
})

function onFilesChosen(event: Event): void {
  const input = event.target as HTMLInputElement

  for (const file of Array.from(input.files ?? [])) void props.controller.attachFile(file)

  input.value = ''
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col gap-2">
    <div class="relative min-h-0 flex-1">
      <deep-chat ref="chat" class="absolute inset-0" data-testid="deep-chat"></deep-chat>
      <div
        v-if="state.conversationId === null || state.empty"
        class="pointer-events-none absolute inset-x-0 top-0 flex flex-col items-center gap-2 pt-10 text-center"
        data-testid="empty-state"
      >
        <img src="/images/jooby.png" alt="" class="h-40 w-auto" width="190" height="160" />
        <p class="max-w-md text-sm whitespace-pre-line" data-testid="empty-welcome">
          {{ props.welcome || t('askJooby.empty') }}
        </p>
      </div>
    </div>

    <div
      v-if="state.thinking"
      role="status"
      class="flex items-center gap-2 text-sm"
      data-testid="thinking"
    >
      <img
        src="/images/askjooby-avatar.png"
        alt=""
        class="size-7 rounded-full animate-gallop motion-reduce:animate-none"
        width="28"
        height="28"
      />
      <span class="animate-canter motion-reduce:animate-none">{{ t('askJooby.thinking') }}</span>
      <span class="badge badge-ghost badge-sm font-mono tabular-nums">{{
        t('askJooby.elapsed', { seconds })
      }}</span>
    </div>

    <fieldset
      v-if="state.options.length > 0"
      class="fieldset rounded-box border border-base-300 bg-base-100 p-3"
      data-testid="options"
    >
      <legend class="fieldset-legend">{{ t('askJooby.options') }}</legend>
      <p v-if="state.hint" class="label">{{ state.hint }}</p>
      <div class="flex flex-wrap gap-2">
        <input
          v-for="option in state.options"
          :key="option"
          type="checkbox"
          class="btn btn-sm rounded-full"
          :aria-label="option"
          :checked="state.selected.includes(option)"
          @change="props.controller.toggleOption(option)"
        />
      </div>
      <button
        type="button"
        class="btn btn-primary btn-sm self-start"
        :disabled="state.selected.length === 0"
        @click="props.controller.send('', false)"
      >
        {{ t('askJooby.go') }}
      </button>
    </fieldset>

    <div class="flex flex-wrap items-center gap-2">
      <template v-if="props.attachments && !props.offline">
        <button type="button" class="btn btn-sm" data-testid="attach" @click="fileInput?.click()">
          <Paperclip aria-hidden="true" class="size-4" />
          {{ t('askJooby.attach') }}
        </button>
        <input
          ref="fileInput"
          type="file"
          class="hidden"
          multiple
          :aria-label="t('askJooby.attachInput')"
          @change="onFilesChosen"
        />
        <span
          v-for="attachment in state.pending"
          :key="attachment.key"
          class="badge badge-neutral gap-1"
        >
          {{
            attachment.uploading
              ? t('askJooby.uploading', { name: attachment.name })
              : attachment.name
          }}
          <button
            v-if="!attachment.uploading"
            type="button"
            class="btn btn-ghost btn-xs btn-square"
            :aria-label="t('askJooby.removeAttachment', { name: attachment.name })"
            @click="props.controller.removeAttachment(attachment.key)"
          >
            <X aria-hidden="true" class="size-3" />
          </button>
        </span>
      </template>
      <button
        v-if="state.busy"
        type="button"
        class="btn btn-sm ms-auto"
        :disabled="!state.allowStop"
        data-testid="stop"
        @click="props.controller.stop()"
      >
        <Square aria-hidden="true" class="size-4" />
        {{ t('askJooby.stop') }}
      </button>
    </div>

    <p
      v-if="state.hasExchange && state.conversationId !== null"
      class="flex flex-wrap gap-x-4 text-xs"
    >
      <span v-if="props.controller.startedLabel()">
        {{ t('askJooby.started', { time: props.controller.startedLabel() }) }}
      </span>
      <a class="link" :href="joobyApi.transcriptUrl(state.conversationId)">{{
        t('askJooby.downloadTranscript')
      }}</a>
      <a v-if="props.landlord" class="link" :href="joobyApi.auditUrl(state.conversationId)">{{
        t('askJooby.downloadAudit')
      }}</a>
    </p>
  </div>
</template>
