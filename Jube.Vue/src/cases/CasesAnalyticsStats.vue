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
import { useI18n } from 'vue-i18n'
import StatCard from '../components/StatCard.vue'
import { useFormat } from '../composables/useFormat'
import type { CanvasName } from './analyticsLayout'
import { durationText } from './duration'
import type { CaseAnalyticsHeadline } from './types'

defineProps<{ canvas: CanvasName; headline: CaseAnalyticsHeadline }>()

const { t } = useI18n()
const { number, percent } = useFormat()

function minutes(value: number | null): string {
  return value === null ? t('cases.headline.none') : durationText(t, value, number)
}

function share(value: number | null): string {
  return value === null ? t('cases.headline.none') : percent(value)
}
</script>

<template>
  <section
    v-if="canvas === 'queue'"
    key="queue"
    class="grid w-full gap-3 sm:grid-cols-2 xl:grid-cols-4"
    :aria-label="t('cases.headline.label')"
    data-testid="cases-analytics-stats"
  >
    <StatCard :title="t('cases.pulse.open')">
      {{ number(headline.openNow) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.pulse.urgent', { n: number(headline.urgentOpen) }) }}
        </div>
      </template>
    </StatCard>
    <StatCard :title="t('cases.headline.overdue')">
      {{ number(headline.slaBreachedOpen) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.headline.atRisk', { n: number(headline.slaAtRiskOpen) }) }}
        </div>
      </template>
    </StatCard>
    <StatCard
      :title="t('cases.analytics.stat.firstLook')"
      :small="headline.medianFirstViewMinutes === null"
    >
      {{ minutes(headline.medianFirstViewMinutes) }}
    </StatCard>
    <StatCard
      :title="t('cases.analytics.stat.resolution')"
      :small="headline.medianResolutionMinutes === null"
    >
      {{ minutes(headline.medianResolutionMinutes) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.analytics.stat.closed', { n: number(headline.closed) }) }}
        </div>
      </template>
    </StatCard>
  </section>

  <section
    v-else
    key="quality"
    class="grid w-full gap-3 sm:grid-cols-2 xl:grid-cols-4"
    :aria-label="t('cases.headline.label')"
    data-testid="cases-analytics-stats"
  >
    <StatCard :title="t('cases.analytics.stat.confirmed')" :small="headline.confirmedRate === null">
      {{ share(headline.confirmedRate) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.analytics.stat.decided', { n: number(headline.decided) }) }}
        </div>
      </template>
    </StatCard>
    <StatCard :title="t('cases.analytics.stat.reopen')">
      {{ percent(headline.reopenRate) }}
    </StatCard>
    <StatCard :title="t('cases.analytics.stat.unassessed')">
      {{ number(headline.unassessedOpen) }}
    </StatCard>
    <StatCard :title="t('cases.analytics.stat.closedTitle')">
      {{ number(headline.closed) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.analytics.stat.opened', { n: number(headline.opened) }) }}
        </div>
      </template>
    </StatCard>
  </section>
</template>
