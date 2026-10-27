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
import { useExpandHosts } from '../composables/useExpandHosts'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { exhaustiveApi } from '../exhaustive/api'
import { groupPrescriptions } from '../exhaustive/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ promotedId: number; version: string }>()

const { t, locale } = useI18n()
const { fixed } = useFormat()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))

const prescriptions = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.promotedId, 'prescriptions', props.version)),
  queryFn: () => exhaustiveApi.prescriptions(props.promotedId),
  retry: false,
})

const rows = computed(() => groupPrescriptions(prescriptions.data.value ?? []))

const figure = (value: unknown) => (typeof value === 'number' ? fixed(value, 3) : '')
const numeric = (field: string, title: string): GridColumn => ({
  field,
  title,
  align: 'end',
  mono: true,
  width: 120,
  format: figure,
})

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('exhaustive.prescription.variable'), minWidth: 200 },
  numeric('variableMean', t('exhaustive.prescription.variableMean')),
  numeric('variableStandardDeviation', t('exhaustive.prescription.variableSd')),
  numeric('variableMinimum', t('exhaustive.prescription.variableMin')),
  numeric('variableMaximum', t('exhaustive.prescription.variableMax')),
  numeric('positivePrescriptionMean', t('exhaustive.prescription.positiveMean')),
  numeric('positivePrescriptionStandardDeviation', t('exhaustive.prescription.positiveSd')),
  numeric('negativePrescriptionMean', t('exhaustive.prescription.negativeMean')),
  numeric('negativePrescriptionStandardDeviation', t('exhaustive.prescription.negativeSd')),
  numeric('sensitivity', t('exhaustive.prescription.sensitivity')),
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" :data-testid="`prescriptions-${promotedId}`">
    <ProblemAlert
      v-if="prescriptions.isError.value"
      key="failed"
      :reasons="[t('exhaustive.prescription.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('exhaustive.prescription.empty')"
      :label="t('exhaustive.prescription.title')"
      expandable
      auto-height
      :expand-labels="{
        expand: t('exhaustive.prescription.show'),
        collapse: t('exhaustive.prescription.hide'),
      }"
      @expand="onExpand"
    />
    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <div class="grid min-w-0 gap-4 @container @3xl:grid-cols-2">
          <ServerChart
            :url="`/api/Chart/Exhaustive/PrescriptionHistogram/${promotedId}/${rows.find((row) => row.id === id)?.chartId ?? 0}`"
            :label="t('exhaustive.prescription.histogram')"
            :version="version"
          />
          <ServerChart
            :url="`/api/Chart/Exhaustive/PrescriptionSummary/${promotedId}/${rows.find((row) => row.id === id)?.chartId ?? 0}`"
            :label="t('exhaustive.prescription.summary')"
            :version="version"
          />
        </div>
      </Teleport>
    </template>
  </div>
</template>
