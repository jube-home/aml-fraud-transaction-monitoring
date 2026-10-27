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
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { Plus, Search, Trash2 } from 'lucide-vue-next'
import { joobyApi } from '../../askjooby/api'
import { relativeTime } from '../../askjooby/format'
import { conversationsKey } from './useJooby'

const props = defineProps<{ selectedId: number | null; canCreate: boolean }>()
const emit = defineEmits<{ select: [id: number]; create: []; remove: [id: number] }>()

const { t } = useI18n()
const search = ref('')
const debounced = ref('')
const pendingDelete = ref<number>()
const dialog = ref<HTMLDialogElement>()

let timer: ReturnType<typeof setTimeout> | undefined

watch(search, (value) => {
  clearTimeout(timer)
  timer = setTimeout(() => {
    debounced.value = value
  }, 250)
})

const { data: conversations } = useQuery({
  queryKey: [...conversationsKey, debounced],
  queryFn: () => joobyApi.list(debounced.value),
  staleTime: 0,
})

function askDelete(id: number): void {
  pendingDelete.value = id
  dialog.value?.showModal()
}

function confirmDelete(): void {
  if (pendingDelete.value !== undefined) emit('remove', pendingDelete.value)

  dialog.value?.close()
}
</script>

<template>
  <section
    :aria-label="t('askJooby.conversations')"
    class="flex h-full min-h-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 p-3"
  >
    <button
      type="button"
      class="btn btn-primary btn-sm"
      :disabled="!props.canCreate"
      data-testid="new-conversation"
      @click="emit('create')"
    >
      <Plus aria-hidden="true" class="size-4" />
      {{ t('askJooby.newConversation') }}
    </button>
    <label class="input input-sm w-full">
      <Search aria-hidden="true" class="size-4 opacity-70" />
      <input
        v-model="search"
        type="search"
        class="grow"
        :placeholder="t('askJooby.searchConversations')"
        :aria-label="t('askJooby.searchConversations')"
      />
    </label>
    <ul
      class="menu min-h-0 w-full flex-1 flex-nowrap overflow-y-auto p-0"
      data-testid="conversations"
    >
      <li v-for="conversation in conversations ?? []" :key="conversation.id">
        <div class="flex items-center gap-1 p-0">
          <button
            type="button"
            class="flex min-w-0 flex-1 flex-col items-start gap-0 px-3 py-2 text-start"
            :class="{
              'menu-active bg-base-200! font-semibold text-base-content!':
                conversation.id === props.selectedId,
            }"
            :aria-current="conversation.id === props.selectedId ? 'true' : undefined"
            @click="emit('select', conversation.id)"
          >
            <span class="w-full truncate">{{ conversation.title || t('askJooby.untitled') }}</span>
            <span class="badge badge-ghost badge-xs font-mono">{{
              relativeTime(conversation.updatedDate ?? conversation.createdDate)
            }}</span>
          </button>
          <button
            type="button"
            class="btn btn-ghost btn-xs btn-square"
            :aria-label="t('askJooby.deleteConversation')"
            @click="askDelete(conversation.id)"
          >
            <Trash2 aria-hidden="true" class="size-4" />
          </button>
        </div>
      </li>
    </ul>
    <dialog ref="dialog" class="modal">
      <div class="modal-box">
        <h2 class="text-lg font-semibold">{{ t('askJooby.deleteTitle') }}</h2>
        <p class="py-4">{{ t('askJooby.deleteBody') }}</p>
        <div class="modal-action">
          <form method="dialog" class="flex gap-2">
            <button class="btn btn-sm">{{ t('askJooby.cancel') }}</button>
            <button
              type="button"
              class="btn btn-error btn-sm"
              data-testid="confirm-delete"
              @click="confirmDelete"
            >
              {{ t('askJooby.delete') }}
            </button>
          </form>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button>{{ t('askJooby.cancel') }}</button>
      </form>
    </dialog>
  </section>
</template>
