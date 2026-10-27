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
import { ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import FileUpload from '../../components/FileUpload.vue'

const { t } = useI18n()
const extensionsId = useId()
const failId = useId()

const extensions = ref('.csv,.json')
const fails = ref(false)
const result = ref<{ kind: 'uploaded' | 'failed'; text: string } | null>(null)

function parsed(): string[] {
  return extensions.value
    .split(',')
    .map((entry) => entry.trim())
    .filter((entry) => entry !== '')
}

async function upload(file: File): Promise<void> {
  if (fails.value) throw new Error('bench')

  result.value = { kind: 'uploaded', text: `${file.name} ${file.size}` }
}

function failed(): void {
  result.value = { kind: 'failed', text: '' }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm" :for="extensionsId">
        <span class="label">{{ t('uploadBench.extensions') }}</span>
        <input
          :id="extensionsId"
          v-model="extensions"
          type="text"
          class="input input-sm font-mono"
          data-testid="bench-extensions"
        />
      </label>
      <label class="flex items-center gap-3 text-sm" :for="failId">
        <input
          :id="failId"
          v-model="fails"
          type="checkbox"
          class="checkbox checkbox-sm"
          data-testid="bench-fails"
        />
        <span>{{ t('uploadBench.fails') }}</span>
      </label>
    </div>
    <FileUpload
      :label="t('uploadBench.file')"
      :extensions="parsed()"
      :upload="upload"
      @failed="failed"
    />
    <p
      v-if="result?.kind === 'uploaded'"
      key="uploaded"
      role="status"
      class="text-sm"
      data-testid="bench-uploaded"
    >
      {{ t('uploadBench.uploaded') }}
      <span class="badge badge-sm font-mono">{{ result.text }}</span>
    </p>
    <p
      v-else-if="result?.kind === 'failed'"
      key="failed"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="bench-failed"
    >
      {{ t('uploadBench.failed') }}
    </p>
  </div>
</template>
