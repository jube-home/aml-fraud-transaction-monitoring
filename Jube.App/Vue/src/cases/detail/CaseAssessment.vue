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
import {UserCheck} from 'lucide-vue-next'
import {useFormat} from '../../composables/useFormat'
import {isolate} from '../isolate'
import {
  analystLikelihoodKeys,
  assessmentApi,
  assessmentKey,
  earlierOf,
  type AnalystAssessment,
} from './assessmentApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const {dateTime, percent} = useFormat()

const state = useQuery({
  queryKey: computed(() => assessmentKey(props.caseId)),
  queryFn: () => assessmentApi.state(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const current = computed(() => state.data.value?.current ?? null)
const earlier = computed(() => earlierOf(state.data.value))

function view(item: AnalystAssessment): string {
  return t(analystLikelihoodKeys[item.likelihoodId] ?? 'caseDetail.assessment.medium')
}

function byline(item: AnalystAssessment): string {
  return t('caseDetail.assessment.by', {
    user: isolate(item.createdUser),
    when: dateTime(item.createdDate),
  })
}
</script>

<template>
  <section
    v-if="state.data.value"
    class="flex min-w-0 flex-col overflow-hidden rounded-box border border-base-300 bg-base-100"
    data-testid="case-assessment"
  >
    <h4
      class="flex items-center gap-2 border-b border-base-300 bg-base-200/50 px-4 py-2 text-sm font-semibold"
    >
      <UserCheck class="size-4" aria-hidden="true"/>
      {{ t('caseDetail.assessment.canvasTitle') }}
    </h4>

    <div class="flex min-w-0 flex-col gap-4 p-4">
      <div
        v-if="current"
        key="current"
        class="flex min-w-0 flex-col gap-1"
        data-testid="case-assessment-current"
      >
        <div class="flex flex-wrap items-center gap-2">
          <span class="badge badge-soft font-semibold" data-testid="case-assessment-view">
            {{ view(current) }}
          </span>
          <span class="text-xs text-base-content/70">{{ byline(current) }}</span>
        </div>
        <p class="text-sm break-words">{{ isolate(current.reason ?? '') }}</p>
        <p
          v-if="current.computedLikelihoodProbability !== null"
          class="font-mono text-xs text-base-content/70"
        >
          {{
            t('caseDetail.assessment.computed', {
              rate: percent(current.computedLikelihoodProbability),
            })
          }}
        </p>
      </div>
      <p v-else key="none" class="text-sm text-base-content/70" role="status">
        {{ t('caseDetail.assessment.none') }}
      </p>

      <div v-if="earlier.length > 0" key="earlier" class="flex min-w-0 flex-col gap-1">
        <h5 class="text-xs text-base-content/70">{{ t('caseDetail.assessment.earlier') }}</h5>
        <ul class="flex flex-col gap-1" data-testid="case-assessment-earlier">
          <li
            v-for="item in earlier"
            :key="item.guid"
            class="flex flex-wrap items-baseline gap-2 border-b border-base-200 py-1 text-sm"
          >
            <span class="badge badge-ghost badge-sm">{{ view(item) }}</span>
            <span class="text-xs text-base-content/70">{{ byline(item) }}</span>
            <span v-if="item.withdrawnDate !== null" class="badge badge-ghost badge-sm">
              {{ t('caseDetail.assessment.withdrawnLabel') }}
            </span>
            <span class="w-full break-words">{{ isolate(item.reason ?? '') }}</span>
          </li>
        </ul>
      </div>
    </div>
  </section>
</template>
