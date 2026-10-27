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
import { computed, nextTick, onBeforeUnmount, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Undo2 } from 'lucide-vue-next'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { useRoute, useRouter } from 'vue-router'
import DataGrid from '../../components/DataGrid.vue'
import type { GridActionEvent, GridRowPaint } from '../../components/dataGridTypes'
import { useFormat } from '../../composables/useFormat'
import { usePagerLabels } from '../../composables/usePagerLabels'
import { useToasts } from '../../stores/toasts'
import { journalApi, type JournalQuery } from './api'
import { buildColumns, buildRows, type GridRow } from './columns'
import {
  apply,
  emptyLayout,
  movable,
  moved,
  parseLayout,
  resized,
  serialiseLayout,
  shown,
  visibleFields,
  withLens,
  type ColumnEntry,
  type JournalLayout,
} from './layout'
import JournalColumns from './JournalColumns.vue'
import { serialiseFilter } from './filter'
import JournalCharts from './JournalCharts.vue'
import JournalControls from './JournalControls.vue'
import JournalMap from './JournalMap.vue'
import JournalMapOptions from './JournalMapOptions.vue'
import JournalPeers from './JournalPeers.vue'
import JournalSavedViews from './JournalSavedViews.vue'
import JournalWindow from './JournalWindow.vue'
import JournalScope from './JournalScope.vue'
import JournalStrip from './JournalStrip.vue'
import JournalTagAll from './JournalTagAll.vue'
import JournalTagBar from './JournalTagBar.vue'
import JournalTagJob from './JournalTagJob.vue'
import {
  maximumLanes,
  mergeJournalState,
  parseJournalState,
  windowFrom,
  type JournalState,
} from './state'
import { isActive, pollDelay } from './jobs'
import { guidOf, keepSelected, presentTags, toggled } from './tags'
import TransactionDrawer from './TransactionDrawer.vue'
import type { JournalRow, TagJob, TagSet } from './types'

const props = defineProps<{
  workflow: string
  caseKey: string
  caseKeyValue: string
  caseId: number
}>()

const mapView = useTemplateRef<{ fit: () => void }>('mapView')
const mapFocus = ref<string | null>(null)
const mapSelected = ref<string | null>(null)

const { t } = useI18n()
const { number, dateTime, fixed } = useFormat()
const pagerLabels = usePagerLabels()
const toasts = useToasts()
const queryClient = useQueryClient()
const route = useRoute()
const router = useRouter()

const grid = ref<{
  downloadCsv: (name: string) => void
  downloadXlsx: (name: string, sheet?: string) => Promise<void>
} | null>(null)

const state = computed(() => parseJournalState(route.query))
const now = ref(new Date())

const key = computed(() => state.value.pivot?.key ?? props.caseKey)
const value = computed(() => state.value.pivot?.value ?? props.caseKeyValue)

const query = computed<JournalQuery>(() => ({
  caseWorkflowGuid: props.workflow,
  key: key.value,
  value: value.value,
  from: windowFrom(state.value.days, now.value),
  activationsOnly: state.value.activationsOnly,
}))

const filterText = computed(() => serialiseFilter(state.value.filter))

const rows = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'rows',
        props.workflow,
        key.value,
        value.value,
        state.value.lens,
        state.value.days,
        state.value.activationsOnly,
        JSON.stringify(state.value.merge),
        state.value.pinnedOnly,
        filterText.value,
        props.caseId,
      ] as const,
  ),
  queryFn: () =>
    journalApi.rows({
      ...query.value,
      lens: state.value.lens,
      limit: 1000,
      caseId: props.caseId,
      merge: state.value.merge,
      pinnedOnly: state.value.pinnedOnly,
      filter: filterText.value,
    }),
  placeholderData: keepPreviousData,
  staleTime: 30_000,
  retry: false,
})

const analytics = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'analytics',
        props.workflow,
        key.value,
        value.value,
        state.value.days,
        state.value.activationsOnly,
      ] as const,
  ),
  queryFn: () =>
    journalApi.analytics({ ...query.value, sections: 'summary,notable,novelty,peers' }),
  placeholderData: keepPreviousData,
  staleTime: 60_000,
  retry: false,
})

