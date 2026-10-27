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
import { useMutation } from '@tanstack/vue-query'
import { Play } from 'lucide-vue-next'
import { usePagerLabels } from '../composables/usePagerLabels'
import DatasourceSqlEditor from '../datasource/DatasourceSqlEditor.vue'
import { useToasts } from '../stores/toasts'
import { caseSqlApi } from './caseSqlApi'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

const props = defineProps<{ modelValue: string; locked?: boolean }>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const { t } = useI18n()
const toasts = useToasts()
const pagerLabels = usePagerLabels()

const caseId = ref('')

const run = useMutation({
  mutationFn: () => caseSqlApi.test(props.modelValue, Number(caseId.value)),
  onError: (error) => toasts.apiError(error),
})

const result = computed(() => run.data.value)
const canRun = computed(
  () =>
    props.modelValue.trim() !== '' &&
    Number.isInteger(Number(caseId.value)) &&
    Number(caseId.value) > 0 &&
    !run.isPending.value,
)
const columns = computed<GridColumn[]>(() =>
  (result.value?.columns ?? []).map((column) => ({ field: column.name, title: column.name })),
)
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="case-sql-editor">
    <div class="flex flex-col gap-1">
      <span class="label text-sm">{{ t('caseSql.label') }}</span>
      <p class="text-xs text-base-content/70">{{ t('caseSql.hint') }}</p>
    </div>
    <DatasourceSqlEditor
      :model-value="modelValue"
      :label="t('caseSql.label')"
      :readonly="locked"
      @update:model-value="emit('update:modelValue', $event)"
    />
    <div class="flex flex-wrap items-end gap-3">
      <fieldset class="fieldset">
        <label class="label" for="case-sql-case-id">{{ t('caseSql.caseId') }}</label>
        <input
          id="case-sql-case-id"
          v-model="caseId"
          type="number"
          min="1"
          class="input input-sm w-40 font-mono"
          data-testid="case-sql-case-id"
        />
      </fieldset>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!canRun"
        data-testid="case-sql-run"
        @click="run.mutate()"
      >
        <Play aria-hidden="true" class="size-4" />
        {{ t('caseSql.run') }}
      </button>
    </div>
    <p
      v-if="modelValue.trim() !== '' && caseId === ''"
      key="nocase"
      class="text-xs text-base-content/70"
      data-testid="case-sql-nocase"
    >
      {{ t('caseSql.noCase') }}
    </p>
    <div
      v-if="result && !result.ok && result.error"
      key="error"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="case-sql-error"
    >
      <span>{{ result.error }}</span>
    </div>
    <div v-else-if="result && result.ok" key="rows" class="flex min-w-0 flex-col gap-2">
      <p class="font-mono text-xs text-base-content/70" data-testid="case-sql-summary">
        {{ t('caseSql.summary', { count: result.rowCount, ms: result.milliseconds }) }}
        <span v-if="result.truncated">{{ t('caseSql.truncated') }}</span>
      </p>
      <DataGrid
        :columns="columns"
        :rows="result.rows"
        :empty-text="t('caseSql.empty')"
        :label="t('caseSql.grid')"
        :page-size="10"
        :pager-labels="pagerLabels"
      />
    </div>
  </div>
</template>
