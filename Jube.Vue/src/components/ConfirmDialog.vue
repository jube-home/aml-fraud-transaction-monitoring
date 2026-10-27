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
import { useTemplateRef, watch } from 'vue'

const props = defineProps<{
  open: boolean
  title: string
  body: string
  confirmLabel: string
  cancelLabel: string
  id: string
}>()

const emit = defineEmits<{ confirm: []; cancel: [] }>()

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
  <dialog ref="dialog" class="modal" :data-testid="`${id}-dialog`" @cancel.prevent="emit('cancel')">
    <div class="modal-box">
      <h3 class="text-lg font-bold">{{ title }}</h3>
      <p class="py-4">{{ body }}</p>
      <div class="modal-action">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`${id}-cancel`"
          @click="emit('cancel')"
        >
          {{ cancelLabel }}
        </button>
        <button
          type="button"
          class="btn btn-sm btn-error"
          :data-testid="`${id}-confirm`"
          @click="emit('confirm')"
        >
          {{ confirmLabel }}
        </button>
      </div>
    </div>
    <div class="modal-backdrop">
      <button
        type="button"
        tabindex="-1"
        :aria-label="cancelLabel"
        @click="emit('cancel')"
      ></button>
    </div>
  </dialog>
</template>
