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
import { computed, onBeforeUnmount, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { histogramElement } from '../statistics/histogramElement'
import {
  peakOf,
  statisticsRows,
  type PayloadStatistics,
  type StatisticsLabels,
  type StatisticsRow,
} from '../statistics/model'
import DataGrid from './DataGrid.vue'
import StatisticsChartCard from './StatisticsChartCard.vue'
import type { GridColumn } from './dataGridTypes'

const props = defineProps<{
  statistics: PayloadStatistics | null | undefined
  labels?: StatisticsLabels | null
}>()

const { t, locale } = useI18n()
const { fixed } = useFormat()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() => statisticsRows(props.statistics, props.labels))

const card = useTemplateRef<HTMLDivElement>('card')
const active = ref<StatisticsRow>()

let anchors = 0
let openTimer: ReturnType<typeof setTimeout> | undefined
let closeTimer: ReturnType<typeof setTimeout> | undefined

function cancelTimers(): void {
  clearTimeout(openTimer)
  clearTimeout(closeTimer)
}

function open(row: StatisticsRow, anchor: string, delay: number): void {
  cancelTimers()
  openTimer = setTimeout(() => {
    active.value = row
    card.value?.style.setProperty('position-anchor', anchor)

    if (!card.value?.matches(':popover-open')) card.value?.showPopover()
  }, delay)
}

function close(delay: number): void {
  clearTimeout(openTimer)
  clearTimeout(closeTimer)
  closeTimer = setTimeout(() => {
    if (card.value?.matches(':popover-open')) card.value.hidePopover()
  }, delay)
}

function keep(): void {
  clearTimeout(closeTimer)
}

onBeforeUnmount(cancelTimers)

function noData(): HTMLElement {
  const element = document.createElement('span')

  element.textContent = t('statistics.noValues')

  return element
}

function draw(row: object): Element {
  const value = row as StatisticsRow

  if (!value.hasData) return noData()

  const peak = peakOf(value.histogram)!

  const anchor = `--stat-${anchors++}`
  const chart = histogramElement(value.histogram, {
    label: t('statistics.histogramLabel', {
      bins: value.histogram.length,
      min: fixed(value.min),
      max: fixed(value.max),
      peak: fixed(peak.bin),
      count: peak.frequency,
    }),
    bar: (bin, frequency) => t('statistics.bar', { bin: fixed(bin), count: frequency }),
  })

  chart.style.setProperty('anchor-name', anchor)
  chart.setAttribute('tabindex', '0')
  chart.setAttribute('aria-haspopup', 'dialog')
  chart.classList.add('cursor-zoom-in', 'focus-visible:outline-2', 'outline-primary')
  chart.addEventListener('pointerenter', () => open(value, anchor, 200))
  chart.addEventListener('pointerleave', () => close(150))
  chart.addEventListener('focus', () => open(value, anchor, 0))
  chart.addEventListener('blur', () => close(150))
  chart.addEventListener('click', () => open(value, anchor, 0))
  chart.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') close(0)
  })

  return chart
}

function figure(
  field: 'min' | 'max' | 'mean' | 'median' | 'standardDeviation',
  title: string,
): GridColumn {
  return {
    field,
    title,
    align: 'end',
    mono: true,
    width: 110,
    format: (value) => fixed(value as number | null),
  }
}

const columns = computed<GridColumn[]>(() => [
  { field: 'label', title: t('statistics.col.column'), minWidth: 180 },
  { field: 'histogram', title: t('statistics.col.distribution'), width: 180, draw },
  figure('min', t('statistics.col.min')),
  figure('max', t('statistics.col.max')),
  figure('mean', t('statistics.col.mean')),
  figure('median', t('statistics.col.median')),
  figure('standardDeviation', t('statistics.col.standardDeviation')),
])
</script>

<template>
  <section class="flex flex-col gap-2" data-testid="grid-statistics">
    <h3 class="text-sm font-semibold">{{ t('statistics.title') }}</h3>
    <p v-if="rows.length === 0" class="text-sm" data-testid="statistics-empty">
      {{ t('statistics.empty') }}
    </p>
    <DataGrid
      v-else
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('statistics.empty')"
      :label="t('statistics.title')"
    />
    <div
      ref="card"
      popover="manual"
      role="dialog"
      :aria-label="t('statistics.chartOpen')"
      class="dropdown dropdown-center card card-border z-10 mt-2 w-[min(36rem,92vw)] border-base-300 bg-base-100 shadow-lg"
      data-testid="statistics-popover"
      @pointerenter="keep"
      @pointerleave="close(150)"
      @keydown.esc="close(0)"
    >
      <StatisticsChartCard v-if="active" :key="active.column" :row="active" />
    </div>
  </section>
</template>
