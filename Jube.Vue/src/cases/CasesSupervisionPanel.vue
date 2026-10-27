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
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useRoute, useRouter } from 'vue-router'
import DataGrid from '../components/DataGrid.vue'
import StatCard from '../components/StatCard.vue'
import type { GridActionEvent, GridColumn } from '../components/dataGridTypes'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { periodFrom, periodOf, periods } from './analyticsLayout'
import CaseSupervisionDialog, { type SupervisionAction } from './CaseSupervisionDialog.vue'
import { durationText } from './duration'
import { caseLabel } from './labels'
import { idleChoices, idleOf, supervisionApi, type StuckLock } from './supervisionApi'
import { useCasesState } from './useCasesState'

const props = defineProps<{ workflow: string }>()

const { t } = useI18n()
const { number, dateTime } = useFormat()
const pagerLabels = usePagerLabels()
const route = useRoute()
const router = useRouter()
const client = useQueryClient()
const { state, go } = useCasesState()

const days = computed(() => periodOf(route.query.days))
const idle = computed(() => idleOf(route.query.idle))
const now = ref(new Date())

const pending = ref<{ action: SupervisionAction; lock: StuckLock } | null>(null)

const enabled = computed(() => props.workflow !== '')

const workload = useQuery({
  queryKey: computed(() => ['service', 'Case', 'supervision', props.workflow, days.value] as const),
  queryFn: () => supervisionApi.workload(props.workflow, periodFrom(days.value, now.value)),
  enabled,
})

const stuck = useQuery({
  queryKey: computed(() => ['service', 'Case', 'stuck', props.workflow, idle.value] as const),
  queryFn: () => supervisionApi.stuck(props.workflow, idle.value),
  enabled,
})

const analysts = computed(() => (workload.data.value?.analysts ?? []).map((row) => row.user))

function minutes(value: unknown): string {
  return typeof value === 'number' ? durationText(t, value, number) : t('cases.supervision.none')
}

const workloadColumns = computed<GridColumn[]>(() => [
  { field: 'user', title: t('cases.supervision.column.user'), mono: true, minWidth: 180 },
  { field: 'holding', title: t('cases.supervision.column.holding'), align: 'end', mono: true },
  {
    field: 'oldestHoldMinutes',
    title: t('cases.supervision.column.oldest'),
    align: 'end',
    mono: true,
    format: minutes,
  },
  { field: 'taken', title: t('cases.supervision.column.taken'), align: 'end', mono: true },
  { field: 'peeks', title: t('cases.supervision.column.peeks'), align: 'end', mono: true },
  { field: 'closed', title: t('cases.supervision.column.closed'), align: 'end', mono: true },
  {
    field: 'medianResolutionMinutes',
    title: t('cases.supervision.column.median'),
    align: 'end',
    mono: true,
    format: minutes,
  },
])

const stuckColumns = computed<GridColumn[]>(() => [
  {
    field: 'caseId',
    title: t('cases.supervision.column.case'),
    mono: true,
    width: 110,
    format: (value) => caseLabel(Number(value)),
  },
  { field: 'caseKeyValue', title: t('cases.supervision.column.key'), mono: true, minWidth: 140 },
  { field: 'statusName', title: t('cases.supervision.column.status'), minWidth: 140 },
  { field: 'lockedUser', title: t('cases.supervision.column.holder'), mono: true, minWidth: 160 },
  {
    field: 'lastActivityDate',
    title: t('cases.supervision.column.lastActivity'),
    mono: true,
    minWidth: 150,
    format: (value) => dateTime(String(value)),
  },
  {
    field: 'idleMinutes',
    title: t('cases.supervision.column.idle'),
    align: 'end',
    mono: true,
    format: minutes,
  },
  { field: 'priority', title: t('cases.supervision.column.priority'), align: 'end', mono: true },
  {
    field: 'actions',
    title: t('cases.supervision.column.actions'),
    hiddenTitle: t('cases.supervision.column.actions'),
    pinned: true,
    width: 230,
    actions: () => [
      { id: 'open', label: t('cases.supervision.action.open') },
      { id: 'reassign', label: t('cases.supervision.action.reassign') },
      { id: 'break', label: t('cases.supervision.action.breakLock'), tone: 'error' },
    ],
  },
])

