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
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { exhaustiveApi } from '../exhaustive/api'
import { chartUrl, performanceCharts, variableLabel } from '../exhaustive/model'
import ExhaustiveNarrative from './ExhaustiveNarrative.vue'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ instanceId: number; version: string }>()

const { t } = useI18n()
const id = useId()

const basisId = ref<number | null>(null)

const variables = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'variables', props.version)),
  queryFn: () => exhaustiveApi.variables(props.instanceId),
  retry: false,
})

const choices = computed(() =>
  (variables.data.value ?? []).map((variable) => ({
    id: variable.id,
    name: variable.name ?? '',
    label: variableLabel(variable),
  })),
)

const basis = computed(() => {
  const found = choices.value.find((choice) => choice.id === basisId.value)

  return found ? { id: found.id, name: found.name } : null
})

const charts = computed(() => performanceCharts(basis.value?.id ?? null))

function chartLabel(key: string): string {
  switch (key) {
    case 'measures':
      return t('exhaustive.performance.measures')
    case 'confusion':
      return t('exhaustive.performance.confusion')
    case 'scoreDistribution':
      return t('exhaustive.performance.scoreDistribution')
    case 'roc':
      return t('exhaustive.performance.roc')
    case 'precisionRecall':
      return t('exhaustive.performance.precisionRecall')
    case 'calibration':
      return t('exhaustive.performance.calibration')
    case 'gains':
      return t('exhaustive.performance.gains')
    case 'lift':
      return t('exhaustive.performance.lift')
    case 'ks':
      return t('exhaustive.performance.ks')
    case 'thresholdSweep':
      return t('exhaustive.performance.thresholdSweep')
    default:
      return t('exhaustive.performance.valueCaptured')
  }
}

function pick(event: Event): void {
  const value = (event.target as HTMLSelectElement).value

  basisId.value = value === '' ? null : Number(value)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="exhaustive-performance">
    <div class="flex flex-col gap-1">
      <label class="flex flex-col gap-1" :for="`${id}-basis`">
        <span class="label text-xs">{{ t('exhaustive.performance.basis') }}</span>
        <select
          :id="`${id}-basis`"
          class="select select-sm w-full max-w-md"
          :value="basisId === null ? '' : String(basisId)"
          data-testid="exhaustive-basis"
          @change="pick"
        >
          <option value="">{{ t('exhaustive.performance.count') }}</option>
          <option v-for="choice in choices" :key="choice.id" :value="String(choice.id)">
            {{ choice.label }}
          </option>
        </select>
      </label>
      <p class="max-w-prose text-xs text-secondary">{{ t('exhaustive.performance.basisHint') }}</p>
    </div>

    <div class="grid min-w-0 gap-6 @container @3xl:grid-cols-2">
      <div
        v-for="chart in charts"
        :key="chart.key"
        class="flex min-w-0 flex-col gap-2"
        :data-testid="`performance-${chart.key}`"
      >
        <ExhaustiveNarrative
          v-if="chart.narrative"
          :instance-id="instanceId"
          :group="chart.narrative"
          :version="version"
        />
        <ServerChart
          :url="chartUrl(chart.route, instanceId, chart.valueBased, basis)"
          :label="chartLabel(chart.key)"
          :version="version"
        />
      </div>
    </div>
  </div>
</template>
