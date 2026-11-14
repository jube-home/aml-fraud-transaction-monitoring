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
import {computed, useTemplateRef} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {useApi} from '../api/useApi'
import {queryKeys} from '../api/queryKeys'
import {integrityApi} from '../integrity/api'
import {entityKindLabel} from '../integrity/labels'
import {clusterKind, dependencyKinds, encodeExpanded, toggleKind} from '../integrity/expansion'
import {useUiStore} from '../stores/ui'
import JubeChart from './JubeChart.vue'
import ProblemAlert from './ProblemAlert.vue'
import {useSvgViewport} from './useSvgViewport'

const props = defineProps<{
  modelId: number
  selected?: string | null
  expanded?: string[] | null
}>()

const emit = defineEmits<{
  select: [id: string | null]
  'update:expanded': [kinds: string[] | null]
}>()

const {t, te} = useI18n()
const ui = useUiStore()
const wrap = useTemplateRef<HTMLDivElement>('wrap')
const scroller = useTemplateRef<HTMLDivElement>('scroller')

const viewport = useSvgViewport(wrap, scroller)
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const graph = useQuery({
  queryKey: computed(() => queryKeys.integrityGraph(props.modelId)),
  queryFn: () => integrityApi.graph(props.modelId),
  retry: false,
})

const encoded = computed(() => (props.expanded ? encodeExpanded(props.expanded) : null))

const svg = useQuery({
  queryKey: computed(
    () => ['integrity', 'graphSvg', props.modelId, dark.value, encoded.value] as const,
  ),
  queryFn: () =>
    useApi<string>(integrityApi.graphSvgPath(props.modelId), {
      query: {dark: dark.value, ...(encoded.value === null ? {} : {expanded: encoded.value})},
    }),
  retry: false,
})

const kinds = computed(() =>
  dependencyKinds(graph.data.value, props.expanded ?? null).map((entry) => ({
    ...entry,
    label: entityKindLabel(t, te, entry.kind),
  })),
)

function toggle(kind: string): void {
  emit('update:expanded', toggleKind(kinds.value, kind))
}

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

function onPick(id: string | null): void {
  if (viewport.dragging.value) return

  const kind = clusterKind(id)

  if (kind === null) emit('select', id)
  else toggle(kind)
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
        {{ t('integrity.deps.truncated', {count: graph.data.value.maxNodes}) }}
      </p>
      <p
        v-if="unconnected > 0"
        key="unconnected"
        class="text-secondary"
        data-testid="dependency-unconnected"
      >
        {{ t('integrity.deps.unconnected', {count: unconnected}) }}
      </p>
      <div class="join ms-auto" role="group" :aria-label="t('integrity.deps.zoom')">
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-out"
          @click="viewport.zoomOut()"
        >
          {{ t('integrity.deps.zoomOut') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-fit"
          @click="viewport.fit()"
        >
          {{ t('integrity.deps.fit') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          data-testid="diagram-zoom-in"
          @click="viewport.zoomIn()"
        >
          {{ t('integrity.deps.zoomIn') }}
        </button>
      </div>
    </div>

    <div
      v-if="kinds.length > 0"
      key="kinds"
      class="flex flex-wrap items-center gap-2 text-sm"
      role="group"
      :aria-label="t('integrity.deps.kinds')"
      data-testid="dependency-kinds"
    >
      <span class="text-secondary">{{ t('integrity.deps.kinds') }}</span>
      <button
        v-for="entry in kinds"
        :key="entry.kind"
        type="button"
        class="btn btn-xs"
        :class="entry.open ? 'btn-primary' : 'btn-outline'"
        :aria-pressed="entry.open"
        :data-testid="`dependency-kind-${entry.kind}`"
        @click="toggle(entry.kind)"
      >
        {{ entry.label }}
        <span class="badge badge-xs">{{ entry.count }}</span>
      </button>
      <button
        v-if="expanded"
        key="automatic"
        type="button"
        class="btn btn-xs btn-ghost"
        data-testid="dependency-kinds-automatic"
        @click="emit('update:expanded', null)"
      >
        {{ t('integrity.deps.automatic') }}
      </button>
    </div>
    <p class="text-xs text-secondary" data-testid="dependency-key">{{ t('integrity.deps.key') }}</p>

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
      ref="scroller"
      key="chart"
      class="max-h-[44rem] min-w-0 touch-pan-x touch-pan-y overflow-auto rounded-box border border-base-300 bg-base-100 [user-select:none]"
      :class="viewport.dragging.value ? 'cursor-grabbing' : 'cursor-grab'"
      tabindex="0"
      :aria-label="t('integrity.deps.label')"
      @pointerdown="viewport.onPointerDown"
      @pointermove="viewport.onPointerMove"
      @pointerup="viewport.onPointerUp"
      @pointercancel="viewport.onPointerUp"
      @keydown="viewport.onKey"
    >
      <div ref="wrap" class="min-w-full">
        <JubeChart
          :svg="markup!"
          :label="t('integrity.deps.label')"
          interactive
          :selected="selected ?? null"
          @pick="onPick"
        />
      </div>
    </div>
  </div>
</template>
