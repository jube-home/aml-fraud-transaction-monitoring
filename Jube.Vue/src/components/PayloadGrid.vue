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
import { computed, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { RefreshCw, Search } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { gridModelApi } from '../gridModel/api'
import { defaultState, requestParams, withRatios } from '../gridModel/state'
import type { GridModel, GridModelColumn, GridState } from '../gridModel/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn, GridSort } from './dataGridTypes'
import GridExportButtons from './GridExportButtons.vue'
import GridStatistics from './GridStatistics.vue'
import PanelCard from './PanelCard.vue'
import PayloadFilterSelect from './PayloadFilterSelect.vue'

const props = defineProps<{ area: string }>()

const { t, locale } = useI18n()
const { dateTime, fixed, number } = useFormat()
const pagerLabels = usePagerLabels()
const grid = useTemplateRef<{
  downloadCsv: (filename: string) => void
  downloadXlsx: (filename: string, sheetName?: string) => Promise<void>
}>('grid')

const direction = computed(() => directionFor(locale.value))

const model = useQuery({
  queryKey: computed(() => queryKeys.gridModel(props.area)),
  queryFn: () => gridModelApi.model(props.area),
  retry: false,
})

const modelValue = computed(() => model.data.value)

const draft = ref<GridState>()
const applied = ref<GridState>()

function start(next: GridModel): GridState {
  if (!draft.value) {
    draft.value = defaultState(next)
    applied.value = { ...draft.value, filters: { ...draft.value.filters } }
  }

  return applied.value!
}

const params = computed(() =>
  modelValue.value ? requestParams(modelValue.value, start(modelValue.value)) : {},
)

const result = useQuery({
  queryKey: computed(() => queryKeys.gridRows(props.area, params.value)),
  queryFn: () => gridModelApi.rows(modelValue.value!, applied.value!),
  enabled: computed(() => modelValue.value !== undefined && applied.value !== undefined),
  retry: false,
})

const shown = computed(() => modelValue.value?.columns.filter((column) => !column.hidden) ?? [])
const rows = computed(() =>
  withRatios(result.data.value?.rows ?? [], modelValue.value?.columns ?? []),
)
const total = computed(() => result.data.value?.total ?? 0)

function labelOf(column: GridModelColumn): string {
  return column.unit ? `${column.title} (${column.unit})` : column.title
}

function formatterOf(column: GridModelColumn): ((value: unknown) => string) | undefined {
  switch (column.format) {
    case 'dateTime':
      return (value) => (value ? dateTime(String(value)) : '')
    case 'integer':
      return (value) =>
        typeof value === 'number' ? number(Math.round(value)) : String(value ?? '')
    case 'decimal':
      return (value) => (typeof value === 'number' ? fixed(value) : String(value ?? ''))
    case 'percent':
      return (value) =>
        typeof value === 'number' ? `${fixed(value * 100, 1)}%` : String(value ?? '')
    default:
      return undefined
  }
}

const columns = computed<GridColumn[]>(() =>
  shown.value.map((column) => {
    const numeric = column.type === 'integer' || column.type === 'number'

    return {
      field: column.field,
      title: labelOf(column),
      minWidth: column.type === 'datetime' ? 180 : 120,
      mono: numeric || column.format === 'code' || column.type === 'datetime',
      align: numeric ? 'end' : undefined,
      type: numeric ? 'number' : column.type === 'datetime' ? 'date' : 'text',
      format: formatterOf(column),
    }
  }),
)

const labels = computed(() =>
  Object.fromEntries(
    (modelValue.value?.columns ?? []).map((column) => [column.field, labelOf(column)]),
  ),
)

const sort = computed<GridSort | undefined>(() => applied.value?.sort)

function apply(): void {
  if (!draft.value) return

  applied.value = {
    ...draft.value,
    filters: { ...draft.value.filters },
    sort: applied.value?.sort ?? draft.value.sort,
  }
}

function clear(): void {
  if (!modelValue.value) return

  const fresh = defaultState(modelValue.value)

  draft.value = fresh
  applied.value = { ...fresh, filters: {} }
}

function onSort(next: GridSort | undefined): void {
  if (!applied.value || !draft.value) return

  const same = next?.field === applied.value.sort?.field && next?.dir === applied.value.sort?.dir

  if (same) return

  applied.value = { ...applied.value, sort: next }
  draft.value = { ...draft.value, sort: next }
}

function setFilter(param: string, value: string): void {
  if (draft.value)
    draft.value = { ...draft.value, filters: { ...draft.value.filters, [param]: value } }
}

function exportCsv(): void {
  grid.value?.downloadCsv(`${props.area}.csv`)
}

function exportExcel(): void {
  void grid.value?.downloadXlsx(`${props.area}.xlsx`, modelValue.value?.title ?? 'Data')
}
</script>

<template>
  <div
    v-if="model.isError.value"
    role="alert"
    class="alert alert-error"
    data-testid="payload-model-error"
  >
    {{ t('payloadGrid.failed') }}
  </div>
  <div
    v-else-if="!modelValue"
    class="skeleton h-40 w-full"
    role="status"
    data-testid="payload-loading"
  ></div>

  <PanelCard v-else :title="modelValue.title" flush :data-testid="`payload-grid-${area}`">
    <form
      class="flex flex-wrap items-end gap-3 border-b border-base-300 p-3"
      :aria-label="t('payloadGrid.filters')"
      @submit.prevent="apply"
    >
      <template v-if="modelValue.timeField && draft">
        <label class="flex flex-col gap-1">
          <span class="label text-xs font-medium text-secondary">{{ t('payloadGrid.from') }}</span>
          <input
            v-model="draft.from"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="payload-from"
          />
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs font-medium text-secondary">{{ t('payloadGrid.to') }}</span>
          <input
            v-model="draft.to"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="payload-to"
          />
        </label>
      </template>

      <PayloadFilterSelect
        v-for="filter in modelValue.filters"
        :key="filter.param"
        :filter="filter"
        :model-value="draft?.filters[filter.param] ?? ''"
        @update:model-value="setFilter(filter.param, $event)"
      />

      <label v-if="modelValue.search && draft" class="flex flex-col gap-1">
        <span class="label text-xs font-medium text-secondary">{{ modelValue.search.label }}</span>
        <span class="input input-sm min-w-56">
          <Search aria-hidden="true" class="size-4 shrink-0 opacity-70" />
          <input
            v-model="draft.search"
            type="search"
            class="grow"
            :aria-label="modelValue.search.label"
            data-testid="payload-search"
          />
        </span>
      </label>

      <div class="ms-auto flex flex-wrap items-center gap-2">
        <button type="submit" class="btn btn-sm btn-primary" data-testid="payload-apply">
          {{ t('payloadGrid.apply') }}
        </button>
        <button
          type="button"
          class="btn btn-sm btn-ghost"
          data-testid="payload-clear"
          @click="clear"
        >
          {{ t('payloadGrid.clear') }}
        </button>
        <button
          type="button"
          class="btn btn-sm btn-square btn-ghost"
          :aria-label="t('payloadGrid.refresh')"
          data-testid="payload-refresh"
          @click="result.refetch()"
        >
          <RefreshCw
            aria-hidden="true"
            :class="['size-4', result.isFetching.value ? 'animate-spin' : '']"
          />
        </button>
        <GridExportButtons :disabled="rows.length === 0" @csv="exportCsv" @excel="exportExcel" />
      </div>
    </form>

    <div class="flex flex-col gap-3 p-3">
      <p
        v-if="result.isError.value"
        key="error"
        role="alert"
        class="alert alert-error"
        data-testid="payload-error"
      >
        {{ t('payloadGrid.failed') }}
        <button type="button" class="btn btn-sm" @click="result.refetch()">
          {{ t('payloadGrid.retry') }}
        </button>
      </p>
      <div
        v-else-if="result.isPending.value"
        key="pending"
        class="skeleton h-40 w-full"
        role="status"
        :aria-label="t('payloadGrid.loading')"
      ></div>
      <template v-else>
        <p
          class="font-mono text-xs text-secondary tabular-nums"
          role="status"
          data-testid="payload-count"
        >
          {{
            total > rows.length
              ? t('payloadGrid.capped', { count: number(rows.length), total: number(total) })
              : t('payloadGrid.rows', { count: number(rows.length), total: number(total) })
          }}
        </p>

        <details
          v-if="
            result.data.value?.statistics?.columns &&
            Object.keys(result.data.value.statistics.columns).length > 0
          "
          class="collapse collapse-arrow border border-base-300 bg-base-200"
          data-testid="payload-statistics"
        >
          <summary class="collapse-title text-sm font-semibold text-primary">
            {{ t('payloadGrid.statistics') }}
          </summary>
          <div class="collapse-content">
            <GridStatistics :statistics="result.data.value?.statistics" :labels="labels" />
          </div>
        </details>

        <DataGrid
          ref="grid"
          :columns="columns"
          :rows="rows"
          :direction="direction"
          :page-size="100"
          :pager-labels="pagerLabels"
          :empty-text="t('payloadGrid.empty')"
          :label="modelValue.title"
          :sort="sort"
          @sort="onSort"
        />
      </template>
    </div>
  </PanelCard>
</template>
