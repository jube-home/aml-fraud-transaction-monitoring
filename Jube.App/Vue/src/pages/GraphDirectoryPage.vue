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
import {computed, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {useRouter} from 'vue-router'
import {useQuery} from '@tanstack/vue-query'
import {RefreshCw, Search} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {ApiError} from '../api/useApi'
import DataGrid from '../components/DataGrid.vue'
import type {GridActionEvent, GridColumn} from '../components/dataGridTypes'
import {usePagerLabels} from '../composables/usePagerLabels'
import {graphApi} from '../graph/api'
import type {GraphRegistry} from '../graph/types'
import {directionFor} from '../i18n'
import {useJoobyPageContext} from '../composables/useJoobyPageContext'

const {t, locale} = useI18n()
const router = useRouter()
const pagerLabels = usePagerLabels()

const search = ref('')

const directory = useQuery({
  queryKey: queryKeys.graphDirectory(),
  queryFn: () => graphApi.directory(),
  retry: false,
})

const rows = computed(() =>
  (directory.data.value ?? [])
    .filter((entry) => entry.contextKindId === 1)
    .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
)

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

function layoutName(id: unknown): string {
  if (id === 2) return t('graph.layout.radial')
  if (id === 3) return t('graph.layout.flow')
  if (id === 4) return t('graph.layout.hierarchy')

  return t('graph.layout.force')
}

const columns = computed<GridColumn[]>(() => [
  {field: 'name', title: t('graph.directory.col.name'), minWidth: 220},
  {
    field: 'defaultLayoutId',
    title: t('graph.directory.col.layout'),
    width: 130,
    badge: (value) => ({label: layoutName(value), tone: 'neutral'}),
  },
  {
    field: 'defaultHops',
    title: t('graph.directory.col.hops'),
    mono: true,
    align: 'end',
    width: 90,
    type: 'number',
  },
  {
    field: 'defaultWindowDays',
    title: t('graph.directory.col.window'),
    mono: true,
    align: 'end',
    width: 120,
    format: (value) =>
      typeof value === 'number' ? t('graph.directory.days', {count: value}, value) : '',
  },
  {
    field: 'renderCap',
    title: t('graph.directory.col.cap'),
    mono: true,
    align: 'end',
    width: 100,
    type: 'number',
  },
  {
    field: '',
    title: t('crud.actions'),
    width: 110,
    pinned: true,
    actions: () => [{id: 'open', label: t('graph.directory.open')}],
  },
])

function onAction(event: GridActionEvent): void {
  if (event.id === 'open')
    void router.push({name: 'graphView', params: {guid: (event.row as GraphRegistry).guid}})
}

useJoobyPageContext(() => ({pageName: 'GraphDirectory', state: 'readonly', record: undefined}))
</script>

<template>
  <section class="flex flex-col gap-4" data-testid="graph-directory-page">
    <h1 class="sr-only">{{ t('nav.graph') }}</h1>

    <div class="flex flex-wrap items-center gap-3">
      <h2 class="text-lg font-semibold">{{ t('graph.directory.title') }}</h2>
      <label class="input input-sm w-full max-w-xs">
        <Search class="size-4 opacity-60" aria-hidden="true"/>
        <input
          v-model="search"
          type="search"
          :placeholder="t('graph.directory.searchPlaceholder')"
          :aria-label="t('graph.directory.search')"
          data-testid="graph-directory-search"
        />
      </label>
      <span class="badge badge-sm badge-neutral font-mono" data-testid="graph-directory-count">{{
          t('graph.directory.count', {count: shown}, shown)
        }}</span>
      <button
        type="button"
        class="btn btn-sm ms-auto"
        :disabled="directory.isFetching.value"
        data-testid="graph-directory-refresh"
        @click="directory.refetch()"
      >
        <RefreshCw aria-hidden="true" class="size-4"/>
        {{ t('graph.directory.refresh') }}
      </button>
    </div>

    <div v-if="directory.isPending.value" key="loading" class="skeleton h-40 w-full"></div>

    <div
      v-else-if="directory.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error items-start text-sm"
      data-testid="graph-directory-failed"
    >
      <span>{{
          forbidden ? t('graph.directory.forbidden') : t('graph.directory.loadFailed')
        }}</span>
      <button
        v-if="!forbidden"
        key="retry"
        type="button"
        class="btn btn-sm"
        data-testid="graph-directory-retry"
        @click="directory.refetch()"
      >
        {{ t('graph.directory.retry') }}
      </button>
    </div>

    <p
      v-else-if="rows.length === 0"
      key="empty"
      class="text-sm text-secondary"
      data-testid="graph-directory-empty"
    >
      {{ t('graph.directory.empty') }}
    </p>

    <div v-else key="list" data-testid="graph-directory-list">
      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        filter-field="name"
        :filter-text="search"
        :page-size="25"
        :pager-labels="pagerLabels"
        :empty-text="t('graph.directory.noMatch')"
        :label="t('graph.directory.list')"
        row-key="guid"
        @action="onAction"
      />
    </div>
  </section>
</template>
