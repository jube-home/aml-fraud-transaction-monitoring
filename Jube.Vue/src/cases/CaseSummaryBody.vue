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
import ServerChart from '../components/ServerChart.vue'
import RiskTimeline from './RiskTimeline.vue'
import StayBar from './StayBar.vue'
import { useUiStore } from '../stores/ui'
import { useFormat } from '../composables/useFormat'
import { eventKeys, laneKeys, laneTone, toneClass } from './labels'
import { riskText, type CellContext } from './cells'
import type { CaseEventSummary, CaseSummary } from './types'

const props = withDefaults(
  defineProps<{ caseId: number; data: CaseSummary; part?: 'all' | 'brief' | 'story' | 'record' }>(),
  { part: 'all' },
)

const { t } = useI18n()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')
const { number, percent, dateTime, fixed } = useFormat()

const context = computed<CellContext>(() => ({ t, number, percent, nowMs: Date.now() }))

function eventText(event: CaseEventSummary): string {
  return event.eventType ?? t(eventKeys[event.eventTypeId] ?? 'cases.event.fullFetch')
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="case-summary-body">
    <section v-if="part === 'all' || part === 'brief'" key="lane" class="flex flex-col gap-1">
      <div class="flex flex-wrap items-center gap-2">
        <span :class="toneClass[laneTone(data.case.risk.lane)]">
          {{ t(laneKeys[data.case.risk.lane] ?? 'cases.lane.standard') }}
        </span>
        <span class="font-mono text-xs tabular-nums">{{ riskText(data.case, context) }}</span>
      </div>
      <p v-if="data.case.risk.laneReason" key="reason" class="text-sm">
        {{ data.case.risk.laneReason }}
      </p>
      <p class="font-mono text-xs text-base-content/70">
        {{ [data.case.caseKey, data.case.caseKeyValue].filter((part) => part).join(' · ') }}
      </p>
    </section>

    <section
      v-if="part !== 'record'"
      key="since"
      class="flex flex-col gap-1"
      data-testid="cases-peek-since"
    >
      <h3 class="text-sm font-semibold">{{ t('cases.peek.since') }}</h3>
      <p v-if="data.since.firstLook" key="first" class="text-sm">
        {{ t('cases.peek.firstLook') }}
      </p>
      <template v-else>
        <p class="text-xs text-base-content/70">
          {{
            t('cases.peek.lastViewed', {
              date: data.since.lastViewedDate ? dateTime(data.since.lastViewedDate) : '',
            })
          }}
        </p>
        <p v-if="data.since.priorityThen !== null" key="priority" class="font-mono text-xs">
          {{
            t('cases.peek.priorityMoved', {
              then: number(data.since.priorityThen),
              now: number(data.since.priorityNow),
            })
          }}
        </p>
        <p v-if="data.since.changes.length === 0" key="none" class="text-sm">
          {{ t('cases.peek.nothingNew') }}
        </p>
        <ul v-else key="changes" class="list">
          <li v-for="(change, index) in data.since.changes" :key="index" class="list-row px-0 py-1">
            <div class="list-col-grow">
              <div class="text-sm">{{ eventText(change) }}</div>
              <div class="font-mono text-xs text-base-content/70">
                {{
                  [change.user, change.date ? dateTime(change.date) : '']
                    .filter((x) => x)
                    .join(' · ')
                }}
              </div>
            </div>
          </li>
        </ul>
      </template>
    </section>

    <section
      v-if="part !== 'record'"
      key="contributions"
      class="flex flex-col gap-1"
      data-testid="cases-peek-contributions"
    >
      <h3 class="text-sm font-semibold">{{ t('cases.peek.contributions') }}</h3>
      <p v-if="data.contributions.length === 0" key="none" class="text-sm text-base-content/70">
        {{ t('cases.peek.noEvidence') }}
      </p>
      <ServerChart
        v-else
        key="chart"
        :url="`/api/CaseSummary/${props.caseId}/Waterfall?dark=${dark}`"
        :label="t('cases.peek.waterfall')"
        :version="`${data.riskHistory[0]?.sequence ?? 0}`"
        :stale-seconds="60"
      />
      <ul v-if="data.contributions.length > 0" key="list" class="list">
        <li v-for="(item, index) in data.contributions" :key="index" class="list-row px-0 py-1">
          <div class="list-col-grow">
            <div class="text-sm">{{ item.label ?? item.family }}</div>
            <div class="font-mono text-xs text-base-content/70">
              {{
                t('cases.peek.precision', {
                  confirmed: number(item.confirmed),
                  closed: number(item.closed),
                })
              }}
            </div>
          </div>
          <span class="badge badge-soft badge-sm font-mono">{{ fixed(item.logOdds) }}</span>
        </li>
      </ul>
      <p v-if="data.baseRate !== null" key="base" class="font-mono text-xs text-base-content/70">
        {{ t('cases.peek.baseRate', { rate: percent(data.baseRate) }) }}
      </p>
    </section>

    <section
      v-if="(part === 'all' || part === 'record') && data.stays.length > 0"
      key="stays"
      class="flex flex-col gap-1"
      data-testid="cases-peek-stays"
    >
      <h3 class="text-sm font-semibold">{{ t('cases.peek.stays') }}</h3>
      <StayBar :stays="data.stays" />
    </section>

    <section
      v-if="(part === 'all' || part === 'record') && data.riskHistory.length > 1"
      key="history"
      class="flex flex-col gap-1"
    >
      <h3 class="text-sm font-semibold">{{ t('cases.peek.history') }}</h3>
      <RiskTimeline :items="data.riskHistory" />
    </section>
  </div>
</template>
