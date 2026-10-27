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
import { useFormat } from '../../composables/useFormat'
import CaseSummaryBody from '../CaseSummaryBody.vue'
import { impactKeys, laneKeys, likelihoodKeys } from '../labels'
import type { CaseSummary } from '../types'

const props = defineProps<{ caseId: number; summary: CaseSummary }>()

const { t } = useI18n()
const { number, percent } = useFormat()

const row = computed(() => props.summary.case)
const risk = computed(() => row.value.risk)

const why = computed(() => {
  const reasons = row.value.attention.reasons
    .map((reason) => reason.text ?? '')
    .filter((text) => text !== '')

  if (reasons.length > 0) return reasons.join('; ')

  return risk.value.laneReason ?? ''
})

const ranked = computed(() => {
  if (!risk.value.assessed) return t('cases.risk.notAssessed')

  return t('caseDetail.lede.rankedValue', {
    lane: t(laneKeys[risk.value.lane] ?? 'cases.lane.standard'),
    band: t(likelihoodKeys[risk.value.likelihoodBand] ?? 'cases.likelihood.possible'),
    likelihood: percent(risk.value.likelihood),
    impact: t(impactKeys[risk.value.impact] ?? 'cases.impact.moderate'),
    priority: number(risk.value.priority),
  })
})

const similar = computed(() => {
  const history = props.summary.keyHistory

  if (history.closed === 0) return t('caseDetail.lede.similarNone')

  return t('caseDetail.lede.similarValue', {
    confirmed: number(history.confirmed),
    closed: number(history.closed),
  })
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="case-lede">
    <dl class="grid gap-x-4 gap-y-2 text-sm sm:grid-cols-[7rem_minmax(0,1fr)]">
      <dt class="text-base-content/70">{{ t('caseDetail.lede.why') }}</dt>
      <dd data-testid="case-lede-why">{{ why !== '' ? why : t('caseDetail.lede.whyNone') }}</dd>
      <template v-if="row.value !== null">
        <dt class="text-base-content/70">{{ t('caseDetail.lede.stake') }}</dt>
        <dd class="font-mono tabular-nums" data-testid="case-lede-stake">
          {{ number(row.value) }}
        </dd>
      </template>
      <dt class="text-base-content/70">{{ t('caseDetail.lede.similar') }}</dt>
      <dd data-testid="case-lede-similar">{{ similar }}</dd>
      <dt class="text-base-content/70">{{ t('caseDetail.lede.ranked') }}</dt>
      <dd data-testid="case-lede-ranked">{{ ranked }}</dd>
    </dl>

    <CaseSummaryBody :case-id="caseId" :data="summary" part="story" />
  </div>
</template>
