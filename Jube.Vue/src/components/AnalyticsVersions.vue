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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { analyticsApi } from '../askJoobyAnalytics/api'
import {
  bestVersionIds,
  liveStanding,
  versionMetrics,
  type MetricKey,
} from '../askJoobyAnalytics/model'
import type { ModelVersion } from '../askJoobyAnalytics/types'
import AnalyticsComparison from './AnalyticsComparison.vue'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number; viewedId: number | null }>()

const emit = defineEmits<{ show: [versionId: number] }>()

const { t, locale } = useI18n()
const { fixed } = useFormat()

const direction = computed(() => directionFor(locale.value))
const comparing = ref<number | null>(null)

const versions = useQuery({
  queryKey: computed(() => queryKeys.analyticsVersions(props.modelId)),
  queryFn: () => analyticsApi.versions(props.modelId),
  retry: false,
})

const list = computed(() => versions.data.value ?? [])
const best = computed(() => bestVersionIds(list.value))
const standing = computed(() => liveStanding(list.value))

interface MatrixRow {
  id: string
  label: string
  kind: 'metric' | 'rows' | 'warnings' | 'actions'
  metric?: MetricKey
}

const ordered = computed(() => [...list.value].sort((a, b) => b.versionNumber - a.versionNumber))

function metricTitle(key: MetricKey): string {
  switch (key) {
    case 'testRocAuc':
      return t('analytics.metric.rocAuc')
    case 'testAveragePrecision':
      return t('analytics.metric.averagePrecision')
    case 'testLogLoss':
      return t('analytics.metric.logLoss')
    case 'testBrierScore':
      return t('analytics.metric.brier')
    default:
      return t('analytics.metric.ece')
  }
}

const rows = computed<MatrixRow[]>(() => [
  ...versionMetrics.map((metric): MatrixRow => ({
    id: metric.key,
    label: metricTitle(metric.key),
    kind: 'metric',
    metric: metric.key,
  })),
  { id: 'testRows', label: t('analytics.versions.testRows'), kind: 'rows' },
  { id: 'warnings', label: t('analytics.versions.warnings'), kind: 'warnings' },
  { id: 'actions', label: t('analytics.versions.actions'), kind: 'actions' },
])

function buttonFor(
  version: ModelVersion,
  id: string,
  label: string,
  disabled: boolean,
  primary: boolean,
  run: () => void,
): HTMLButtonElement {
  const button = document.createElement('button')

  button.type = 'button'
  button.className = primary ? 'btn btn-xs btn-primary' : 'btn btn-xs'
  button.textContent = label
  button.disabled = disabled
  button.dataset.testid = `analytics-${id}-${version.id}`
  button.addEventListener('click', run)

  return button
}

function cellFor(row: object, version: ModelVersion): Element {
  const entry = row as MatrixRow
  const cell = document.createElement('span')

  cell.className = 'inline-flex flex-wrap items-baseline gap-2 font-mono tabular-nums'

  if (entry.kind === 'metric' && entry.metric) {
    const value = version[entry.metric]
    const number = document.createElement('span')
    const isBest = best.value[entry.metric].includes(version.id)

    number.textContent = typeof value === 'number' ? fixed(value, 4) : '–'
    number.className = isBest ? 'font-semibold' : ''
    cell.append(number)

    if (isBest) {
      const tag = document.createElement('span')

      tag.className = 'badge badge-sm badge-soft font-sans'
      tag.textContent = t('analytics.versions.best')
      cell.append(tag)
    }
  } else if (entry.kind === 'rows') {
    cell.textContent = version.testRows === null ? '–' : String(version.testRows)
  } else if (entry.kind === 'warnings') {
    cell.className = 'font-mono text-xs'
    cell.textContent = version.warningCodes.length > 0 ? version.warningCodes.join(', ') : '–'
  } else {
    cell.className = 'inline-flex flex-wrap gap-1'
    cell.append(
      buttonFor(
        version,
        'show',
        props.viewedId === version.id
          ? t('analytics.versions.viewing')
          : t('analytics.versions.show'),
        props.viewedId === version.id,
        true,
        () => emit('show', version.id),
      ),
    )

    if (standing.value.liveId !== null && !version.isActive) {
      cell.append(
        buttonFor(
          version,
          'compare',
          comparing.value === version.id
            ? t('analytics.versions.hideCompare')
            : t('analytics.versions.compare'),
          false,
          false,
          () => {
            comparing.value = comparing.value === version.id ? null : version.id
          },
        ),
      )
    }
  }

  return cell
}

const columns = computed<GridColumn[]>(() => [
  { field: 'label', title: t('analytics.versions.metric'), width: 170, pinned: true },
  ...ordered.value.map((version): GridColumn => ({
    field: `v${version.id}`,
    title: version.isActive
      ? t('analytics.versions.liveTitle', { version: version.versionNumber })
      : `v${version.versionNumber}`,
    width: 190,
    draw: (row) => cellFor(row, version),
  })),
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-versions">
    <p class="text-sm" data-testid="analytics-standing-line">
      <template v-if="standing.liveId === null">{{ t('analytics.versions.noLive') }}</template>
      <template v-else-if="standing.allBest">{{ t('analytics.versions.bestOnAll') }}</template>
      <template v-else>{{
        t('analytics.versions.bestOn', { best: standing.best, of: standing.of })
      }}</template>
    </p>
    <p class="text-xs text-secondary">{{ t('analytics.versions.hint') }}</p>

    <ProblemAlert
      v-if="versions.isError.value"
      key="failed"
      :reasons="[t('analytics.versions.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('analytics.versions.empty')"
      :label="t('analytics.versions.title')"
      row-key="id"
      auto-height
    />

    <AnalyticsComparison
      v-if="comparing !== null && standing.liveId !== null"
      :key="`${standing.liveId}:${comparing}`"
      :live-id="standing.liveId"
      :other-id="comparing"
    />
  </div>
</template>
