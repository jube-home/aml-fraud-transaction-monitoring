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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import DataGrid from '../../components/DataGrid.vue'
import type { GridColumn } from '../../components/dataGridTypes'
import { useFormat } from '../../composables/useFormat'
import { usePagerLabels } from '../../composables/usePagerLabels'
import { caseLabel } from '../labels'
import type { CaseEventEntry, CaseKeyCase } from './types'

defineProps<{
  cases: CaseKeyCase[]
  events: CaseEventEntry[]
  loading: boolean
  failed: boolean
}>()

const { t } = useI18n()
const { dateTime } = useFormat()
const pagerLabels = usePagerLabels()

const caseColumns = computed<GridColumn[]>(() => [
  {
    field: 'id',
    title: t('caseDetail.history.case'),
    mono: true,
    width: 110,
    format: (v) => caseLabel(Number(v)),
  },
  { field: 'caseWorkflowStatusName', title: t('caseDetail.history.status'), minWidth: 160 },
  { field: 'lockedUser', title: t('caseDetail.history.heldBy'), mono: true, minWidth: 140 },
  {
    field: 'createdDate',
    title: t('caseDetail.history.created'),
    type: 'date',
    mono: true,
    minWidth: 150,
    format: (v) => dateTime(String(v)),
  },
  { field: 'closedUser', title: t('caseDetail.history.closedBy'), mono: true, minWidth: 140 },
])

const eventColumns = computed<GridColumn[]>(() => [
  {
    field: 'caseId',
    title: t('caseDetail.history.case'),
    mono: true,
    width: 110,
    format: (v) => caseLabel(Number(v)),
  },
  { field: 'caseEventType', title: t('caseDetail.history.event'), minWidth: 180 },
  { field: 'before', title: t('caseDetail.history.before'), minWidth: 160 },
  { field: 'after', title: t('caseDetail.history.after'), minWidth: 160 },
  { field: 'createdUser', title: t('caseDetail.history.user'), mono: true, minWidth: 140 },
  {
    field: 'createdDate',
    title: t('caseDetail.history.when'),
    type: 'date',
    mono: true,
    minWidth: 150,
    format: (v) => dateTime(String(v)),
  },
])
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="case-history">
    <div v-if="loading" key="loading" class="skeleton h-32 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <div v-else-if="failed" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.history.failed') }}</span>
    </div>
    <template v-else>
      <div class="flex min-w-0 flex-col gap-2" data-testid="case-history-cases">
        <h3 class="text-sm font-semibold">{{ t('caseDetail.history.casesTitle') }}</h3>
        <DataGrid
          :columns="caseColumns"
          :rows="cases"
          :empty-text="t('caseDetail.history.casesEmpty')"
          :label="t('caseDetail.history.casesTitle')"
          :page-size="25"
          :pager-labels="pagerLabels"
          row-key="id"
        />
      </div>
      <div class="flex min-w-0 flex-col gap-2" data-testid="case-history-events">
        <h3 class="text-sm font-semibold">{{ t('caseDetail.history.eventsTitle') }}</h3>
        <DataGrid
          :columns="eventColumns"
          :rows="events"
          :empty-text="t('caseDetail.history.eventsEmpty')"
          :label="t('caseDetail.history.eventsTitle')"
          :page-size="25"
          :pager-labels="pagerLabels"
          row-key="id"
        />
      </div>
    </template>
  </section>
</template>
