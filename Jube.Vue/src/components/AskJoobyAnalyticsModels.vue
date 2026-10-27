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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { analyticsApi } from '../askJoobyAnalytics/api'
import { useAnalyticsLabels } from '../askJoobyAnalytics/labels'
import type { TrainedModel } from '../askJoobyAnalytics/types'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ instanceId: number }>()

const emit = defineEmits<{ open: [model: TrainedModel] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()

const { kindLabel, sourceLabel } = useAnalyticsLabels()

const direction = computed(() => directionFor(locale.value))

const models = useQuery({
  queryKey: computed(() => queryKeys.analyticsModels(props.instanceId)),
  queryFn: () => analyticsApi.models(props.instanceId),
  retry: false,
})

const rows = computed(() => models.data.value ?? [])

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('analytics.models.name'), minWidth: 200 },
  {
    field: 'kind',
    title: t('analytics.models.kind'),
    width: 170,
    format: (value) => kindLabel(Number(value)),
  },
  {
    field: 'source',
    title: t('analytics.models.source'),
    width: 110,
    format: (value) => sourceLabel(Number(value)),
  },
  {
    field: 'activeVersionNumber',
    title: t('analytics.models.live'),
    width: 130,
    format: (value) => (value ? `v${value}` : t('analytics.models.none')),
    badge: (value) =>
      value
        ? { label: `v${value}`, tone: 'success' }
        : { label: t('analytics.models.none'), tone: 'neutral' },
  },
  {
    field: 'createdDate',
    title: t('analytics.models.created'),
    mono: true,
    width: 190,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
  {
    field: 'open',
    title: t('analytics.models.actions'),
    width: 140,
    actions: () => [{ id: 'open', label: t('analytics.models.open'), tone: 'primary' }],
  },
])

function act(event: GridActionEvent): void {
  if (event.id === 'open') emit('open', event.row as TrainedModel)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" :data-testid="`analytics-models-${instanceId}`">
    <ProblemAlert
      v-if="models.isError.value"
      key="failed"
      :reasons="[t('analytics.models.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('analytics.models.empty')"
      :label="t('analytics.models.title')"
      auto-height
      @action="act"
    />
  </div>
</template>