const selected = ref<Set<string>>(new Set())
const bulk = ref(0)
const undo = ref<{ text: string; sets: TagSet[] } | null>(null)

const tagOptions = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'tags', props.workflow] as const),
  queryFn: () => journalApi.tagOptions(props.workflow),
  staleTime: 60_000,
  retry: false,
})

const records = computed(() => rows.data.value?.rows ?? [])
const pinCount = computed(() => records.value.filter((row) => row.Pinned === true).length)
const primary = computed(() => ({ key: key.value, value: value.value }))
const lanes = computed(() => [primary.value, ...state.value.merge])
const present = computed(() => presentTags(records.value, selected.value))

watch(records, (next) => {
  selected.value = keepSelected(selected.value, next)
})

function toggle(guid: string, on: boolean): void {
  selected.value = toggled(selected.value, guid, on)
}

function selectAll(): void {
  selected.value = new Set(everyRow.value.map(guidOf))
  bulk.value += 1
}

function clearSelection(): void {
  selected.value = new Set()
  bulk.value += 1
}

function refresh(): void {
  void queryClient.invalidateQueries({ queryKey: ['service', 'Case', 'keyjournal', 'rows'] })
}

const tagging = useMutation({
  mutationFn: (change: { add: string[]; remove: string[] }) =>
    journalApi.tag(props.workflow, [...selected.value], change.add, change.remove),
  onSuccess: (result) => {
    const done = t('journal.tags.done', { n: number(result.current.length) })

    const notes = [
      result.skipped > 0 ? t('journal.tags.skipped', { n: number(result.skipped) }) : '',
      result.locked > 0 ? t('journal.tags.locked', { n: number(result.locked) }) : '',
    ].filter((note) => note !== '')

    undo.value = { text: [done, ...notes].join(' '), sets: result.previous }
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const pinning = useMutation({
  mutationFn: (change: { guid: string; pinned: boolean }) =>
    journalApi.setPins(props.caseId, [change.guid], change.pinned),
  onSuccess: refresh,
  onError: (error) => toasts.apiError(error),
})

const allOpen = ref(false)
const jobGuid = ref<string | null>(null)

function tagJobView(): JournalQuery {
  return {
    ...query.value,
    merge: state.value.merge,
    filter: filterText.value,
  }
}

const preview = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'tagjobpreview',
        props.workflow,
        key.value,
        value.value,
        state.value.days,
        state.value.activationsOnly,
        JSON.stringify(state.value.merge),
        filterText.value,
      ] as const,
  ),
  queryFn: () => journalApi.tagJobPreview(tagJobView()),
  enabled: allOpen,
  staleTime: 0,
  gcTime: 0,
  retry: false,
})

const jobs = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'tagjobs', props.workflow] as const),
  queryFn: () => journalApi.tagJobs(props.workflow),
  staleTime: 30_000,
  retry: false,
})

watch(
  () => jobs.data.value,
  (list) => {
    if (jobGuid.value !== null) return

    const running = list?.find((item) => isActive(item))

    if (running) jobGuid.value = running.guid
  },
)

const job = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'tagjob', jobGuid.value] as const),
  queryFn: () => journalApi.tagJob(jobGuid.value!),
  enabled: computed(() => jobGuid.value !== null),
  refetchInterval: (query) => pollDelay(query.state.data),
  retry: false,
})

watch(
  () => job.data.value?.state,
  (now, before) => {
    if (before && now && isActive({ state: before }) && !isActive({ state: now })) refresh()
  },
)

function follow(next: TagJob): void {
  queryClient.setQueryData(['service', 'Case', 'keyjournal', 'tagjob', next.guid], next)
  jobGuid.value = next.guid
  void queryClient.invalidateQueries({ queryKey: ['service', 'Case', 'keyjournal', 'tagjobs'] })
}

const submitting = useMutation({
  mutationFn: (change: { add: string[]; remove: string[] }) =>
    journalApi.tagJobSubmit(tagJobView(), change.add, change.remove),
  onSuccess: follow,
  onError: (error) => toasts.apiError(error),
})

