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
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {hasPermission, meQueryOptions} from '../auth/me'
import DataGrid from '../components/DataGrid.vue'
import GridExportButtons from '../components/GridExportButtons.vue'
import type {GridActionEvent, GridColumn} from '../components/dataGridTypes'
import {useFormat} from '../composables/useFormat'
import {useToasts} from '../stores/toasts'
import {usePagerLabels} from '../composables/usePagerLabels'
import {
  caseCell,
  dueCell,
  lockCell,
  riskCell,
  valueText,
  whyCell,
  type CellContext,
} from './cells'
import {sortKeys} from './labels'
import BulkCloseDialog from './BulkCloseDialog.vue'
import BulkReassignDialog from './BulkReassignDialog.vue'
import {bulkApi, bulkReassignPermission, skipSummary, type BulkResult} from './bulkApi'
import {useCaseSelection} from './caseSelection'
import type {CasePage, CaseRow} from './types'

const props = defineProps<{ page: CasePage; sort: number; editable: boolean; exporting: boolean }>()

const emit = defineEmits<{
  sort: [sort: number]
  peek: [caseId: number]
  open: [caseId: number]
  edit: []
  exportCsv: []
  exportExcel: []
}>()

const {t} = useI18n()
const {number, percent} = useFormat()
const pagerLabels = usePagerLabels()
const toasts = useToasts()
const client = useQueryClient()
const selection = useCaseSelection()
const panel = ref<'close' | 'reassign' | null>(null)
const me = useQuery(meQueryOptions)
const canReassign = computed(() => hasPermission(me.data.value, bulkReassignPermission))

watch(
  () => props.page.rows,
  (rows) => selection.keepOnly(rows.map((row) => (row as CaseRow).caseId)),
)

function selectCell(row: CaseRow): HTMLElement {
  const box = document.createElement('input')

  box.type = 'checkbox'
  box.className = 'checkbox checkbox-sm'
  box.checked = selection.has(row.caseId)
  box.dataset.testid = 'cases-select'
  box.setAttribute('aria-label', t('cases.bulk.select'))
  box.addEventListener('change', () => {
    if (!selection.toggle(row.caseId, box.checked)) {
      box.checked = false
      toasts.info(t('cases.bulk.limit'))
    }
  })

  return box
}

function report(result: BulkResult): void {
  panel.value = null
  selection.clear()
  void client.invalidateQueries({queryKey: ['service', 'Case']})

  if (result.skipped === 0) {
    toasts.success(t('cases.bulk.done', {done: number(result.done)}))

    return
  }

  const reasons = [...skipSummary(result)]
    .map(([id, n]) => `${t(`cases.bulk.skip.${id}`)} (${number(n)})`)
    .join('; ')

  toasts.info(
    t('cases.bulk.partial', {
      done: number(result.done),
      skipped: number(result.skipped),
      reasons,
    }),
  )
}

const closeSelected = useMutation({
  mutationFn: (input: { reasonId: number; note: string | null }) =>
    bulkApi.close(selection.ids(), input.reasonId, input.note),
  onSuccess: report,
  onError: (error) => toasts.apiError(error),
})

const reassignSelected = useMutation({
  mutationFn: (toUser: string) => bulkApi.reassign(selection.ids(), toUser),
  onSuccess: report,
  onError: (error) => toasts.apiError(error),
})

const nowMs = ref(Date.now())

const context = computed<CellContext>(() => ({
  t,
  number,
  nowMs: nowMs.value,
  percent,
}))

