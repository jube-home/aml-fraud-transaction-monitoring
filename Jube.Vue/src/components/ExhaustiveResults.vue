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
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { describeApiError } from '../api/problems'
import { queryKeys } from '../api/queryKeys'
import { useCanvasSections } from '../canvas/useCanvasSections'
import { exhaustiveApi } from '../exhaustive/api'
import { generationOf, hasResults, refreshInterval } from '../exhaustive/model'
import CanvasIndex from './CanvasIndex.vue'
import CanvasSection from './CanvasSection.vue'
import ExhaustiveBatchRecall from './ExhaustiveBatchRecall.vue'
import ExhaustiveLearningCurve from './ExhaustiveLearningCurve.vue'
import ExhaustiveNarrative from './ExhaustiveNarrative.vue'
import ExhaustivePerformance from './ExhaustivePerformance.vue'
import ExhaustivePromotedModels from './ExhaustivePromotedModels.vue'
import ExhaustiveSensitivity from './ExhaustiveSensitivity.vue'
import ExhaustiveStatusBand from './ExhaustiveStatusBand.vue'
import ExhaustiveTrials from './ExhaustiveTrials.vue'
import ExhaustiveVariables from './ExhaustiveVariables.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(defineProps<{ instanceId: number; allowed?: boolean }>(), {
  allowed: true,
})

const { t } = useI18n()
const queryClient = useQueryClient()

const sectionIds = ['verdict', 'learned', 'live', 'detects', 'whatif', 'trials'] as const
type SectionId = (typeof sectionIds)[number]

const problems = ref<string[]>([])

const instance = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'instance')),
  queryFn: () => exhaustiveApi.instance(props.instanceId),
  refetchInterval: (query) => refreshInterval(query.state.data?.statusId),
  retry: false,
})

const data = computed(() => instance.data.value)
const version = computed(() => generationOf(data.value))
const ready = computed(() => data.value !== undefined && hasResults(data.value.statusId))

const { opened, pick } = useCanvasSections<SectionId>(
  sectionIds,
  { verdict: true, learned: true, live: true, detects: true, whatif: false, trials: false },
  ready,
)

const items = computed(() => [
  { id: 'verdict', label: t('exhaustive.section.verdict') },
  { id: 'learned', label: t('exhaustive.section.learned') },
  { id: 'live', label: t('exhaustive.section.live') },
  { id: 'detects', label: t('exhaustive.section.detects') },
  { id: 'whatif', label: t('exhaustive.section.whatIf') },
  { id: 'trials', label: t('exhaustive.section.trials') },
])

const stop = useMutation({
  mutationFn: () => exhaustiveApi.stop(data.value!.guid),
  onSuccess: () => {
    problems.value = []
  },
  onError: (error) => {
    problems.value = describeApiError(error, {
      failed: t('exhaustive.stopFailed'),
      refused: t('exhaustive.refused'),
    })
  },
  onSettled: () =>
    queryClient.invalidateQueries({ queryKey: queryKeys.exhaustive(props.instanceId, 'instance') }),
})

function refresh(): void {
  problems.value = []
  void queryClient.invalidateQueries({ queryKey: ['exhaustive', props.instanceId] })
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="exhaustive-results">
    <ProblemAlert
      v-if="instance.isError.value"
      key="failed"
      :reasons="[t('exhaustive.failed')]"
      data-testid="exhaustive-failed"
    />
    <div
      v-else-if="instance.isPending.value"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('exhaustive.loading')"
    ></div>
    <template v-else-if="data">
      <ExhaustiveStatusBand
        :instance="data"
        :busy="instance.isFetching.value"
        :stopping="stop.isPending.value"
        @stop="stop.mutate()"
        @refresh="refresh"
      />

      <ProblemAlert
        v-if="problems.length > 0"
        key="problems"
        :reasons="problems"
        data-testid="exhaustive-problems"
      />

      <p
        v-if="!ready"
        key="waiting"
        class="rounded-box border border-base-300 bg-base-100 p-4 text-sm"
        role="status"
        data-testid="exhaustive-waiting"
      >
        {{ t('exhaustive.waiting') }}
      </p>

      <div v-else key="canvas" class="grid min-w-0 gap-4 lg:grid-cols-[13rem_minmax(0,1fr)]">
        <div class="lg:sticky lg:top-24 lg:self-start">
          <CanvasIndex :items="items" :label="t('exhaustive.index')" @pick="pick" />
        </div>

        <div class="flex min-w-0 flex-col gap-3">
          <CanvasSection
            id="verdict"
            v-model:open="opened.verdict"
            :title="t('exhaustive.section.verdict')"
            :hint="t('exhaustive.hint.verdict')"
          >
            <ExhaustiveNarrative :instance-id="instanceId" :group="null" :version="version" />
            <p
              v-if="data.anomalySummary"
              key="anomaly"
              class="text-sm"
              data-testid="exhaustive-anomaly"
            >
              <span class="font-medium">{{ t('exhaustive.anomaly') }}</span>
              {{ data.anomalySummary }}
            </p>
          </CanvasSection>

          <CanvasSection
            id="learned"
            v-model:open="opened.learned"
            :title="t('exhaustive.section.learned')"
            :hint="t('exhaustive.hint.learned')"
          >
            <ExhaustiveNarrative
              :instance-id="instanceId"
              group="Prescription"
              :version="version"
            />
            <h4 class="text-sm font-semibold">{{ t('exhaustive.variables.title') }}</h4>
            <ExhaustiveVariables :instance-id="instanceId" :version="version" />
            <h4 class="text-sm font-semibold">{{ t('exhaustive.learning.title') }}</h4>
            <ExhaustiveLearningCurve :instance-id="instanceId" :version="version" />
          </CanvasSection>

          <CanvasSection
            id="live"
            v-model:open="opened.live"
            :title="t('exhaustive.section.live')"
            :hint="t('exhaustive.hint.live')"
          >
            <ExhaustivePromotedModels
              :instance-id="instanceId"
              :version="version"
              :allowed="allowed"
            />
          </CanvasSection>

          <CanvasSection
            id="detects"
            v-model:open="opened.detects"
            :title="t('exhaustive.section.detects')"
            :hint="t('exhaustive.hint.detects')"
          >
            <ExhaustivePerformance :instance-id="instanceId" :version="version" />
          </CanvasSection>

          <CanvasSection
            id="whatif"
            v-model:open="opened.whatif"
            :title="t('exhaustive.section.whatIf')"
            :hint="t('exhaustive.hint.whatIf')"
          >
            <ExhaustiveSensitivity :instance-id="instanceId" :guid="data.guid" :version="version" />
            <ExhaustiveBatchRecall :guid="data.guid" />
          </CanvasSection>

          <CanvasSection
            id="trials"
            v-model:open="opened.trials"
            :title="t('exhaustive.section.trials')"
            :hint="t('exhaustive.hint.trials')"
          >
            <ExhaustiveTrials :instance-id="instanceId" :version="version" />
          </CanvasSection>
        </div>
      </div>
    </template>
  </div>
</template>
