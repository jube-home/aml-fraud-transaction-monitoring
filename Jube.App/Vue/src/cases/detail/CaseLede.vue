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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {useFormat} from '../../composables/useFormat'
import CaseSummaryBody from '../CaseSummaryBody.vue'
import {impactKeys, likelihoodKeys, likelihoodTone} from '../labels'
import type {CaseSummary} from '../types'
import {analystLikelihoodKeys, assessmentApi, assessmentKey} from './assessmentApi'
import {headlineKeys, headlineOf, similarityApi, similarityKey} from './similarityApi'

const props = defineProps<{ caseId: number; summary: CaseSummary }>()

const {t} = useI18n()
const {number, percent} = useFormat()

const row = computed(() => props.summary.case)
const risk = computed(() => row.value.risk)

const why = computed(() => {
  const reasons = row.value.attention.reasons
    .map((reason) => reason.text ?? '')
    .filter((text) => text !== '')

  if (reasons.length > 0) return reasons.join('; ')

  return risk.value.laneReason ?? ''
})

const likelihoodBand = computed(() =>
  t(likelihoodKeys[risk.value.likelihoodBand] ?? 'cases.likelihood.possible'),
)

const likelihoodAlarming = computed(() => likelihoodTone(risk.value.likelihoodBand) === 'error')

const impact = computed(() => t(impactKeys[risk.value.impact] ?? 'cases.impact.moderate'))

const history = computed(() => props.summary.keyHistory)

const keyHistory = computed(() => {
  if (history.value.closed === 0) return t('caseDetail.lede.similarNone')

  return t('caseDetail.lede.similarValue', {
    confirmed: number(history.value.confirmed),
    closed: number(history.value.closed),
  })
})

const similarity = useQuery({
  queryKey: computed(() => similarityKey(props.caseId)),
  queryFn: () => similarityApi.get(props.caseId),
  staleTime: 60_000,
  retry: false,
})

const assessment = useQuery({
  queryKey: computed(() => assessmentKey(props.caseId)),
  queryFn: () => assessmentApi.state(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const analystView = computed(() => {
  const current = assessment.data.value?.current

  return current
    ? t('caseDetail.assessment.lede', {
      view: t(analystLikelihoodKeys[current.likelihoodId] ?? 'caseDetail.assessment.medium'),
    })
    : ''
})

const similarHeadline = computed(() => headlineOf(similarity.data.value, similarity.isError.value))

const similarValue = computed(() => {
  const data = similarity.data.value

  if (!data || !data.built) return '-'
  if (data.enoughToJudge) return `${number(data.confirmed)} / ${number(data.closed)}`

  return number(data.similar)
})

const similarDescription = computed(() => {
  const data = similarity.data.value

  if (similarHeadline.value === 'loading') return ''
  if (similarHeadline.value === 'headline')
    return t('caseDetail.similar.headline', {
      confirmed: number(data?.confirmed ?? 0),
      closed: number(data?.closed ?? 0),
    })

  return t(headlineKeys[similarHeadline.value])
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-5" data-testid="case-lede">
    <section class="flex min-w-0 flex-col gap-1" aria-labelledby="case-lede-why-title">
      <h3 id="case-lede-why-title" class="text-xs font-semibold text-base-content/70">
        {{ t('caseDetail.lede.why') }}
      </h3>
      <p class="text-base leading-relaxed break-words" data-testid="case-lede-why">
        {{ why !== '' ? why : t('caseDetail.lede.whyNone') }}
      </p>
    </section>

    <dl
      class="grid w-full min-w-0 gap-px overflow-hidden rounded-box border border-base-300 bg-base-300 sm:grid-flow-col sm:auto-cols-fr"
      data-testid="case-lede-stats"
    >
      <div class="flex min-w-0 flex-col gap-0.5 bg-base-100 px-4 py-3" data-testid="case-lede-likelihood">
        <dt class="text-xs text-base-content/70">{{ t('caseDetail.lede.likelihood') }}</dt>
        <dd
          v-if="risk.assessed"
          key="likelihood"
          class="font-mono text-2xl font-semibold tabular-nums"
          :class="likelihoodAlarming ? 'text-error' : ''"
        >
          {{ percent(risk.likelihood) }}
        </dd>
        <dd v-else key="unassessed" class="text-lg font-semibold">
          {{ t('cases.risk.notAssessed') }}
        </dd>
        <dd v-if="risk.assessed" class="text-xs text-base-content/70">{{ likelihoodBand }}</dd>
        <dd
          v-if="analystView !== ''"
          key="analyst"
          class="text-xs font-semibold"
          data-testid="case-lede-analyst"
        >
          {{ analystView }}
        </dd>
      </div>

      <div
        v-if="risk.assessed"
        key="impact"
        class="flex min-w-0 flex-col gap-0.5 bg-base-100 px-4 py-3"
        data-testid="case-lede-impact"
      >
        <dt class="text-xs text-base-content/70">{{ t('caseDetail.lede.impact') }}</dt>
        <dd class="text-2xl font-semibold">{{ impact }}</dd>
      </div>

      <div
        v-if="row.value !== null"
        key="stake"
        class="flex min-w-0 flex-col gap-0.5 bg-base-100 px-4 py-3"
        data-testid="case-lede-stake"
      >
        <dt class="text-xs text-base-content/70">{{ t('caseDetail.lede.stake') }}</dt>
        <dd class="font-mono text-2xl font-semibold tabular-nums">{{ number(row.value) }}</dd>
      </div>

      <div class="flex min-w-0 flex-col gap-0.5 bg-base-100 px-4 py-3" data-testid="case-lede-similar">
        <dt class="text-xs text-base-content/70">{{ t('caseDetail.lede.similar') }}</dt>
        <dd
          class="font-mono text-2xl font-semibold tabular-nums"
          data-testid="case-lede-similar-value"
        >
          {{ similarValue }}
        </dd>
        <dd class="text-xs text-base-content/70">{{ similarDescription }}</dd>
        <dd class="text-xs text-base-content/70" data-testid="case-lede-key-history">
          {{ keyHistory }}
        </dd>
      </div>
    </dl>

    <CaseSummaryBody :case-id="caseId" :data="summary" part="story"/>
  </div>
</template>
