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
import { useQuery } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { analyticsApi } from '../askJoobyAnalytics/api'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ versionId: number }>()

const { t } = useI18n()

const promotion = useQuery({
  queryKey: computed(() => queryKeys.analyticsPromotion(props.versionId)),
  queryFn: () => analyticsApi.promotion(props.versionId),
  retry: false,
})

const narrative = useQuery({
  queryKey: computed(() => queryKeys.analyticsNarrative(props.versionId)),
  queryFn: () => analyticsApi.narrative(props.versionId),
  retry: false,
})

const names = useQuery({
  queryKey: queryKeys.analyticsModelNames(),
  queryFn: analyticsApi.modelNames,
  retry: false,
})

const data = computed(() => promotion.data.value)
const paragraphs = computed(() => narrative.data.value?.holistic ?? [])
const noEvidence = computed(
  () => promotion.error.value instanceof ApiError && promotion.error.value.kind === 'conflict',
)

const allocation = computed(() => {
  if (!data.value?.instanceAllocated) return t('analytics.story.notAllocated')

  const id = data.value.allocatedEntityAnalysisModelId
  const name = names.data.value?.find((model) => model.id === id)?.name ?? String(id)

  return t('analytics.story.allocated', { model: name })
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-story">
    <div
      v-if="promotion.isPending.value"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('analytics.story.loading')"
    ></div>
    <p v-else-if="noEvidence" key="no-evidence" class="text-sm" data-testid="analytics-no-evidence">
      {{ t('analytics.story.noEvidence') }}
    </p>
    <ProblemAlert
      v-else-if="promotion.isError.value"
      key="failed"
      :reasons="[t('analytics.story.failed')]"
    />
    <template v-else-if="data">
      <p class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm" data-testid="analytics-facts">
        <span class="font-semibold">{{
          t('analytics.story.version', { name: data.modelName ?? '', version: data.versionNumber })
        }}</span>
        <span v-if="data.isActiveVersion" key="live" class="badge badge-sm badge-success">{{
          t('analytics.versions.live')
        }}</span>
        <span class="text-secondary">{{ allocation }}</span>
        <span class="font-mono text-xs tabular-nums text-secondary">{{
          t('analytics.story.seed', { seed: data.seed })
        }}</span>
      </p>

      <div
        v-for="warning in data.warnings"
        :key="warning.code"
        role="alert"
        class="alert alert-warning items-start"
        data-testid="analytics-warning"
      >
        <div class="flex flex-col gap-0.5">
          <p class="font-mono text-xs">{{ warning.code }}</p>
          <p class="text-sm">{{ warning.message }}</p>
        </div>
      </div>

      <div
        v-if="paragraphs.length > 0"
        key="narrative"
        class="flex max-w-prose flex-col gap-2 text-sm"
        data-testid="analytics-narrative"
      >
        <p v-for="paragraph in paragraphs" :key="paragraph">{{ paragraph }}</p>
      </div>
      <p v-else key="no-narrative" class="text-sm text-secondary">
        {{ t('analytics.story.noNarrative') }}
      </p>
    </template>
  </div>
</template>
