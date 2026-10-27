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
import { computed, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { directionFor } from '../../i18n'
import { useFormat } from '../../composables/useFormat'
import DataGrid from '../../components/DataGrid.vue'
import GridExportButtons from '../../components/GridExportButtons.vue'
import type { GridColumn } from '../../components/dataGridTypes'

const { t, locale } = useI18n()
const { dateTime } = useFormat()

const grid = useTemplateRef<{
  downloadCsv: (filename: string) => void
  downloadXlsx: (filename: string, sheetName?: string) => Promise<void>
}>('grid')

const direction = computed(() => directionFor(locale.value))

const rows = [
  { id: 1, source: 'waf', detail: 'plain text', hits: 12, seen: '2026-10-20T10:30:00Z' },
  {
    id: 2,
    source: 'waf',
    detail: '=HYPERLINK("http://x.test","click")',
    hits: 3,
    seen: '2026-10-21T08:00:00Z',
  },
  { id: 3, source: 'agent', detail: 'comma, "quote" and\nnewline', hits: -4, seen: null },
  { id: 4, source: 'agent', detail: '@SUM(1+1)', hits: 0, seen: null },
  { id: 5, source: 'agent', detail: '-5', hits: 7, seen: '2026-10-22T23:59:00Z' },
]

const columns = computed<GridColumn[]>(() => [
  { field: 'id', title: t('csvExport.col.id'), mono: true, width: 80, type: 'number' },
  { field: 'source', title: t('csvExport.col.source'), width: 120 },
  { field: 'detail', title: t('csvExport.col.detail'), minWidth: 260 },
  {
    field: 'hits',
    title: t('csvExport.col.hits'),
    align: 'end',
    mono: true,
    width: 100,
    type: 'number',
  },
  {
    field: 'seen',
    title: t('csvExport.col.seen'),
    width: 190,
    mono: true,
    type: 'date',
    format: (value) => (value ? dateTime(String(value)) : ''),
  },
  {
    field: '',
    title: t('csvExport.col.actions'),
    width: 140,
    pinned: true,
    actions: () => [{ id: 'noop', label: t('csvExport.row') }],
  },
])
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <p class="text-sm">{{ t('csvExport.note') }}</p>
    <div>
      <GridExportButtons
        @csv="grid?.downloadCsv('evidence.csv')"
        @excel="grid?.downloadXlsx('evidence.xlsx', t('csvExport.title'))"
      />
    </div>
    <DataGrid
      ref="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('csvExport.empty')"
      :label="t('csvExport.title')"
    />
  </div>
</template>
