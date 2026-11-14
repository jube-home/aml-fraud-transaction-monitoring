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
import {nextTick, useTemplateRef, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {Maximize, ZoomIn, ZoomOut} from 'lucide-vue-next'
import JubeChart from '../components/JubeChart.vue'
import {containZoom, useSvgViewport, viewBoxSize} from '../components/useSvgViewport'

const props = withDefaults(
  defineProps<{
    svg: string | null
    selected: string | null
    selectedEdge: string | null
    hidden: Set<string>
    emphasised: Set<string>
    skeleton?: boolean
    refreshing?: boolean
    picking?: boolean
  }>(),
  {skeleton: false, refreshing: false, picking: false},
)

const emit = defineEmits<{ pick: [id: string | null]; pickEdge: [id: string | null] }>()

const {t} = useI18n()
const wrap = useTemplateRef<HTMLDivElement>('wrap')
const scroller = useTemplateRef<HTMLDivElement>('scroller')
const viewport = useSvgViewport(wrap, scroller, {fitZoom})

function fitZoom(): number {
  const box = scroller.value
  const size = viewBoxSize(props.svg)

  if (!box || !size) return 1

  return containZoom(box.clientWidth, box.clientHeight, size.width, size.height)
}

watch(
  () => props.svg,
  async () => {
    await nextTick()
    viewport.fit()
  },
  {immediate: true},
)

function onPick(id: string | null): void {
  if (!viewport.dragging.value) emit('pick', id)
}

function onPickEdge(id: string | null): void {
  if (!viewport.dragging.value) emit('pickEdge', id)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="graph-canvas">
    <div class="flex items-center gap-2">
      <span
        v-if="picking"
        key="picking"
        class="badge badge-info badge-sm"
        role="status"
        data-testid="graph-canvas-picking"
      >{{ t('graph.paths.pickOnCanvas') }}</span
      >
      <span
        v-if="refreshing"
        key="refreshing"
        class="loading loading-spinner loading-xs"
        role="status"
        :aria-label="t('graph.canvas.refreshing')"
        data-testid="graph-refreshing"
      ></span>
      <div class="join ms-auto" role="group" :aria-label="t('graph.canvas.zoom')">
        <button
          type="button"
          class="btn btn-sm join-item"
          :aria-label="t('graph.canvas.zoomOut')"
          data-testid="graph-zoom-out"
          @click="viewport.zoomOut()"
        >
          <ZoomOut class="size-4" aria-hidden="true"/>
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :aria-label="t('graph.canvas.fit')"
          data-testid="graph-zoom-fit"
          @click="viewport.fit()"
        >
          <Maximize class="size-4" aria-hidden="true"/>
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :aria-label="t('graph.canvas.zoomIn')"
          data-testid="graph-zoom-in"
          @click="viewport.zoomIn()"
        >
          <ZoomIn class="size-4" aria-hidden="true"/>
        </button>
      </div>
    </div>

    <div
      v-if="skeleton || svg === null"
      key="skeleton"
      class="skeleton h-96 w-full"
      role="status"
      :aria-label="t('graph.canvas.loading')"
      data-testid="graph-canvas-skeleton"
    ></div>
    <div
      v-else
      ref="scroller"
      key="chart"
      class="h-[clamp(20rem,calc(100dvh-24rem),44rem)] min-w-0 touch-pan-x touch-pan-y overflow-auto rounded-box border border-base-300 bg-base-100 [user-select:none]"
      :class="[
        viewport.dragging.value ? 'cursor-grabbing' : 'cursor-grab',
        picking ? 'ring-2 ring-info' : '',
      ]"
      tabindex="0"
      :aria-label="t('graph.canvas.label')"
      data-testid="graph-canvas-scroller"
      @pointerdown="viewport.onPointerDown"
      @pointermove="viewport.onPointerMove"
      @pointerup="viewport.onPointerUp"
      @pointercancel="viewport.onPointerUp"
      @keydown="viewport.onKey"
    >
      <div ref="wrap" class="mx-auto w-full">
        <JubeChart
          :svg="svg"
          :label="t('graph.canvas.label')"
          interactive
          :selected="selected"
          :selected-edge="selectedEdge"
          :hidden="hidden"
          :emphasised="emphasised"
          @pick="onPick"
          @pick-edge="onPickEdge"
        />
      </div>
    </div>
  </div>
</template>