function act(event: GridActionEvent): void {
  const lock = event.row as StuckLock

  if (event.id === 'open') {
    go({ ...state.value, tab: 'queue', caseId: lock.caseId })
  } else if (event.id === 'reassign' || event.id === 'break') {
    pending.value = { action: event.id, lock }
  }
}

function done(): void {
  pending.value = null
  void client.invalidateQueries({ queryKey: ['service', 'Case'] })
}

function change(next: { days?: number; idle?: number }): void {
  void router.replace({
    query: {
      ...route.query,
      days: String(next.days ?? days.value),
      idle: String(next.idle ?? idle.value),
    },
  })
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="cases-supervision-page">
    <h2 class="sr-only">{{ t('cases.supervision.title') }}</h2>

    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('cases.analytics.period') }}</span>
        <select
          class="select select-sm"
          :value="days"
          data-testid="cases-supervision-period"
          @change="change({ days: Number(($event.target as HTMLSelectElement).value) })"
        >
          <option v-for="period in periods" :key="period" :value="period">
            {{ t('cases.analytics.days', { n: period }) }}
          </option>
        </select>
      </label>

      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('cases.supervision.idle') }}</span>
        <select
          class="select select-sm"
          :value="idle"
          data-testid="cases-supervision-idle"
          @change="change({ idle: Number(($event.target as HTMLSelectElement).value) })"
        >
          <option v-for="choice in idleChoices" :key="choice" :value="choice">
            {{ durationText(t, choice, number) }}
          </option>
        </select>
      </label>
    </div>

    <section
      v-if="workload.data.value"
      key="headline"
      class="grid w-full gap-3 sm:grid-cols-3"
      :aria-label="t('cases.supervision.headline.label')"
      data-testid="cases-supervision-headline"
    >
      <StatCard :title="t('cases.supervision.headline.held')">
        {{ number(workload.data.value.held) }}
      </StatCard>
      <StatCard :title="t('cases.supervision.headline.unassigned')">
        {{ number(workload.data.value.unassigned) }}
      </StatCard>
      <StatCard :title="t('cases.supervision.headline.stuck')">
        {{ number(stuck.data.value?.length ?? 0) }}
      </StatCard>
    </section>

    <section class="flex min-w-0 flex-col gap-2" data-testid="cases-supervision-workload">
      <h3 class="text-sm font-semibold">{{ t('cases.supervision.workload.title') }}</h3>
      <p class="text-xs text-base-content/70">{{ t('cases.supervision.workload.hint') }}</p>
      <div v-if="workload.isError.value" key="failed" role="alert" class="alert alert-error">
        <span>{{ t('cases.supervision.failed') }}</span>
      </div>
      <DataGrid
        v-else-if="workload.data.value"
        key="grid"
        :columns="workloadColumns"
        :rows="workload.data.value.analysts"
        :empty-text="t('cases.supervision.workload.empty')"
        :label="t('cases.supervision.workload.title')"
        :page-size="25"
        :pager-labels="pagerLabels"
        row-key="user"
      />
      <div v-else key="loading" class="skeleton h-24 w-full"></div>
    </section>

    <section class="flex min-w-0 flex-col gap-2" data-testid="cases-supervision-stuck">
      <h3 class="text-sm font-semibold">{{ t('cases.supervision.stuck.title') }}</h3>
      <p class="text-xs text-base-content/70">{{ t('cases.supervision.stuck.hint') }}</p>
      <div v-if="stuck.isError.value" key="failed" role="alert" class="alert alert-error">
        <span>{{ t('cases.supervision.failed') }}</span>
      </div>
      <DataGrid
        v-else-if="stuck.data.value"
        key="grid"
        :columns="stuckColumns"
        :rows="stuck.data.value"
        :empty-text="t('cases.supervision.stuck.empty')"
        :label="t('cases.supervision.stuck.title')"
        :page-size="25"
        :pager-labels="pagerLabels"
        row-key="caseId"
        @action="act"
      />
      <div v-else key="loading" class="skeleton h-24 w-full"></div>
    </section>

    <CaseSupervisionDialog
      :action="pending?.action ?? null"
      :lock="pending?.lock ?? null"
      :analysts="analysts"
      @close="pending = null"
      @done="done"
    />
  </section>
</template>
