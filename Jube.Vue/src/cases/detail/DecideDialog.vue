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
import { useTemplateRef, watch, type Component } from 'vue'
import { useI18n } from 'vue-i18n'
import { X } from 'lucide-vue-next'

const props = defineProps<{ open: boolean; title: string; icon: Component; testid: string }>()

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const dialog = useTemplateRef<HTMLDialogElement>('dialog')

watch(
  () => props.open,
  (open) => {
    if (open) dialog.value?.showModal()
    else dialog.value?.close?.()
  },
  { flush: 'post' },
)
</script>

<template>
  <dialog ref="dialog" class="modal" :data-testid="testid" @cancel.prevent="emit('close')">
    <div class="modal-box flex max-w-lg flex-col gap-4 p-0">
      <header
        class="flex items-center gap-3 border-b border-base-300 border-t-2 border-t-accent px-5 py-3"
      >
        <span class="grid size-9 place-items-center rounded-field bg-base-200">
          <component :is="icon" class="size-5" aria-hidden="true" />
        </span>
        <h3 class="flex-1 text-base font-semibold">{{ title }}</h3>
        <button
          type="button"
          class="btn btn-sm btn-ghost btn-circle"
          :aria-label="t('caseDetail.decide.cancel')"
          @click="emit('close')"
        >
          <X class="size-4" aria-hidden="true" />
        </button>
      </header>
      <div v-if="open" key="body" class="flex flex-col gap-4 px-5 pb-5">
        <slot></slot>
      </div>
    </div>
    <div class="modal-backdrop">
      <button
        type="button"
        tabindex="-1"
        :aria-label="t('caseDetail.decide.cancel')"
        @click="emit('close')"
      ></button>
    </div>
  </dialog>
</template>
