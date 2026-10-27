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
import type { StatisticsRow } from '../statistics/model'
import { useUiStore } from '../stores/ui'
import { cssColourHex } from '../composables/themeColour'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ row: StatisticsRow }>()

const ui = useUiStore()

const request = computed(() => ({
  title: props.row.label,
  unit: null,
  dark: ui.resolvedTheme === 'jube-dark',
  color: ui.resolvedTheme ? cssColourHex('--color-primary') : null,
  statistics: {
    min: props.row.min ?? 0,
    max: props.row.max ?? 0,
    mean: props.row.mean ?? 0,
    median: props.row.median ?? 0,
    standardDeviation: props.row.standardDeviation ?? 0,
    histogram: props.row.histogram,
  },
}))
</script>

<template>
  <div class="flex flex-col gap-3 p-3" data-testid="statistics-chart-card">
    <ServerChart
      url="/api/Chart/PayloadStatistics/Histogram"
      :body="request"
      :label="row.label"
      :stale-seconds="3600"
    />
  </div>
</template>
