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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ApiError } from '../api/useApi'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { exhaustiveApi } from '../exhaustive/api'
import { batchErrorText, batchNotes } from '../exhaustive/model'
import type { BatchResult } from '../exhaustive/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import FileUpload from './FileUpload.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ guid: string }>()

const { t, locale } = useI18n()
const { fixed, number } = useFormat()

const direction = computed(() => directionFor(locale.value))
const result = ref<BatchResult | null>(null)
const problems = ref<string[]>([])

async function upload(file: File): Promise<void> {
  problems.value = []
  result.value = await exhaustiveApi.batch(props.guid, file)
}

function failed(error: unknown): void {
  result.value = null
  problems.value = [
    error instanceof ApiError
      ? batchErrorText(error.body, t('exhaustive.batch.failed'))
      : t('exhaustive.batch.failed'),
  ]
}

const columns = computed<GridColumn[]>(() => [
  { field: 'rowNumber', title: t('exhaustive.batch.row'), align: 'end', mono: true, width: 90 },
  {
    field: 'score',
    title: t('exhaustive.batch.score'),
    align: 'end',
    mono: true,
    width: 120,
    format: (value) => (typeof value === 'number' ? fixed(value, 4) : ''),
  },
  {
    field: 'baseValue',
    title: t('exhaustive.batch.baseValue'),
    align: 'end',
    mono: true,
    width: 130,
    format: (value) => (typeof value === 'number' ? fixed(value, 4) : ''),
  },
  { field: 'error', title: t('exhaustive.batch.error'), minWidth: 240 },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="exhaustive-batch">
    <h4 class="text-sm font-semibold">{{ t('exhaustive.batch.title') }}</h4>
    <p class="max-w-prose text-xs text-secondary">{{ t('exhaustive.batch.hint') }}</p>
    <FileUpload
      :label="t('exhaustive.batch.file')"
      :extensions="['.csv', '.txt', '.json']"
      :upload="upload"
      @failed="failed"
    />
    <ProblemAlert
      v-if="problems.length > 0"
      key="problems"
      :reasons="problems"
      data-testid="batch-problems"
    />
    <template v-if="result">
      <p class="text-sm" role="status" data-testid="batch-summary">
        {{
          t('exhaustive.batch.summary', {
            rows: number(result.rowCount),
            scored: number(result.scoredCount),
          })
        }}
      </p>
      <p v-for="note in batchNotes(result)" :key="note" class="text-xs" data-testid="batch-note">
        {{ note }}
      </p>
      <DataGrid
        :columns="columns"
        :rows="result.rows"
        :direction="direction"
        :empty-text="t('exhaustive.batch.empty')"
        :label="t('exhaustive.batch.title')"
        row-key="rowNumber"
        :page-size="25"
        auto-height
      />
    </template>
  </div>
</template>
