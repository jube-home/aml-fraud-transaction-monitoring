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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ApiDownload } from '../api/useApi'
import { filenameFromDisposition, saveBlob } from '../download/file'

const props = withDefaults(
  defineProps<{
    label: string
    fetcher: () => Promise<ApiDownload>
    fallbackName?: string
    disabled?: boolean
    guard?: () => boolean
  }>(),
  { fallbackName: 'download', disabled: false, guard: () => true },
)

const emit = defineEmits<{ saved: [filename: string]; failed: [error: unknown] }>()

const { t } = useI18n()
const busy = ref(false)

async function run(): Promise<void> {
  if (!props.guard()) return

  busy.value = true

  try {
    const { blob, disposition } = await props.fetcher()

    if (blob.size === 0) throw new Error('empty')

    const filename = filenameFromDisposition(disposition, props.fallbackName)

    saveBlob(blob, filename)
    emit('saved', filename)
  } catch (error) {
    emit('failed', error)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <button
    type="button"
    class="btn btn-sm btn-primary"
    :disabled="disabled || busy"
    :aria-busy="busy"
    data-testid="download-button"
    @click="run"
  >
    <span
      v-if="busy"
      key="busy"
      class="loading loading-spinner loading-xs"
      aria-hidden="true"
    ></span>
    {{ busy ? t('download.preparing') : label }}
  </button>
</template>
