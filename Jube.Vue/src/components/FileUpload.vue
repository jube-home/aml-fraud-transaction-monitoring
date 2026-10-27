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
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { uploadProblem, type UploadProblem } from '../upload/file'

const props = withDefaults(
  defineProps<{
    label: string
    extensions?: string[]
    upload: (file: File) => Promise<unknown>
    disabled?: boolean
  }>(),
  { extensions: () => [], disabled: false },
)

const emit = defineEmits<{ uploaded: [filename: string]; failed: [error: unknown] }>()

const { t } = useI18n()
const id = useId()

const file = ref<File | null>(null)
const problem = ref<UploadProblem | null>(null)
const busy = ref(false)
const input = ref<HTMLInputElement | null>(null)

const accept = computed(() => props.extensions.join(','))

const problemText = computed(() => {
  switch (problem.value) {
    case 'none':
      return t('upload.problem.none')
    case 'type':
      return t('upload.problem.type', { types: props.extensions.join(', ') })
    case 'empty':
      return t('upload.problem.empty')
    default:
      return ''
  }
})

function choose(event: Event): void {
  file.value = (event.target as HTMLInputElement).files?.[0] ?? null
  problem.value = null
}

async function send(): Promise<void> {
  problem.value = uploadProblem(file.value, props.extensions)

  if (problem.value || !file.value) return

  const chosen = file.value

  busy.value = true

  try {
    await props.upload(chosen)
    emit('uploaded', chosen.name)
    file.value = null

    if (input.value) input.value.value = ''
  } catch (error) {
    emit('failed', error)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="file-upload">
    <label class="flex flex-col gap-1" :for="id">
      <span class="label text-xs">{{ label }}</span>
      <input
        :id="id"
        ref="input"
        type="file"
        class="file-input file-input-sm w-full max-w-sm"
        :accept="accept"
        :disabled="disabled || busy"
        data-testid="file-upload-input"
        @change="choose"
      />
    </label>
    <p
      v-if="problem"
      key="problem"
      class="text-sm text-error"
      role="alert"
      data-testid="file-upload-problem"
    >
      {{ problemText }}
    </p>
    <div>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="disabled || busy"
        :aria-busy="busy"
        data-testid="file-upload-send"
        @click="send"
      >
        <span
          v-if="busy"
          key="busy"
          class="loading loading-spinner loading-xs"
          aria-hidden="true"
        ></span>
        {{ busy ? t('upload.sending') : t('upload.send') }}
      </button>
    </div>
  </div>
</template>
