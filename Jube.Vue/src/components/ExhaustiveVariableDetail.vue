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
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { exhaustiveApi } from '../exhaustive/api'
import { correlationTone, sortedVariance } from '../exhaustive/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ServerChart from './ServerChart.vue'

const props = defineProps<{
  instanceId: number
  variableId: number
  counts: boolean
  version: string
}>()

const { t, locale } = useI18n()
const { fixed } = useFormat()

const direction = computed(() => directionFor(locale.value))

const variance = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'variance', props.variableId)),
  queryFn: () => exhaustiveApi.variance(props.variableId),
  retry: false,
})

const rows = computed(() => sortedVariance(variance.data.value ?? []))
const classes = computed(() => `?large=true&counts=${props.counts ? 'true' : 'false'}`)
const route = computed(() => `${props.instanceId}/${props.variableId}`)

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('exhaustive.variables.correlatedWith'), minWidth: 220 },
  {
    field: 'correlation',
    title: t('exhaustive.variables.correlation'),
    align: 'end',
    mono: true,
    width: 130,
    badge: (value) => ({
      label: fixed(Number(value), 3),
      tone: correlationTone(Number(value)),
    }),
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" :data-testid="`variable-detail-${variableId}`">
    <div class="grid min-w-0 gap-4 @container @3xl:grid-cols-2">
      <ServerChart
        :url="`/api/Chart/Exhaustive/VariableHistogram/${route}`"
        :label="t('exhaustive.variables.histogram')"
        :version="version"
      />
      <ServerChart
        :url="`/api/Chart/ExhaustiveStatistics/VariableClasses/${route}${classes}`"
        :label="t('exhaustive.variables.classes')"
        :version="version"
      />
      <ServerChart
        :url="`/api/Chart/ExhaustiveStatistics/MonteCarlo/${route}${classes}`"
        :label="t('exhaustive.variables.monteCarlo')"
        :version="version"
      />
      <ServerChart
        :url="`/api/Chart/ExhaustiveStatistics/MonteCarloSummary/${route}?large=true`"
        :label="t('exhaustive.variables.monteCarloSummary')"
        :version="version"
      />
    </div>
    <DataGrid
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('exhaustive.variables.noCorrelated')"
      :label="t('exhaustive.variables.correlatedWith')"
      row-key="name"
      auto-height
    />
  </div>
</template>
