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
import { comparable, differences, movementOf, type Movement } from '../askJoobyAnalytics/model'
import DataGrid from './DataGrid.vue'
import type { GridBadge, GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ liveId: number; otherId: number }>()

const { t, locale } = useI18n()
const { fixed } = useFormat()

const direction = computed(() => directionFor(locale.value))

const comparison = useQuery({
  queryKey: computed(() => queryKeys.analyticsCompare(props.liveId, props.otherId)),
  queryFn: () => analyticsApi.compare(props.liveId, props.otherId),
  retry: false,
})

const data = computed(() => comparison.data.value)
const different = computed(() => (data.value ? differences(data.value) : []))

const rows = computed(() =>
  (data.value?.metrics ?? []).map((metric) => ({ ...metric, movement: movementOf(metric) })),
)

function metricLabel(name: string): string {
  switch (name) {
    case 'TestRocAuc':
      return t('analytics.metric.rocAuc')
    case 'TestAveragePrecision':
      return t('analytics.metric.averagePrecision')
    case 'TestLogLoss':
      return t('analytics.metric.logLoss')
    case 'TestBrierScore':
      return t('analytics.metric.brier')
    case 'TestExpectedCalibrationError':
      return t('analytics.metric.ece')
    case 'TestCalibrationSlope':
      return t('analytics.metric.calibrationSlope')
    case 'TestMatthewsCorrelation':
      return t('analytics.metric.matthews')
    case 'TestPrecision':
      return t('analytics.metric.precision')
    case 'TestRecall':
      return t('analytics.metric.recall')
    default:
      return name
  }
}

function movementBadge(value: unknown): GridBadge {
  switch (value as Movement) {
    case 'better':
      return { label: t('analytics.compare.better'), tone: 'success' }
    case 'worse':
      return { label: t('analytics.compare.worse'), tone: 'error' }
    case 'same':
      return { label: t('analytics.compare.same'), tone: 'neutral' }
    default:
      return { label: t('analytics.compare.unknown'), tone: 'neutral' }
  }
}

const number = (value: unknown) => (typeof value === 'number' ? fixed(value, 4) : '–')

const columns = computed<GridColumn[]>(() => [
  {
    field: 'name',
    title: t('analytics.compare.metric'),
    minWidth: 180,
    format: (value) => metricLabel(String(value)),
  },
  {
    field: 'valueA',
    title: t('analytics.compare.live', { version: data.value?.versionA.versionNumber ?? '' }),
    align: 'end',
    mono: true,
    width: 110,
    format: number,
  },
  {
    field: 'valueB',
    title: t('analytics.compare.other', { version: data.value?.versionB.versionNumber ?? '' }),
    align: 'end',
    mono: true,
    width: 110,
    format: number,
  },
  {
    field: 'difference',
    title: t('analytics.compare.change'),
    align: 'end',
    mono: true,
    width: 110,
    format: (value) =>
      typeof value === 'number' ? `${value > 0 ? '+' : ''}${fixed(value, 4)}` : '–',
  },
  { field: 'movement', title: t('analytics.compare.verdict'), width: 130, badge: movementBadge },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-comparison">
    <div
      v-if="comparison.isPending.value"
      key="loading"
      class="skeleton h-24 w-full"
      role="status"
      :aria-label="t('analytics.compare.loading')"
    ></div>
    <ProblemAlert
      v-else-if="comparison.isError.value"
      key="failed"
      :reasons="[t('analytics.compare.failed')]"
    />
    <template v-else-if="data">
      <h4 class="text-sm font-semibold">
        {{
          t('analytics.compare.title', {
            live: data.versionA.versionNumber,
            other: data.versionB.versionNumber,
          })
        }}
      </h4>

      <div
        v-if="!comparable(data)"
        key="not-comparable"
        role="alert"
        class="alert alert-warning items-start"
        data-testid="analytics-not-comparable"
      >
        <div class="flex flex-col gap-1">
          <p class="font-semibold">{{ t('analytics.compare.notComparable') }}</p>
          <ul class="text-sm">
            <li v-for="reason in different" :key="reason">
              {{
                reason === 'seed'
                  ? t('analytics.compare.differentSeed')
                  : reason === 'splits'
                    ? t('analytics.compare.differentSplits')
                    : t('analytics.compare.differentVariables')
              }}
            </li>
          </ul>
        </div>
      </div>
      <p v-else key="comparable" class="text-sm" data-testid="analytics-comparable">
        {{ t('analytics.compare.comparable') }}
      </p>

      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('analytics.compare.empty')"
        :label="
          t('analytics.compare.title', {
            live: data.versionA.versionNumber,
            other: data.versionB.versionNumber,
          })
        "
        row-key="name"
        auto-height
      />

      <p v-if="data.warningsOnlyInA.length > 0" key="only-live" class="text-sm">
        {{ t('analytics.compare.onlyLive', { codes: data.warningsOnlyInA.join(', ') }) }}
      </p>
      <p v-if="data.warningsOnlyInB.length > 0" key="only-other" class="text-sm">
        {{ t('analytics.compare.onlyOther', { codes: data.warningsOnlyInB.join(', ') }) }}
      </p>
    </template>
  </div>
</template>
