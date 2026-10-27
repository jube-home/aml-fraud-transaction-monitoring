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
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { ApiError, useApi } from '../api/useApi'
import JubeChart from './JubeChart.vue'

const props = withDefaults(
  defineProps<{
    url: string
    body?: object
    label: string
    staleSeconds?: number
    version?: string
    emptyOnRefusal?: boolean
  }>(),
  { staleSeconds: 30, emptyOnRefusal: false },
)

const emit = defineEmits<{ empty: [empty: boolean] }>()

const { t } = useI18n()

const chart = useQuery({
  queryKey: computed(
    () => ['serverChart', props.url, props.body ?? null, props.version ?? ''] as const,
  ),
  queryFn: () =>
    useApi<string>(props.url, props.body ? { method: 'POST', body: props.body } : undefined),
  staleTime: computed(() => props.staleSeconds * 1000),
  retry: false,
})

const svg = computed(() => {
  const text = chart.data.value

  return typeof text === 'string' && text.trimStart().startsWith('<svg') ? text : null
})

const nothingToChart = computed(() => {
  const error = chart.error.value

  if (!props.emptyOnRefusal || !(error instanceof ApiError) || error.kind !== 'validation')
    return null

  return typeof error.body === 'string' && error.body !== '' ? error.body : t('chart.empty')
})
watch(nothingToChart, (value) => emit('empty', value !== null), { immediate: true })
</script>

<template>
  <div class="min-h-40 min-w-0" data-testid="server-chart">
    <div
      v-if="chart.isPending.value"
      key="loading"
      class="skeleton h-40 w-full"
      role="status"
      :aria-label="t('chart.loading')"
    ></div>
    <p
      v-else-if="nothingToChart !== null"
      key="empty"
      class="text-sm text-base-content/70"
      role="status"
      data-testid="server-chart-empty"
    >
      {{ nothingToChart }}
    </p>
    <div
      v-else-if="chart.isError.value || !svg"
      key="failed"
      class="flex flex-wrap items-center gap-3 text-sm"
      role="alert"
    >
      <span class="text-error">{{ t('chart.failed') }}</span>
      <button type="button" class="btn btn-xs" @click="chart.refetch()">
        {{ t('chart.retry') }}
      </button>
    </div>
    <JubeChart v-else key="chart" :svg="svg" :label="label" />
  </div>
</template>
