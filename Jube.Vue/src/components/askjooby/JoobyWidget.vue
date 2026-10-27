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
import { nextTick, onBeforeUnmount, onMounted, watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { Plus, X } from 'lucide-vue-next'
import { useJoobyContextStore } from '../../askjooby/pageContext'
import { useJoobyWidgetStore } from '../../askjooby/widgetState'
import JoobyChat from './JoobyChat.vue'
import { useJooby } from './useJooby'

const { t } = useI18n()
const context = useJoobyContextStore()
const { controller, offline, landlord, guard } = useJooby(() => context.serialised())
const state = controller.state

const widget = useJoobyWidgetStore()
const { open } = storeToRefs(widget)
let loaded = false

function remember(id: number | null): void {
  try {
    if (id === null) localStorage.removeItem(context.storageKey())
    else localStorage.setItem(context.storageKey(), String(id))
  } catch {
    return
  }
}

function recall(): number | null {
  try {
    const stored = localStorage.getItem(context.storageKey())

    return stored ? Number(stored) : null
  } catch {
    return null
  }
}

watch(
  () => state.conversationId,
  (id) => {
    if (id !== null) remember(id)
  },
)

async function restart(): Promise<void> {
  remember(null)
  await guard(() => controller.newConversation(true))
}

async function load(): Promise<void> {
  if (loaded) return

  loaded = true

  const stored = recall()

  if (stored === null) {
    await guard(() => controller.newConversation(true))
    return
  }

  try {
    await controller.openConversation(stored, true)
  } catch {
    await restart()
  }
}

watch(open, (isOpen) => {
  if (isOpen) void load()
})

async function close(): Promise<void> {
  widget.close()
  await nextTick()
  document.getElementById('jooby-launcher')?.focus()
}

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape' && open.value) void close()
}

onMounted(() => document.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => document.removeEventListener('keydown', onKeydown))
</script>

<template>
  <aside
    v-show="open"
    id="jooby-widget-panel"
    :aria-label="t('askJooby.widgetTitle')"
    class="z-40 flex w-[28rem] shrink-0 flex-col gap-2 border-s border-base-300 bg-base-100 p-3 max-lg:fixed max-lg:end-0 max-lg:top-16 max-lg:bottom-0 max-lg:w-[min(28rem,100vw)] max-lg:shadow-lg"
    data-testid="widget-panel"
  >
    <div class="flex items-center justify-between">
      <h2 class="font-semibold">{{ t('askJooby.widgetTitle') }}</h2>
      <div class="flex gap-1">
        <button
          type="button"
          class="btn btn-ghost btn-xs btn-square"
          :aria-label="t('askJooby.newConversation')"
          @click="restart"
        >
          <Plus aria-hidden="true" class="size-4" />
        </button>
        <button
          type="button"
          class="btn btn-ghost btn-xs btn-square"
          :aria-label="t('askJooby.closeChat')"
          @click="close"
        >
          <X aria-hidden="true" class="size-4" />
        </button>
      </div>
    </div>
    <div class="min-h-0 flex-1">
      <JoobyChat
        :controller="controller"
        :landlord="landlord"
        :offline="offline"
        :attachments="false"
      />
    </div>
  </aside>
</template>
