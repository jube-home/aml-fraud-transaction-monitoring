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
import DownloadButton from '../../components/DownloadButton.vue'
import type { ApiDownload } from '../../api/useApi'

type Outcome = 'file' | 'named' | 'empty' | 'fails'

const { t } = useI18n()
const selectId = useId()

const outcome = ref<Outcome>('file')
const result = ref<'saved' | 'failed' | null>(null)
const savedName = ref('')

function fetcher(): Promise<ApiDownload> {
  switch (outcome.value) {
    case 'fails':
      return Promise.reject(new Error('bench'))
    case 'empty':
      return Promise.resolve({ blob: new Blob([]), disposition: null })
    case 'named':
      return Promise.resolve({
        blob: new Blob(['bench']),
        disposition: 'attachment; filename="named-by-server.txt"',
      })
    default:
      return Promise.resolve({ blob: new Blob(['bench']), disposition: null })
  }
}

function saved(filename: string): void {
  result.value = 'saved'
  savedName.value = filename
}

function failed(): void {
  result.value = 'failed'
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm" :for="selectId">
        <span class="label">{{ t('downloadBench.outcome') }}</span>
        <select
          :id="selectId"
          v-model="outcome"
          class="select select-sm"
          data-testid="bench-outcome"
        >
          <option value="file">{{ t('downloadBench.file') }}</option>
          <option value="named">{{ t('downloadBench.named') }}</option>
          <option value="empty">{{ t('downloadBench.empty') }}</option>
          <option value="fails">{{ t('downloadBench.fails') }}</option>
        </select>
      </label>
      <DownloadButton
        :label="t('downloadBench.download')"
        :fetcher="fetcher"
        fallback-name="bench.txt"
        @saved="saved"
        @failed="failed"
      />
    </div>
    <p
      v-if="result === 'saved'"
      key="saved"
      role="status"
      class="text-sm"
      data-testid="bench-saved"
    >
      {{ t('downloadBench.saved') }}
      <span class="badge badge-sm font-mono">{{ savedName }}</span>
    </p>
    <p
      v-else-if="result === 'failed'"
      key="failed"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="bench-failed"
    >
      {{ t('downloadBench.failed') }}
    </p>
  </div>
</template>
