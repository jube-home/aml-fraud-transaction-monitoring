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
import GridStatistics from '../../components/GridStatistics.vue'
import type { PayloadStatistics, StatisticsLabels } from '../../statistics/model'

const { t } = useI18n()

const sample = {
  columns: {
    queueDepth: {
      min: 0,
      max: 412,
      mean: 96.4,
      median: 60,
      standardDeviation: 88.12,
      histogram: [
        { bin: 0, frequency: 120 },
        { bin: 41.2, frequency: 80 },
        { bin: 82.4, frequency: 54 },
        { bin: 123.6, frequency: 31 },
        { bin: 164.8, frequency: 18 },
        { bin: 206, frequency: 9 },
        { bin: 247.2, frequency: 5 },
        { bin: 288.4, frequency: 3 },
        { bin: 329.6, frequency: 1 },
        { bin: 370.8, frequency: 1 },
      ],
    },
    droppedCount: {
      min: 0,
      max: 0,
      mean: 0,
      median: 0,
      standardDeviation: 0,
      histogram: [],
    },
  },
}

const text = ref(JSON.stringify(sample, null, 2))
const labelsText = ref(JSON.stringify({ queueDepth: 'Queue depth' }, null, 2))

const labels = computed<{ value?: StatisticsLabels | null; failed: boolean }>(() => {
  try {
    return { value: JSON.parse(labelsText.value) as StatisticsLabels | null, failed: false }
  } catch {
    return { failed: true }
  }
})

const parsed = computed<{ value?: PayloadStatistics | null; failed: boolean }>(() => {
  try {
    return { value: JSON.parse(text.value) as PayloadStatistics | null, failed: false }
  } catch {
    return { failed: true }
  }
})
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <label class="flex flex-col gap-1 text-sm">
      <span class="label">{{ t('statistics.bench.json') }}</span>
      <textarea
        v-model="text"
        rows="8"
        class="textarea w-full font-mono text-xs"
        data-testid="bench-json"
      ></textarea>
    </label>
    <label class="flex flex-col gap-1 text-sm">
      <span class="label">{{ t('statistics.bench.labels') }}</span>
      <textarea
        v-model="labelsText"
        rows="3"
        class="textarea w-full font-mono text-xs"
        data-testid="bench-labels"
      ></textarea>
    </label>
    <p
      v-if="parsed.failed || labels.failed"
      role="alert"
      class="alert alert-error"
      data-testid="bench-json-error"
    >
      {{ t('statistics.bench.invalid') }}
    </p>
    <GridStatistics v-else :statistics="parsed.value" :labels="labels.value" />
  </div>
</template>
