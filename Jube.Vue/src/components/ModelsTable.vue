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
import { Plus, RefreshCw, Search } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { describeApiError } from '../api/problems'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { useApprovalState } from '../composables/useApprovalState'
import { useModelApprovals } from '../composables/useModelApprovals'
import { usePendingCounts } from '../composables/usePendingCounts'
import { stateOf, type ApprovalState } from '../approval/model'
import { formModelApi } from '../formModel/api'
import type { RecordValues } from '../formModel/types'
import { modelArea } from '../story/path'
import { hasPermission, meQueryOptions } from '../auth/me'
import { integrityPermission } from '../integrity/model'
import DataGrid from './DataGrid.vue'
import ModelSynchronisation from './ModelSynchronisation.vue'
import type { GridActionEvent, GridBadge, GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const emit = defineEmits<{ open: [values: RecordValues]; create: [] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const pagerLabels = usePagerLabels()

const search = ref('')
const syncing = ref(false)
const me = useQuery(meQueryOptions)
const canSynchronise = computed(() => hasPermission(me.data.value, integrityPermission.synchronise))

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel(modelArea)),
  queryFn: () => formModelApi.model(modelArea),
  staleTime: 30_000,
  retry: false,
})

const records = useQuery({
  queryKey: computed(() => queryKeys.formRecords(modelArea, undefined)),
  queryFn: () => formModelApi.records(model.data.value!, undefined),
  enabled: computed(() => model.data.value !== undefined),
  retry: false,
})

const ids = computed(() =>
  (records.data.value ?? [])
    .map((row) => row.id)
    .filter((id): id is number => typeof id === 'number'),
)
const awaiting = usePendingCounts(ids)
const approvalState = useApprovalState()
const statuses = useModelApprovals(
  ids,
  computed(() => model.data.value?.approval?.kind),
)

const rows = computed(() =>
  (records.data.value ?? []).map((row) => ({
    ...row,
    awaiting: awaiting.value.get(row.id as number),
    approval: statusState(row.id as number),
  })),
)

const shown = computed(() => {
  const needle = search.value.trim().toLowerCase()

  return needle === ''
    ? rows.value.length
    : rows.value.filter((row) =>
        String((row as RecordValues).name ?? '')
          .toLowerCase()
          .includes(needle),
      ).length
})

function statusState(id: number): ApprovalState | undefined {
  const status = statuses.value.get(id)

  return status ? stateOf(status) : undefined
}

function approvalBadge(value: unknown): GridBadge {
  if (typeof value !== 'string') return { label: t('models.unknown'), tone: 'neutral' }

  const state = value as ApprovalState

  return { label: approvalState.label(state), tone: approvalState.tone(state) }
}

const canCreate = computed(() => model.data.value?.operations.includes('create') ?? false)
const direction = computed(() => directionFor(locale.value))
const failure = computed(() => {
  const error = model.error.value ?? records.error.value

  return error
    ? describeApiError(error, { failed: t('models.failed'), refused: t('models.refused') })
    : undefined
})

function awaitingBadge(value: unknown): GridBadge {
  if (typeof value !== 'number') return { label: t('models.unknown'), tone: 'neutral' }

  return value > 0
    ? { label: t('models.awaitingFact', { count: value }), tone: 'warning' }
    : { label: t('models.noneAwaiting'), tone: 'neutral' }
}

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('models.col.name'), minWidth: 200 },
  {
    field: 'version',
    title: t('models.col.version'),
    mono: true,
    align: 'end',
    width: 100,
    type: 'number',
  },
  {
    field: 'active',
    title: t('models.col.status'),
    width: 130,
    badge: (value) =>
      value === true
        ? { label: t('models.active'), tone: 'success' }
        : { label: t('models.inactive'), tone: 'neutral' },
  },
  {
    field: 'locked',
    title: t('models.col.locked'),
    width: 130,
    badge: (value) =>
      value === true
        ? { label: t('models.locked'), tone: 'warning' }
        : { label: t('models.editable'), tone: 'neutral' },
  },
  { field: 'approval', title: t('models.col.approval'), width: 190, badge: approvalBadge },
  { field: 'awaiting', title: t('models.col.awaiting'), width: 190, badge: awaitingBadge },
  {
    field: 'updatedDate',
    title: t('models.col.updated'),
    mono: true,
    width: 170,
    type: 'date',
    format: (value) => (value ? dateTime(String(value)) : ''),
  },
  { field: 'updatedUser', title: t('models.col.by'), width: 140 },
  {
    field: '',
    title: t('crud.actions'),
    width: 110,
    pinned: true,
    actions: () => [{ id: 'open', label: t('models.open') }],
  },
])

function onAction(event: GridActionEvent): void {
  if (event.id === 'open') emit('open', event.row as RecordValues)
}
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="models-table">
    <div class="flex flex-wrap items-center gap-3">
      <label class="input input-sm w-full max-w-xs">
        <Search class="size-4 opacity-60" aria-hidden="true" />
        <input
          v-model="search"
          type="search"
          :placeholder="t('models.searchPlaceholder')"
          :aria-label="t('models.search')"
          data-testid="models-search"
        />
      </label>
      <span class="badge badge-sm badge-neutral font-mono" data-testid="models-count">{{
        t('models.count', { count: shown })
      }}</span>
      <button
        v-if="canSynchronise"
        key="sync"
        type="button"
        class="btn btn-sm ms-auto"
        :aria-expanded="syncing"
        data-testid="models-sync-all"
        @click="syncing = !syncing"
      >
        <RefreshCw class="size-4" aria-hidden="true" />
        {{ t('models.syncAll') }}
      </button>
      <button
        v-if="canCreate"
        key="create"
        type="button"
        class="btn btn-sm btn-primary"
        data-testid="models-new"
        @click="emit('create')"
      >
        <Plus class="size-4" aria-hidden="true" />
        {{ t('models.new') }}
      </button>
    </div>

    <ModelSynchronisation v-if="syncing && canSynchronise" key="synchronise" />

    <ProblemAlert v-if="failure" key="failure" :reasons="failure" />

    <div v-else-if="records.isPending.value" key="loading" class="skeleton h-64 w-full"></div>

    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      filter-field="name"
      :filter-text="search"
      :page-size="25"
      :pager-labels="pagerLabels"
      :empty-text="t('models.empty')"
      :label="t('models.list')"
      row-key="id"
      @action="onAction"
    />
  </div>
</template>
