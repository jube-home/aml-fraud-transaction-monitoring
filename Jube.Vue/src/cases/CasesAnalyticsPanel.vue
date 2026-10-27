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
import { useRoute, useRouter } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import { useCanvasSections } from '../canvas/useCanvasSections'
import CanvasIndex from '../components/CanvasIndex.vue'
import CanvasSection from '../components/CanvasSection.vue'
import ServerChart from '../components/ServerChart.vue'
import CasesAnalyticsStats from './CasesAnalyticsStats.vue'
import CasesQaCard from './CasesQaCard.vue'
import { casesApi } from './api'
import {
  canvasLayouts,
  chartPath,
  periodFrom,
  periodOf,
  periods,
  type CanvasName,
} from './analyticsLayout'

const props = defineProps<{ canvas: CanvasName; workflow: string }>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const layout = computed(() => canvasLayouts[props.canvas])
const days = computed(() => periodOf(route.query.days))
const now = ref(new Date())

const summary = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'analytics', props.workflow, 'period', days.value] as const,
  ),
  queryFn: () => casesApi.analyticsSince(props.workflow, periodFrom(days.value, now.value)),
  enabled: computed(() => props.workflow !== ''),
  staleTime: 60_000,
  retry: false,
})

const { opened, pick } = useCanvasSections<string>(
  [
    'ageing',
    'flow',
    'bottlenecks',
    'service',
    'rhythm',
    'rules',
    'ranking',
    'sample',
    'calibration',
  ],
  {
    ageing: true,
    flow: true,
    bottlenecks: true,
    service: true,
    rhythm: false,
    rules: true,
    ranking: true,
    sample: true,
    calibration: true,
  },
  ref(true),
)

const items = computed(() =>
  layout.value.sections.map((section) => ({ id: section.id, label: t(section.titleKey) })),
)

function change(next: { days?: number }): void {
  void router.replace({
    query: { ...route.query, days: String(next.days ?? days.value) },
  })
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="cases-analytics-page">
    <h2 class="sr-only">{{ t(layout.titleKey) }}</h2>

    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('cases.analytics.period') }}</span>
        <select
          class="select select-sm"
          :value="days"
          data-testid="cases-analytics-period"
          @change="change({ days: Number(($event.target as HTMLSelectElement).value) })"
        >
          <option v-for="period in periods" :key="period" :value="period">
            {{ t('cases.analytics.days', { n: period }) }}
          </option>
        </select>
      </label>
    </div>

    <CasesAnalyticsStats
      v-if="summary.data.value"
      key="stats"
      :canvas="canvas"
      :headline="summary.data.value.headline"
    />

    <div
      v-if="workflow !== ''"
      key="canvas"
      class="grid h-[max(24rem,calc(100dvh-29rem))] min-w-0 gap-4 lg:grid-cols-[13rem_minmax(0,1fr)]"
    >
      <div class="lg:self-start">
        <CanvasIndex :items="items" :label="t(layout.indexKey)" @pick="pick" />
      </div>

      <div
        class="flex min-h-0 min-w-0 flex-col gap-3 overflow-y-auto pe-1"
        tabindex="0"
        role="region"
        :aria-label="t(layout.titleKey)"
        data-testid="cases-analytics-scroll"
      >
        <CanvasSection
          v-for="section in layout.sections"
          :id="section.id"
          :key="`${canvas}-${section.id}`"
          v-model:open="opened[section.id]"
          :title="t(section.titleKey)"
          :hint="t(section.hintKey)"
        >
          <CasesQaCard
            v-if="section.card === 'qaSample'"
            key="qa"
            :workflow="workflow"
            :from="periodFrom(days, now)"
          />
          <ServerChart
            v-for="chart in section.charts"
            :key="`${workflow}-${days}-${chart.kind}`"
            :url="chartPath(chart.kind, workflow, days, now)"
            :label="t(chart.labelKey)"
            :stale-seconds="60"
            empty-on-refusal
          />
        </CanvasSection>
      </div>
    </div>
  </section>
</template>
