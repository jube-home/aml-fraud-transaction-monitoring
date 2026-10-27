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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useExpandHosts } from '../composables/useExpandHosts'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { exhaustiveApi } from '../exhaustive/api'
import { correlationTone, variableLabel } from '../exhaustive/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ExhaustiveVariableDetail from './ExhaustiveVariableDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ instanceId: number; version: string }>()

const { t, locale } = useI18n()
const { fixed, number } = useFormat()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))
const search = ref('')
const counts = ref(false)

const variables = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'variables', props.version)),
  queryFn: () => exhaustiveApi.variables(props.instanceId),
  retry: false,
})

const rows = computed(() =>
  (variables.data.value ?? []).map((variable) => ({ ...variable, label: variableLabel(variable) })),
)

const figure = (value: unknown) => (typeof value === 'number' ? fixed(value, 3) : '')

const columns = computed<GridColumn[]>(() => [
  { field: 'label', title: t('exhaustive.variables.variable'), minWidth: 220 },
  {
    field: 'correlation',
    title: t('exhaustive.variables.correlation'),
    align: 'end',
    mono: true,
    width: 130,
    badge: (value) => ({ label: fixed(Number(value), 3), tone: correlationTone(Number(value)) }),
  },
  {
    field: 'mean',
    title: t('exhaustive.variables.mean'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'standardDeviation',
    title: t('exhaustive.variables.sd'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'minimum',
    title: t('exhaustive.variables.min'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'maximum',
    title: t('exhaustive.variables.max'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'kurtosis',
    title: t('exhaustive.variables.kurtosis'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'skewness',
    title: t('exhaustive.variables.skewness'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'iqr',
    title: t('exhaustive.variables.iqr'),
    align: 'end',
    mono: true,
    width: 110,
    format: figure,
  },
  {
    field: 'distinctValues',
    title: t('exhaustive.variables.distinct'),
    align: 'end',
    mono: true,
    width: 110,
    format: (value) => (typeof value === 'number' ? number(value) : ''),
  },
  { field: 'normalisationType', title: t('exhaustive.variables.normalisation'), width: 150 },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="exhaustive-variables">
    <div class="flex flex-wrap items-end gap-4">
      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('exhaustive.variables.search') }}</span>
        <input
          v-model="search"
          type="search"
          class="input input-sm w-64"
          data-testid="exhaustive-variable-search"
        />
      </label>
      <label class="flex items-center gap-2 text-sm">
        <input
          v-model="counts"
          type="checkbox"
          class="toggle toggle-sm"
          data-testid="exhaustive-variable-counts"
        />
        <span>{{ t('exhaustive.variables.counts') }}</span>
      </label>
    </div>
    <p class="text-xs text-secondary">{{ t('exhaustive.variables.classesHint') }}</p>

    <ProblemAlert
      v-if="variables.isError.value"
      key="failed"
      :reasons="[t('exhaustive.variables.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('exhaustive.variables.empty')"
      :label="t('exhaustive.variables.title')"
      filter-field="label"
      :filter-text="search"
      :page-size="25"
      expandable
      auto-height
      :expand-labels="{
        expand: t('exhaustive.variables.show'),
        collapse: t('exhaustive.variables.hide'),
      }"
      @expand="onExpand"
    />
    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <ExhaustiveVariableDetail
          :instance-id="instanceId"
          :variable-id="Number(id)"
          :counts="counts"
          :version="version"
        />
      </Teleport>
    </template>
  </div>
</template>