const stopping = useMutation({
  mutationFn: (guid: string) => journalApi.tagJobCancel(guid),
  onSuccess: () => void job.refetch(),
  onError: (error) => toasts.apiError(error),
})

const reversing = useMutation({
  mutationFn: (guid: string) => journalApi.tagJobUndo(guid),
  onSuccess: follow,
  onError: (error) => toasts.apiError(error),
})

const jobBusy = computed(
  () => submitting.isPending.value || stopping.isPending.value || reversing.isPending.value,
)

const restoring = useMutation({
  mutationFn: (sets: TagSet[]) => journalApi.restore(props.workflow, sets),
  onSuccess: () => {
    undo.value = null
    toasts.success(t('journal.tags.undone'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const defaultColumns = computed(() =>
  buildColumns(
    rows.data.value?.columns ?? [],
    {
      t: (name, values) => t(name, values ?? {}),
      number,
      dateTime,
      fixed,
      selection: { has: (guid) => selected.value.has(guid), toggle },
      pin: { toggle: (guid, pinned) => pinning.mutate({ guid, pinned }) },
      merged: state.value.merge.length > 0,
      mappable: rows.data.value?.meta.location != null,
      formatted: everyRow.value.some((row) => row.__paint !== null),
    },
    analytics.data.value?.novelty?.fields ?? null,
  ),
)

const savedLayout = useQuery({
  queryKey: computed(() => ['service', 'Case', 'keyjournal', 'layout', props.workflow] as const),
  queryFn: () => journalApi.layout(props.workflow),
  staleTime: Infinity,
  retry: false,
})

const layout = ref<JournalLayout>(emptyLayout())
let layoutTouched = false
let saveTimer: ReturnType<typeof setTimeout> | undefined
let unsaved = false

watch(
  () => savedLayout.data.value,
  (saved) => {
    if (saved !== undefined && !layoutTouched) layout.value = parseLayout(saved?.json)
  },
  { immediate: true },
)

const savingLayout = useMutation({
  mutationFn: (json: string) => journalApi.saveLayout(props.workflow, json),
  onError: (error) => toasts.apiError(error),
})

const lensEntries = computed(() => layout.value.lenses[state.value.lens])

const columns = computed(() => apply(defaultColumns.value, lensEntries.value))

const choices = computed(() => {
  const on = new Set(visibleFields(defaultColumns.value, lensEntries.value))

  return movable(defaultColumns.value).map((column) => ({
    field: column.field,
    title: column.title,
    on: on.has(column.field),
  }))
})

function changeLayout(entries: ColumnEntry[] | undefined): void {
  layoutTouched = true
  layout.value = withLens(layout.value, state.value.lens, entries)
  unsaved = true
  clearTimeout(saveTimer)
  saveTimer = setTimeout(() => {
    unsaved = false
    savingLayout.mutate(serialiseLayout(layout.value))
  }, 600)
}

function columnMoved(order: string[]): void {
  changeLayout(moved(defaultColumns.value, lensEntries.value, order))
}

function columnResized(event: { field: string; width: number }): void {
  changeLayout(resized(defaultColumns.value, lensEntries.value, event.field, event.width))
}

function columnShown(field: string, on: boolean): void {
  changeLayout(shown(defaultColumns.value, lensEntries.value, field, on))
}

onBeforeUnmount(() => {
  if (unsaved) {
    clearTimeout(saveTimer)
    savingLayout.mutate(serialiseLayout(layout.value))
  }
})

const everyRow = computed(() =>
  buildRows(
    rows.data.value?.rows ?? [],
    analytics.data.value?.notable?.rows ?? [],
    analytics.data.value?.novelty?.rows ?? null,
    (name, values) => t(name, values ?? {}),
  ).map((row) => ({ ...row, __bulk: bulk.value })),
)

const openPaint = computed(
  () => everyRow.value.find((row) => row.Id === state.value.transaction)?.__paint ?? null,
)

const marked = computed(() => {
  const open = everyRow.value.find((row) => row.Id === state.value.transaction)

  return {
    field: 'EntityAnalysisModelInstanceEntryGuid',
    values: [open ? guidOf(open) : undefined, mapSelected.value].filter(
      (value): value is string => typeof value === 'string' && value !== '',
    ),
  }
})

function rowPaint(row: object): GridRowPaint | null {
  const paint = (row as GridRow).__paint

  return paint ? { ...paint.row, title: paint.reasons.join('. ') } : null
}

function change(next: JournalState): void {
  void router.replace({ query: mergeJournalState(route.query, next) })
}

function open(id: number): void {
  change({ ...state.value, transaction: id })
}

function close(): void {
  change({ ...state.value, transaction: null })
}

function addLane(laneKey: string, laneValue: string): void {
  const exists = lanes.value.some((lane) => lane.key === laneKey && lane.value === laneValue)

  if (exists || state.value.merge.length >= maximumLanes) return

  change({ ...state.value, merge: [...state.value.merge, { key: laneKey, value: laneValue }] })
}

const mapBlock = useTemplateRef<HTMLElement>('mapBlock')

async function showOnMap(guid: string): Promise<void> {
  mapFocus.value = null
  await nextTick()
  mapFocus.value = guid
  mapBlock.value?.scrollIntoView?.({ block: 'start' })
}

function pivot(pivotKey: string, pivotValue: string): void {
  change({ ...state.value, pivot: { key: pivotKey, value: pivotValue }, transaction: null })
}

function act(event: GridActionEvent): void {
  if (event.id === 'open') open((event.row as { Id: number }).Id)

  if (event.id === 'map') void showOnMap(guidOf(event.row as JournalRow))
}

function exportName(extension: string): string {
  return `journal-${value.value}.${extension}`
}

function csv(): void {
  grid.value?.downloadCsv(exportName('csv'))
}

function excel(): void {
  void grid.value?.downloadXlsx(exportName('xlsx'), t('journal.sheet'))
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="case-journal">
    <div
      v-if="state.pivot"
      key="pivot"
      class="alert alert-soft flex flex-wrap items-center gap-3"
      role="status"
      data-testid="journal-pivot"
    >
      <span class="text-sm">
        {{ t('journal.pivot.showing', { key: state.pivot.key, value: state.pivot.value }) }}
      </span>
      <button
        type="button"
        class="btn btn-sm"
        data-testid="journal-pivot-back"
        @click="change({ ...state, pivot: null, transaction: null })"
      >
        <Undo2 class="size-4" aria-hidden="true" />
        {{ t('journal.pivot.back', { key: caseKey }) }}
      </button>
    </div>

    <JournalWindow :state="state" @change="change" />

    <JournalStrip
      :analytics="analytics.data.value ?? null"
      :failed="analytics.isError.value"
      @open="open"
    />

    <JournalPeers
      v-if="analytics.data.value?.peers"
      key="peers"
      :peers="analytics.data.value.peers"
      :fields="analytics.data.value.fields"
    />

    <JournalCharts
      v-if="analytics.data.value && analytics.data.value.meta.rowCount > 0"
      key="charts"
      :query="query"
      :fields="analytics.data.value.fields"
    />

    <section
      class="flex min-w-0 flex-col gap-4 rounded-box border border-base-300 bg-base-100 p-3"
      data-testid="journal-card"
    >
      <JournalScope
        :state="state"
        :workflow="workflow"
        :primary="primary"
        :keys="rows.data.value?.meta.drillKeys ?? []"
        :shown="rows.data.value?.meta.rowCount ?? 0"
        :total="rows.data.value?.meta.total ?? 0"
        @change="change"
      >
        <JournalSavedViews
          :case-id="caseId"
          :state="state"
          @load="change({ ...state, ...$event })"
        />
      </JournalScope>

      <section class="flex min-w-0 flex-col gap-3" aria-labelledby="journal-transactions">
        <h3 id="journal-transactions" class="text-sm font-semibold">
          {{ t('journal.section.transactions') }}
        </h3>

        <JournalControls
          :state="state"
          :lenses="rows.data.value?.lenses ?? []"
          :pin-count="pinCount"
          :empty="everyRow.length === 0"
          :formatted="everyRow.some((row) => row.__paint !== null)"
          @change="change"
          @csv="csv"
          @excel="excel"
        >
          <JournalColumns
            :choices="choices"
            :customised="lensEntries !== undefined"
            @toggle="columnShown"
            @reset="changeLayout(undefined)"
          />
          <template #tags>
            <JournalTagBar
              :count="selected.size"
              :total="everyRow.length"
              :present="present"
              :options="tagOptions.data.value?.tag ?? []"
              :options-failed="tagOptions.isError.value"
              :busy="tagging.isPending.value || restoring.isPending.value"
              :undo-text="undo?.text ?? null"
              @select-all="selectAll"
              @clear="clearSelection"
              @apply="tagging.mutate($event)"
              @undo="undo && restoring.mutate(undo.sets)"
              @dismiss="undo = null"
            >
              <JournalTagAll
                :preview="preview.data.value ?? null"
                :pending="preview.isPending.value && allOpen"
                :failed="preview.isError.value"
                :options="tagOptions.data.value?.tag ?? []"
                :busy="jobBusy || (job.data.value ? isActive(job.data.value) : false)"
                :disabled="records.length === 0 || state.pinnedOnly"
                :filtered="state.filter !== null"
                @open="allOpen = true"
                @close="allOpen = false"
                @apply="submitting.mutate($event)"
              />
            </JournalTagBar>
          </template>
        </JournalControls>

        <JournalTagJob
          v-if="job.data.value"
          key="job"
          :job="job.data.value"
          :busy="jobBusy"
          @cancel="stopping.mutate(job.data.value.guid)"
          @undo="reversing.mutate(job.data.value.guid)"
          @dismiss="jobGuid = null"
        />

        <div v-if="rows.isPending.value" key="loading" class="skeleton h-40 w-full" role="status">
          <span class="sr-only">{{ t('cases.loading') }}</span>
        </div>
        <div v-else-if="rows.isError.value" key="failed" role="alert" class="alert alert-error">
          <span>{{ t('journal.failed') }}</span>
          <button type="button" class="btn btn-sm" @click="rows.refetch()">
            {{ t('chart.retry') }}
          </button>
        </div>
        <DataGrid
          v-else
          key="grid"
          ref="grid"
          :columns="columns"
          :rows="everyRow"
          :empty-text="t('journal.empty')"
          :label="t('journal.label')"
          :page-size="100"
          :pager-labels="pagerLabels"
          row-key="__row"
          movable
          :row-paint="rowPaint"
          :mark="marked"
          @action="act"
          @column-moved="columnMoved"
          @column-resized="columnResized"
        />
      </section>

      <section
        v-if="rows.data.value?.meta.location"
        key="mapblock"
        ref="mapBlock"
        class="flex min-w-0 flex-col gap-3"
        data-testid="journal-map-block"
      >
        <h3 id="journal-map-heading" class="text-sm font-semibold">
          {{ t('journal.section.map') }}
        </h3>

        <JournalMapOptions :state="state" @change="change" @fit="mapView?.fit()" />

        <JournalMap
          ref="mapView"
          :workflow="workflow"
          :case-id="caseId"
          :primary="primary"
          :state="state"
          :location="rows.data.value.meta.location"
          :focus="mapFocus"
          @select="mapSelected = $event"
          @open-transaction="open"
        />
      </section>
    </section>

    <Teleport to="body">
      <TransactionDrawer
        v-if="state.transaction !== null"
        key="drawer"
        :workflow="workflow"
        :id="state.transaction"
        :lanes="lanes"
        :can-add-lane="state.merge.length < maximumLanes"
        :paint="openPaint"
        :can-show-on-map="rows.data.value?.meta.location != null"
        @show-on-map="showOnMap"
        @close="close"
        @pivot="pivot"
        @merge="addLane"
        @open="open"
      />
    </Teleport>
  </section>
</template>
