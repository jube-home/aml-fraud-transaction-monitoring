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
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { describeApiError } from '../api/problems'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { sanctionApi } from '../sanction/api'
import { canSearch, paramsOf } from '../sanction/state'
import { sanctionLimits, type SanctionParams, type SanctionResult } from '../sanction/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(
  defineProps<{
    multiPartString?: string
    distance?: number
    maxDistanceRatio?: number
    maxCoverageRatio?: number
    showForm?: boolean
    autoRun?: boolean
  }>(),
  { showForm: true, autoRun: false },
)

const emit = defineEmits<{
  result: [result: SanctionResult]
  error: [error: Error]
  search: [params: SanctionParams]
}>()

const { t, locale } = useI18n()
const { fixed } = useFormat()
const pagerLabels = usePagerLabels()
const uid = useId()

const direction = computed(() => directionFor(locale.value))

const draft = ref<SanctionParams>(paramsOf(props))
const submitted = ref<SanctionParams>()

function submit(): void {
  if (canSearch(draft.value)) submitted.value = { ...draft.value }
}

function commit(): void {
  if (!canSearch(draft.value)) return

  submit()
  emit('search', { ...draft.value })
}

watch(
  () => [props.multiPartString, props.distance, props.maxDistanceRatio, props.maxCoverageRatio],
  () => {
    draft.value = paramsOf(props)

    if (props.autoRun) submit()
  },
  { immediate: true },
)

const search = useQuery({
  queryKey: computed(() => queryKeys.sanctionSearch(submitted.value ?? null)),
  queryFn: () => sanctionApi.search(submitted.value!),
  enabled: computed(() => submitted.value !== undefined),
  retry: false,
})

watch(
  () => search.data.value,
  (value) => {
    if (value) emit('result', value)
  },
)

watch(
  () => search.error.value,
  (value) => {
    if (value) emit('error', value)
  },
)

const reasons = computed(() =>
  search.error.value
    ? describeApiError(search.error.value, {
        failed: t('sanction.failed'),
        refused: t('sanction.forbidden'),
      })
    : [],
)

const refused = computed(
  () => search.error.value instanceof ApiError && search.error.value.kind === 'validation',
)

const result = computed(() => search.data.value)
const running = computed(() => search.isFetching.value)

const decimals = (digits: number) => (value: unknown) =>
  value === null || value === undefined ? '' : fixed(value as number, digits)

function aggregationColumns(): GridColumn[] {
  const figure = (field: string, title: string, digits: number): GridColumn => ({
    field,
    title,
    align: 'end',
    mono: true,
    minWidth: 90,
    format: decimals(digits),
  })

  return [
    figure('sum', t('sanction.col.sum'), 2),
    figure('average', t('sanction.col.average'), 2),
    figure('count', t('sanction.col.count'), 0),
    figure('max', t('sanction.col.max'), 0),
    figure('min', t('sanction.col.min'), 0),
    figure('first', t('sanction.col.first'), 0),
    figure('last', t('sanction.col.last'), 0),
    figure('confidence', t('sanction.col.confidence'), 4),
  ]
}

const entryColumns = computed<GridColumn[]>(() => [
  { field: 'id', title: t('sanction.col.id'), mono: true, width: 90 },
  { field: 'distance', title: t('sanction.col.distance'), align: 'end', mono: true, width: 110 },
  { field: 'value', title: t('sanction.col.element'), minWidth: 200 },
  { field: 'sanctionEntrySourceId', title: t('sanction.col.sourceId'), mono: true, width: 110 },
  { field: 'source', title: t('sanction.col.source'), minWidth: 160 },
  { field: 'reference', title: t('sanction.col.reference'), minWidth: 160 },
])

const totalColumns = computed(aggregationColumns)

const sourceColumns = computed<GridColumn[]>(() => [
  { field: 'sourceId', title: t('sanction.col.sourceId'), mono: true, width: 110 },
  { field: 'sourceName', title: t('sanction.col.source'), minWidth: 160 },
  ...aggregationColumns(),
])

