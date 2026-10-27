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
import { RefreshCw, Search } from 'lucide-vue-next'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import DataGrid from '../components/DataGrid.vue'
import type { GridActionEvent, GridColumn } from '../components/dataGridTypes'
import { directionFor } from '../i18n'
import { usePagerLabels } from '../composables/usePagerLabels'
import { useExpandHosts } from '../composables/useExpandHosts'
import { useFormat } from '../composables/useFormat'
import { dashboardApi } from './api'
import VisualisationDirectoryDetail from './VisualisationDirectoryDetail.vue'
import { listedEntries } from './url'
import type { DirectoryEntry } from './types'

const emit = defineEmits<{ open: [guid: string] }>()

const { t, locale } = useI18n()
const pagerLabels = usePagerLabels()

const search = ref('')
const { dateTime } = useFormat()
const { hosts, onExpand } = useExpandHosts()

const directory = useQuery({
  queryKey: queryKeys.visualisationDirectory(),
  queryFn: () => dashboardApi.directory(),
  retry: false,
})

const summary = useQuery({
  queryKey: queryKeys.visualisationSummary(),
  queryFn: () => dashboardApi.summary(),
  retry: false,
})

const windowDays = computed(() => summary.data.value?.[0]?.windowDays ?? 30)

const rows = computed(() =>
  listedEntries(directory.data.value ?? []).map((entry) => {
    const found = summary.data.value?.find((candidate) => candidate.registryId === entry.id)

    return {
      ...entry,
      tiles: found?.datasourceCount,
      parameters: found?.parameterCount,
      lastRun: found?.lastExecutionDate ?? null,
      runBy: found?.lastExecutionUser ?? null,
      runs: found?.executionsInWindow,
      failed: found?.failuresInWindow,
      average: found?.averageResponseMilliseconds ?? null,
    }
  }),
)

function rowOf(guid: unknown): DirectoryEntry | undefined {
  return rows.value.find((row) => row.guid === guid)
}

const unavailable = computed(() => t('visualisation.directory.unavailable'))

const shown = computed(() => {
  const needle = search.value.trim().toLowerCase()

  return needle === ''
    ? rows.value.length
    : rows.value.filter((row) => (row.name ?? '').toLowerCase().includes(needle)).length
})

const forbidden = computed(
  () => directory.error.value instanceof ApiError && directory.error.value.kind === 'forbidden',
)

const direction = computed(() => directionFor(locale.value))

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('visualisation.directory.col.name'), minWidth: 220 },
  {
    field: 'tiles',
    title: t('visualisation.directory.col.tiles'),
    mono: true,
    align: 'end',
    width: 90,
    format: (value) => (typeof value === 'number' ? String(value) : unavailable.value),
  },
  {
    field: 'parameters',
    title: t('visualisation.directory.col.parameters'),
    mono: true,
    align: 'end',
    width: 120,
    format: (value) => (typeof value === 'number' ? String(value) : unavailable.value),
  },
  {
    field: 'lastRun',
    title: t('visualisation.directory.col.lastRun'),
    mono: true,
    width: 190,
    format: (value) =>
      value
        ? dateTime(String(value))
        : summary.isSuccess.value
          ? t('visualisation.directory.neverRun')
          : unavailable.value,
  },
  {
    field: 'runBy',
    title: t('visualisation.directory.col.runBy'),
    width: 130,
    format: (value) => (typeof value === 'string' && value !== '' ? value : ''),
  },
  {
    field: 'runs',
    title: t('visualisation.directory.col.runs', { days: windowDays.value }),
    mono: true,
    align: 'end',
    width: 120,
    format: (value) => (typeof value === 'number' ? String(value) : unavailable.value),
  },
  {
    field: 'failed',
    title: t('visualisation.directory.col.failed', { days: windowDays.value }),
    width: 130,
    badge: (value) =>
      typeof value !== 'number'
        ? { label: unavailable.value, tone: 'neutral' }
        : value > 0
          ? { label: String(value), tone: 'error' }
          : { label: '0', tone: 'neutral' },
  },
  {
    field: 'average',
    title: t('visualisation.directory.col.average'),
    mono: true,
    align: 'end',
    width: 120,
    format: (value) =>
      typeof value === 'number'
        ? t('visualisation.directory.ms', { count: Math.round(value) })
        : summary.isSuccess.value
          ? ''
          : unavailable.value,
  },
  {
    field: '',
    title: t('crud.actions'),
    width: 110,
    pinned: true,
    actions: () => [{ id: 'open', label: t('visualisation.directory.open') }],
  },
])

function onAction(event: GridActionEvent): void {
  if (event.id === 'open') emit('open', (event.row as DirectoryEntry).guid)
}
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="visualisation-directory">
    <div class="flex flex-wrap items-center gap-3">
      <label class="input input-sm w-full max-w-xs">
        <Search class="size-4 opacity-60" aria-hidden="true" />
        <input
          v-model="search"
          type="search"
          :placeholder="t('visualisation.directory.searchPlaceholder')"
          :aria-label="t('visualisation.directory.search')"
          data-testid="directory-search"
        />
      </label>
      <span class="badge badge-sm badge-neutral font-mono" data-testid="directory-count">{{
        t('visualisation.directory.count', { count: shown })
      }}</span>
      <button
        type="button"
        class="btn btn-sm ms-auto"
        :disabled="directory.isFetching.value"
        data-testid="directory-refresh"
        @click="directory.refetch()"
      >
        <RefreshCw aria-hidden="true" class="size-4" />
        {{ t('visualisation.refresh') }}
      </button>
    </div>

    <div v-if="directory.isPending.value" key="loading" class="skeleton h-40 w-full"></div>

    <div
      v-else-if="directory.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error items-start text-sm"
      data-testid="directory-failed"
    >
      <span>{{ forbidden ? t('visualisation.forbidden') : t('visualisation.loadFailed') }}</span>
      <button
        v-if="!forbidden"
        key="retry"
        type="button"
        class="btn btn-sm"
        data-testid="directory-retry"
        @click="directory.refetch()"
      >
        {{ t('visualisation.retry') }}
      </button>
    </div>

    <p
      v-else-if="rows.length === 0"
      key="empty"
      class="text-sm text-secondary"
      data-testid="directory-empty"
    >
      {{ t('visualisation.directory.empty') }}
    </p>

    <div v-else key="list" data-testid="directory-list">
      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        filter-field="name"
        :filter-text="search"
        :page-size="25"
        :pager-labels="pagerLabels"
        :empty-text="t('visualisation.directory.noMatch')"
        :label="t('visualisation.directory.list')"
        row-key="guid"
        expandable
        :expand-labels="{
          expand: t('visualisation.directory.showDetail'),
          collapse: t('visualisation.directory.hideDetail'),
        }"
        @action="onAction"
        @expand="onExpand"
      />
      <p class="mt-2 text-xs text-secondary" data-testid="directory-window-note">
        {{ t('visualisation.directory.windowNote', { days: windowDays }) }}
      </p>
      <template v-for="[id, host] in hosts" :key="String(id)">
        <Teleport :to="host">
          <VisualisationDirectoryDetail v-if="rowOf(id)" :registry-id="rowOf(id)!.id" />
        </Teleport>
      </template>
    </div>
  </div>
</template>
