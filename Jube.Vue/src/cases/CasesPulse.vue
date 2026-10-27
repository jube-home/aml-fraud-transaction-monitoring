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
import { Flame, Trophy } from 'lucide-vue-next'
import DayBars from '../components/DayBars.vue'
import FieldHelp from '../components/FieldHelp.vue'
import StatCard from '../components/StatCard.vue'
import TrendLine from '../components/TrendLine.vue'
import { useFormat } from '../composables/useFormat'
import { durationText } from './duration'
import { againstTypical, dayState, ringValue } from './pulse'
import type { CaseAnalyticsSummary } from './types'

const props = defineProps<{ summary: CaseAnalyticsSummary }>()

const { t } = useI18n()
const { number } = useFormat()

const headline = computed(() => props.summary.headline)
const mine = computed(() => props.summary.myDay)
const recent = computed(() => props.summary.flow.slice(-14))
const backlog = computed(() => recent.value.map((day) => day.backlog))
const closed = computed(() => recent.value.map((day) => day.closed))
const closedToday = computed(() => recent.value.at(-1)?.closed ?? 0)
const mineSeries = computed(() => mine.value.days.map((day) => day.closed))
const state = computed(() => dayState(mine.value.closedToday, mine.value.goal))
const ring = computed(() => ringValue(mine.value.closedToday, mine.value.goal))
const difference = computed(() => againstTypical(mine.value.closedToday, mine.value.typicalDay))

const firstLook = computed(() =>
  headline.value.medianFirstViewMinutes === null
    ? null
    : durationText(t, headline.value.medianFirstViewMinutes, number),
)

const progress = computed(() => {
  if (mine.value.goal === null)
    return t('cases.pulse.mine.closed', { n: number(mine.value.closedToday) })

  return t('cases.pulse.mine.of', {
    n: number(mine.value.closedToday),
    goal: number(mine.value.goal),
  })
})

const nudge = computed(() => {
  if (state.value === 'met') return t('cases.pulse.mine.met')
  if (state.value === 'toGo' && mine.value.goal !== null)
    return t('cases.pulse.mine.toGo', { n: number(mine.value.goal - mine.value.closedToday) })
  if (state.value === 'started') return t('cases.pulse.mine.started')

  return t('cases.pulse.mine.fresh')
})

const comparison = computed(() => {
  if (difference.value > 0) return t('cases.pulse.mine.above', { n: number(difference.value) })
  if (difference.value < 0) return t('cases.pulse.mine.below', { n: number(-difference.value) })

  return t('cases.pulse.mine.level')
})
</script>

<template>
  <section
    class="grid w-full gap-3 sm:grid-cols-2 xl:grid-cols-[1fr_1fr_1fr_1.6fr]"
    :aria-label="t('cases.headline.label')"
    data-testid="cases-headline"
  >
    <StatCard
      :title="t('cases.pulse.open')"
      :help="t('cases.help.open')"
      value-testid="cases-pulse-open"
    >
      {{ number(headline.openNow) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.pulse.urgent', { n: number(headline.urgentOpen) }) }}
        </div>
        <TrendLine :values="backlog" :label="t('cases.pulse.openTrend')" />
      </template>
    </StatCard>

    <StatCard
      :title="t('cases.pulse.closed')"
      :help="t('cases.help.closed')"
      value-testid="cases-pulse-closed"
    >
      {{ number(closedToday) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.pulse.opened', { n: number(recent.at(-1)?.opened ?? 0) }) }}
        </div>
        <TrendLine :values="closed" :label="t('cases.pulse.closedTrend')" />
      </template>
    </StatCard>

    <StatCard :title="t('cases.headline.overdue')" :help="t('cases.help.overdue')">
      {{ number(headline.slaBreachedOpen) }}
      <template #more>
        <div class="stat-desc">
          {{ t('cases.headline.atRisk', { n: number(headline.slaAtRiskOpen) }) }}
        </div>
        <div v-if="firstLook !== null" key="look" class="stat-desc">
          {{ t('cases.pulse.firstLook', { time: firstLook }) }}
        </div>
      </template>
    </StatCard>

    <div
      class="order-first flex min-w-0 flex-col gap-3 rounded-box border border-base-300 bg-base-100 px-4 py-3 sm:col-span-2 xl:order-none xl:col-span-1"
      data-testid="cases-pulse-mine"
    >
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-1">
          <span class="text-sm text-base-content/70">{{ t('cases.pulse.mine.title') }}</span>
          <FieldHelp :label="t('cases.pulse.mine.title')" :text="t('cases.help.mine')" />
        </div>
        <div class="flex flex-wrap items-center justify-end gap-2">
          <span
            v-if="mine.streak > 0"
            class="badge badge-sm gap-1 whitespace-nowrap"
            data-testid="cases-pulse-streak"
          >
            <Flame class="size-3" aria-hidden="true" />
            {{ t('cases.pulse.mine.streak', { n: number(mine.streak) }) }}
          </span>
          <span v-if="state === 'met'" class="badge badge-sm gap-1 whitespace-nowrap">
            <Trophy class="size-3" aria-hidden="true" />
            {{ t('cases.pulse.mine.goalMet') }}
          </span>
        </div>
      </div>

      <div class="flex items-center gap-4">
        <div
          class="radial-progress shrink-0"
          :class="state === 'met' ? 'text-success' : 'text-primary'"
          :style="`--value:${ring}; --size:5rem; --thickness:0.5rem;`"
          role="progressbar"
          :aria-valuenow="ring"
          aria-valuemin="0"
          aria-valuemax="100"
          :aria-label="t('cases.pulse.mine.ring')"
        >
          <span class="font-mono text-xl tabular-nums text-base-content">
            {{ number(mine.closedToday) }}
          </span>
        </div>

        <div class="flex min-w-0 flex-col gap-0.5">
          <span class="text-lg font-semibold" data-testid="cases-pulse-nudge">{{ nudge }}</span>
          <span class="text-sm text-base-content/70" data-testid="cases-pulse-progress">
            {{ progress }}
          </span>
        </div>
      </div>

      <div class="mt-auto flex flex-col gap-1">
        <DayBars :values="mineSeries" :label="t('cases.pulse.mine.trend')" />
        <span
          v-if="mine.bestDay > 0"
          key="history"
          class="flex flex-wrap justify-between gap-x-3 text-xs text-base-content/70"
        >
          <span>{{ comparison }}</span>
          <span>{{ t('cases.pulse.mine.best', { n: number(mine.bestDay) }) }}</span>
        </span>
      </div>
    </div>
  </section>
</template>
