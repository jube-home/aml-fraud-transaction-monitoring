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
import DataGrid from '../../components/DataGrid.vue'
import type {GridColumn} from '../../components/dataGridTypes'
import {useFormat} from '../../composables/useFormat'
import {usePagerLabels} from '../../composables/usePagerLabels'
import {historyActionKey} from './historyText'
import type {ChecklistHistoryEntry} from './types'

defineProps<{ entries: ChecklistHistoryEntry[]; loading: boolean; failed: boolean }>()

const {t} = useI18n()
const {dateTime} = useFormat()
const pagerLabels = usePagerLabels()

const columns = computed<GridColumn[]>(() => [
  {
    field: 'actionId',
    title: t('caseDetail.checklist.history.columns.action'),
    minWidth: 160,
    format: (v) => t(historyActionKey(Number(v))),
  },
  {
    field: 'itemName',
    title: t('caseDetail.checklist.history.columns.item'),
    minWidth: 200,
    format: (v) => (v ? String(v) : t('caseDetail.checklist.history.wholeList')),
  },
  {
    field: 'actor',
    title: t('caseDetail.checklist.history.columns.user'),
    mono: true,
    minWidth: 140,
    format: (v) => (v ? String(v) : t('caseDetail.checklist.history.system')),
  },
  {field: 'detail', title: t('caseDetail.checklist.history.columns.detail'), minWidth: 240},
  {
    field: 'createdDate',
    title: t('caseDetail.checklist.history.columns.when'),
    type: 'date',
    mono: true,
    minWidth: 150,
    format: (v) => dateTime(String(v)),
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="checklist-history">
    <h3 class="text-sm font-semibold">{{ t('caseDetail.checklist.history.title') }}</h3>
    <div v-if="loading" key="loading" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <div
      v-else-if="failed"
      key="failed"
      role="alert"
      class="alert alert-error"
      data-testid="checklist-history-error"
    >
      <span>{{ t('caseDetail.checklist.history.error') }}</span>
    </div>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="entries"
      :empty-text="t('caseDetail.checklist.history.empty')"
      :label="t('caseDetail.checklist.history.title')"
      :page-size="25"
      :pager-labels="pagerLabels"
      row-key="id"
    />
  </div>
</template>
