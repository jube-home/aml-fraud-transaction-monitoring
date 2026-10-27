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
import { computed, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { useApi } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { integrityApi } from '../integrity/api'
import { useUiStore } from '../stores/ui'
import JubeChart from './JubeChart.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number; selected?: string | null }>()

const emit = defineEmits<{ select: [id: string | null] }>()

const { t } = useI18n()
const ui = useUiStore()
const wrap = useTemplateRef<HTMLDivElement>('wrap')

const zoom = ref(1)
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const graph = useQuery({
  queryKey: computed(() => queryKeys.integrityGraph(props.modelId)),
  queryFn: () => integrityApi.graph(props.modelId),
  retry: false,
})

const svg = useQuery({
  queryKey: computed(() => ['integrity', 'graphSvg', props.modelId, dark.value] as const),
  queryFn: () =>
    useApi<string>(integrityApi.graphSvgPath(props.modelId), { query: { dark: dark.value } }),
  retry: false,
})

const unconnected = computed(() => {
  const data = graph.data.value

  if (!data) return 0

  const linked = new Set(data.edges.flatMap((edge) => [edge.from, edge.to]))

  return data.nodes.filter((node) => !linked.has(node.id)).length
})

const markup = computed(() => {
  const text = svg.data.value

  return typeof text === 'string' && text.trimStart().startsWith('<svg') ? text : null
})

watch(
  zoom,
  (value) => {
    if (wrap.value) wrap.value.style.width = `${value * 100}%`
  },
  { flush: 'post' },
)

function step(direction: 1 | -1): void {
  zoom.value = Math.min(6, Math.max(1, Math.round((zoom.value + direction * 0.5) * 2) / 2))
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="dependency-diagram">
    <div class="flex flex-wrap items-center gap-3 text-sm">
      <p class="text-secondary">{{ t('integrity.deps.note') }}</p>
      <p
        v-if="graph.data.value?.truncated"
        key="truncated"
        class="font-semibold"
        data-testid="dependency-truncated"
      >
        {{ t('integrity.deps.truncated', { count: graph.data.value.maxNodes }) }}
      </p>
      <p
        v-if="unconnected > 0"
        key="unconnected"
        class="text-secondary"
        data-testid="dependency-unconnected"
      >
        {{ t('integrity.deps.unconnected', { count: unconnected }) }}
      </p>
      <div class="join ms-auto" role="group" :aria-label="t('integrity.deps.zoom')">
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-out"
          @click="step(-1)"
        >
          {{ t('integrity.deps.zoomOut') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-fit"
          @click="zoom = 1"
        >
          {{ t('integrity.deps.fit') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-in"
          @click="step(1)"
        >
          {{ t('integrity.deps.zoomIn') }}
        </button>
      </div>
    </div>

    <ProblemAlert
      v-if="svg.isError.value || (svg.isSuccess.value && !markup)"
      key="failed"
      :reasons="[t('integrity.deps.failed')]"
    />
    <div
      v-else-if="svg.isPending.value"
      key="loading"
      class="skeleton h-80 w-full"
      role="status"
      :aria-label="t('chart.loading')"
    ></div>
    <div
      v-else
      key="chart"
      class="max-h-[44rem] min-w-0 overflow-auto rounded-box border border-base-300 bg-base-100"
    >
      <div ref="wrap" class="min-w-full">
        <JubeChart
          :svg="markup!"
          :label="t('integrity.deps.label')"
          interactive
          :selected="selected ?? null"
          @pick="emit('select', $event)"
        />
      </div>
    </div>
  </div>
</template>