const columns = computed<GridColumn[]>(() => [
  {
    field: 'select',
    title: t('cases.bulk.select'),
    hiddenTitle: t('cases.bulk.select'),
    width: 52,
    draw: (row) => selectCell(row as CaseRow),
  },
  {
    field: 'risk',
    title: t('cases.column.risk'),
    width: 230,
    draw: (row) => riskCell(row as CaseRow, context.value),
  },
  {
    field: 'caseId',
    title: t('cases.column.case'),
    width: 190,
    draw: (row) => caseCell(row as CaseRow, context.value),
  },
  {
    field: 'attention',
    title: t('cases.column.why'),
    minWidth: 260,
    draw: (row) => whyCell(row as CaseRow, context.value),
  },
  {
    field: 'value',
    title: t('cases.column.value'),
    align: 'end',
    mono: true,
    width: 110,
    draw: (row) => {
      const cell = document.createElement('span')

      cell.className = 'font-mono text-sm tabular-nums'
      cell.textContent = valueText(row as CaseRow, context.value)

      return cell
    },
  },
  {
    field: 'serviceLevel',
    title: t('cases.column.due'),
    width: 200,
    draw: (row) => dueCell(row as CaseRow, context.value),
  },
  {
    field: 'statusName',
    title: t('cases.column.status'),
    width: 160,
    draw: (row) => lockCell(row as CaseRow, context.value),
  },
  {
    field: 'actions',
    title: t('cases.column.actions'),
    hiddenTitle: t('cases.column.actions'),
    width: 150,
    pinned: true,
    actions: () => [
      {id: 'peek', label: t('cases.action.peek')},
      {id: 'open', label: t('cases.action.open'), tone: 'primary'},
    ],
  },
])

const sorts = computed(() =>
  Object.entries(sortKeys).map(([value, key]) => ({value: Number(value), label: t(key)})),
)

function act(event: GridActionEvent): void {
  const caseId = (event.row as CaseRow).caseId

  if (event.id === 'peek') emit('peek', caseId)
  else emit('open', caseId)
}

function pick(event: Event): void {
  emit('sort', Number((event.target as HTMLSelectElement).value))
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="cases-list">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <p class="font-mono text-xs tabular-nums text-base-content/70" data-testid="cases-total">
        {{
          page.total > page.rows.length
            ? t('cases.list.firstOf', {n: number(page.rows.length), total: number(page.total)})
            : t('cases.list.all', {total: number(page.total)})
        }}
      </p>
      <button
        v-if="editable"
        key="edit"
        type="button"
        class="btn btn-sm"
        data-testid="cases-edit-search"
        @click="emit('edit')"
      >
        {{ t('cases.list.editSearch') }}
      </button>
      <GridExportButtons
        :disabled="page.rows.length === 0 || exporting"
        @csv="emit('exportCsv')"
        @excel="emit('exportExcel')"
      />
      <label class="flex items-center gap-2">
        <span class="text-xs">{{ t('cases.sort.label') }}</span>
        <select class="select select-sm" :value="sort" data-testid="cases-sort" @change="pick">
          <option v-for="option in sorts" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
    </div>

    <div
      v-if="selection.count.value > 0"
      key="bulk"
      class="flex flex-wrap items-center gap-2 rounded-box bg-base-200 p-2"
      data-testid="cases-bulk-bar"
    >
      <span class="font-mono text-xs tabular-nums" data-testid="cases-bulk-count">{{
          t('cases.bulk.selected', {count: number(selection.count.value)})
        }}</span>
      <button
        type="button"
        class="btn btn-sm"
        data-testid="cases-bulk-close"
        @click="panel = 'close'"
      >
        {{ t('cases.bulk.close') }}
      </button>
      <button
        v-if="canReassign"
        type="button"
        class="btn btn-sm"
        data-testid="cases-bulk-reassign"
        @click="panel = 'reassign'"
      >
        {{ t('cases.bulk.reassign') }}
      </button>
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        data-testid="cases-bulk-clear"
        @click="selection.clear()"
      >
        {{ t('cases.bulk.clear') }}
      </button>
    </div>

    <BulkCloseDialog
      :open="panel === 'close'"
      :count="selection.count.value"
      :busy="closeSelected.isPending.value"
      @confirm="(reasonId, note) => closeSelected.mutate({ reasonId, note })"
      @cancel="panel = null"
    />
    <BulkReassignDialog
      v-if="canReassign"
      :open="panel === 'reassign'"
      :count="selection.count.value"
      :busy="reassignSelected.isPending.value"
      @confirm="(toUser) => reassignSelected.mutate(toUser)"
      @cancel="panel = null"
    />

    <div class="h-[max(18rem,calc(100dvh-31rem))] min-h-0">
      <DataGrid
        fill
        :columns="columns"
        :rows="page.rows"
        :empty-text="t('cases.list.empty')"
        :label="t('cases.list.label')"
        :page-size="100"
        :pager-labels="pagerLabels"
        row-key="caseId"
        @action="act"
      />
    </div>
  </section>
</template>