const sliders = computed(() => [
  { key: 'distance' as const, label: t('sanction.distance'), limits: sanctionLimits.distance },
  {
    key: 'maxDistanceRatio' as const,
    label: t('sanction.maxDistanceRatio'),
    limits: sanctionLimits.maxDistanceRatio,
  },
  {
    key: 'maxCoverageRatio' as const,
    label: t('sanction.maxCoverageRatio'),
    limits: sanctionLimits.maxCoverageRatio,
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="sanction-search">
    <PanelCard v-if="showForm" key="form" :title="t('sanction.title')" :level="2">
      <form class="flex flex-col gap-4" @submit.prevent="commit">
        <fieldset class="fieldset">
          <label class="label" :for="`${uid}-text`">{{ t('sanction.multiPartString') }}</label>
          <input
            :id="`${uid}-text`"
            v-model="draft.multiPartString"
            type="search"
            class="input input-sm w-full max-w-xl"
            autocomplete="off"
            data-testid="sanction-text"
          />
          <p class="label">{{ t('sanction.multiPartHint') }}</p>
        </fieldset>

        <div class="grid gap-4 md:grid-cols-3">
          <div v-for="slider in sliders" :key="slider.key" class="flex flex-col gap-1">
            <div class="flex items-baseline justify-between gap-2">
              <label class="label" :for="`${uid}-${slider.key}`">{{ slider.label }}</label>
              <output
                class="badge badge-sm font-mono"
                :for="`${uid}-${slider.key}`"
                :data-testid="`sanction-${slider.key}-value`"
                >{{ draft[slider.key] }}</output
              >
            </div>
            <input
              :id="`${uid}-${slider.key}`"
              v-model.number="draft[slider.key]"
              type="range"
              class="range range-sm"
              :min="slider.limits.min"
              :max="slider.limits.max"
              :step="slider.limits.step"
              :data-testid="`sanction-${slider.key}`"
            />
          </div>
        </div>

        <div>
          <button
            type="submit"
            class="btn btn-sm btn-primary"
            :disabled="!canSearch(draft) || running"
            data-testid="sanction-check"
          >
            <span v-if="running" key="busy" class="loading loading-spinner loading-xs"></span>
            {{ running ? t('sanction.checking') : t('sanction.check') }}
          </button>
        </div>
      </form>
    </PanelCard>

    <div class="flex min-w-0 flex-col gap-4" aria-live="polite" data-testid="sanction-results">
      <ProblemAlert
        v-if="search.isError.value"
        key="failed"
        :reasons="reasons"
        :title="refused ? t('sanction.validation') : undefined"
        data-testid="sanction-error"
      />

      <p
        v-else-if="!result && !running"
        key="prompt"
        class="text-sm text-secondary"
        data-testid="sanction-prompt"
      >
        {{ t('sanction.prompt') }}
      </p>

      <template v-else-if="result">
        <p
          v-if="result.entries.length === 0"
          key="zero"
          class="text-sm text-secondary"
          data-testid="sanction-zero"
        >
          {{ t('sanction.zero') }}
        </p>

        <PanelCard key="entries" :title="t('sanction.entries')" :level="2" compact>
          <template #actions>
            <span class="badge ms-auto" data-testid="sanction-count">{{
              t('sanction.matches', result.entries.length)
            }}</span>
          </template>
          <DataGrid
            :columns="entryColumns"
            :rows="result.entries"
            :direction="direction"
            :empty-text="t('sanction.empty')"
            :label="t('sanction.entries')"
            :page-size="20"
            :pager-labels="pagerLabels"
            auto-height
          />
        </PanelCard>

        <PanelCard key="total" :title="t('sanction.total')" :level="2" compact>
          <DataGrid
            :columns="totalColumns"
            :rows="result.total ? [result.total] : []"
            :direction="direction"
            :empty-text="t('sanction.empty')"
            :label="t('sanction.total')"
            auto-height
          />
        </PanelCard>

        <PanelCard key="bySource" :title="t('sanction.bySource')" :level="2" compact>
          <DataGrid
            :columns="sourceColumns"
            :rows="result.bySource"
            :direction="direction"
            :empty-text="t('sanction.empty')"
            :label="t('sanction.bySource')"
            auto-height
          />
        </PanelCard>
      </template>
    </div>
  </div>
</template>
