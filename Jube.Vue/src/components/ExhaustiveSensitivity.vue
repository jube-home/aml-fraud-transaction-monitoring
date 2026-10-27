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
import { computed, onBeforeUnmount, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { exhaustiveApi } from '../exhaustive/api'
import { controlsOf, initialParams } from '../exhaustive/model'
import ExhaustiveNarrative from './ExhaustiveNarrative.vue'
import ProblemAlert from './ProblemAlert.vue'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ instanceId: number; guid: string; version: string }>()

const { t } = useI18n()
const { fixed } = useFormat()
const id = useId()

const settleMs = 250

const variables = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'sliders', props.version)),
  queryFn: () => exhaustiveApi.sliders(props.instanceId),
  retry: false,
})

const controls = computed(() => controlsOf(variables.data.value ?? []))
const params = ref<Record<string, string | number>>({})
const settled = ref<Record<string, string | number>>({})
let timer: ReturnType<typeof setTimeout> | undefined

watch(
  controls,
  (value) => {
    params.value = initialParams(value)
    settled.value = { ...params.value }
  },
  { immediate: true },
)

watch(
  params,
  (value) => {
    clearTimeout(timer)
    timer = setTimeout(() => {
      settled.value = { ...value }
    }, settleMs)
  },
  { deep: true },
)

onBeforeUnmount(() => clearTimeout(timer))

const recall = useQuery({
  queryKey: computed(
    () =>
      ['exhaustive', props.instanceId, 'recall', props.guid, props.version, settled.value] as const,
  ),
  queryFn: () => exhaustiveApi.recall(props.guid, settled.value),
  enabled: computed(() => controls.value.length > 0),
  placeholderData: keepPreviousData,
  staleTime: Infinity,
  retry: false,
})

const topologyBody = computed(() =>
  recall.data.value ? { nodeRelevance: recall.data.value.nodeRelevance } : undefined,
)

function setNumber(name: string, event: Event): void {
  params.value = { ...params.value, [name]: Number((event.target as HTMLInputElement).value) }
}

function setCategory(name: string, event: Event): void {
  params.value = { ...params.value, [name]: (event.target as HTMLSelectElement).value }
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="exhaustive-sensitivity">
    <ExhaustiveNarrative :instance-id="instanceId" group="Sensitivity" :version="version" />

    <ProblemAlert
      v-if="variables.isError.value"
      key="failed"
      :reasons="[t('exhaustive.sensitivity.failed')]"
    />
    <p
      v-else-if="controls.length === 0 && !variables.isPending.value"
      key="empty"
      class="text-sm text-secondary"
    >
      {{ t('exhaustive.sensitivity.empty') }}
    </p>
    <template v-else>
      <div
        class="flex flex-wrap items-baseline gap-3"
        role="status"
        data-testid="sensitivity-score"
      >
        <span class="text-xs text-secondary">{{ t('exhaustive.sensitivity.score') }}</span>
        <span class="font-mono text-3xl tabular-nums">{{
          recall.data.value ? fixed(recall.data.value.score, 4) : '–'
        }}</span>
        <span
          v-if="recall.isError.value"
          key="recall-failed"
          class="text-sm text-error"
          role="alert"
          >{{ t('exhaustive.sensitivity.recallFailed') }}</span
        >
      </div>

      <div class="grid min-w-0 gap-x-8 gap-y-3 @container @3xl:grid-cols-2">
        <template v-for="(control, index) in controls" :key="control.name">
          <label
            v-if="control.kind === 'number'"
            key="number"
            class="flex min-w-0 flex-col gap-1"
            :for="`${id}-${index}`"
          >
            <span class="flex items-baseline justify-between gap-3 text-xs">
              <span class="font-medium">{{ control.name }}</span>
              <span class="font-mono tabular-nums">{{ params[control.name] }}</span>
            </span>
            <input
              :id="`${id}-${index}`"
              type="range"
              class="range range-xs w-full"
              :min="control.min"
              :max="control.max"
              :step="control.step"
              :value="params[control.name]"
              :data-testid="`sensitivity-control-${index}`"
              @input="setNumber(control.name, $event)"
            />
          </label>
          <label v-else key="category" class="flex min-w-0 flex-col gap-1" :for="`${id}-${index}`">
            <span class="text-xs font-medium">{{ control.name }}</span>
            <select
              :id="`${id}-${index}`"
              class="select select-sm w-full"
              :value="params[control.name]"
              :data-testid="`sensitivity-control-${index}`"
              @change="setCategory(control.name, $event)"
            >
              <option v-for="option in control.options" :key="option.value" :value="option.value">
                {{ option.text }}
              </option>
            </select>
          </label>
        </template>
      </div>

      <ServerChart
        v-if="topologyBody"
        key="topology"
        :url="`/api/Chart/Exhaustive/Topology/${instanceId}`"
        :body="topologyBody"
        :label="t('exhaustive.sensitivity.topology')"
        :version="version"
      />
    </template>
  </div>
</template>
