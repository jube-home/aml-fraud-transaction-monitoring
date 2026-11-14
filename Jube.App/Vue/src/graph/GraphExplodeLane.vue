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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {queryKeys} from '../api/queryKeys'
import {journalApi} from '../cases/journal/api'
import DataGrid from '../components/DataGrid.vue'
import type {GridColumn} from '../components/dataGridTypes'
import {useFormat} from '../composables/useFormat'
import {directionFor} from '../i18n'
import {graphApi} from './api'
import type {GraphExplodeLane, GraphWindow} from './types'

const props = withDefaults(
  defineProps<{
    lane: GraphExplodeLane
    window: GraphWindow | null
    workflow?: string
    seedKey: string
    seedValue: string
  }>(),
  {workflow: ''},
)

const {t, locale} = useI18n()
const {fixed, dateTime} = useFormat()

const direction = computed(() => directionFor(locale.value))

const transactions = useQuery({
  queryKey: computed(
    () =>
      [
        ...queryKeys.graphEdgeTransactions(
          props.lane.edgeIds[0] ?? 0,
          props.window?.from,
          props.window?.to,
        ),
        props.lane.edgeIds.join(','),
      ] as const,
  ),
  queryFn: async () => {
    const found = await Promise.all(
      props.lane.edgeIds.map((edgeId) =>
        graphApi.edgeTransactions(edgeId, props.window?.from, props.window?.to, 200),
      ),
    )

    return found.flatMap((result) => result.transactions)
  },
  enabled: computed(() => props.lane.edgeIds.length > 0),
  staleTime: 30_000,
  retry: false,
})

const archiveIds = computed(() =>
  [...new Set((transactions.data.value ?? []).map((row) => row.archiveId))].slice(0, 500),
)

const journal = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'rows',
        props.workflow,
        props.seedKey,
        props.seedValue,
        'archive',
        archiveIds.value.join(','),
      ] as const,
  ),
  queryFn: () =>
    journalApi.rows({
      caseWorkflowGuid: props.workflow,
      key: props.seedKey,
      value: props.seedValue,
      from: props.window?.from ?? new Date(0).toISOString(),
      limit: 500,
      archiveIds: archiveIds.value.join(','),
    }),
  enabled: computed(
    () => props.workflow !== '' && props.seedKey !== '' && archiveIds.value.length > 0,
  ),
  staleTime: 30_000,
  retry: false,
})

const journalColumns = computed<GridColumn[]>(() =>
  (journal.data.value?.columns ?? []).slice(0, 12).map((column) => ({
    field: column.name,
    title: column.title,
    mono: column.type === 'number' || column.type === 'date',
    align: column.type === 'number' ? 'end' : 'start',
    type: column.type === 'number' ? 'number' : column.type === 'date' ? 'date' : 'text',
    format: (value: unknown) =>
      column.type === 'date' && typeof value === 'string'
        ? dateTime(value)
        : column.type === 'number' && typeof value === 'number'
          ? fixed(value, Number.isInteger(value) ? 0 : 2)
          : column.type === 'boolean'
            ? value
              ? t('graph.explode.lane.yes')
              : t('graph.explode.lane.no')
            : value === null || value === undefined
              ? ''
              : String(value),
  })),
)

const plainColumns = computed<GridColumn[]>(() => [
  {
    field: 'archiveId',
    title: t('graph.explode.lane.col.archiveId'),
    mono: true,
    type: 'number',
    width: 140,
  },
  {
    field: 'occurredDate',
    title: t('graph.explode.lane.col.occurred'),
    mono: true,
    type: 'date',
    width: 190,
    format: (value) => (typeof value === 'string' ? dateTime(value) : ''),
  },
  {
    field: 'amount',
    title: t('graph.explode.lane.col.amount'),
    mono: true,
    type: 'number',
    align: 'end',
    width: 140,
    format: (value) => (typeof value === 'number' ? fixed(value) : ''),
  },
])

const useJournal = computed(() => props.workflow !== '' && props.seedKey !== '')
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2 p-2" :data-testid="`graph-lane-${lane.node.id}`">
    <div
      v-if="transactions.isPending.value || (useJournal && journal.isPending.value)"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('graph.explode.lane.loading')"
    ></div>
    <p
      v-else-if="transactions.isError.value || (useJournal && journal.isError.value)"
      key="failed"
      role="alert"
      class="text-sm"
    >
      {{ t('graph.explode.lane.failed') }}
    </p>
    <p
      v-else-if="archiveIds.length === 0"
      key="empty"
      class="text-sm text-secondary"
      data-testid="graph-lane-empty"
    >
      {{ t('graph.explode.lane.empty') }}
    </p>
    <template v-else>
      <p class="text-xs text-secondary">
        {{ t('graph.explode.lane.journal', {count: archiveIds.length}) }}
      </p>
      <DataGrid
        v-if="useJournal && journal.data.value"
        key="journal"
        :columns="journalColumns"
        :rows="journal.data.value.rows"
        :direction="direction"
        :empty-text="t('graph.explode.lane.empty')"
        :label="t('graph.explode.lane.label')"
        auto-height
      />
      <DataGrid
        v-else
        key="plain"
        :columns="plainColumns"
        :rows="transactions.data.value ?? []"
        :direction="direction"
        :empty-text="t('graph.explode.lane.empty')"
        :label="t('graph.explode.lane.label')"
        auto-height
      />
    </template>
  </div>
</template>
