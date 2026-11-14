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
import {computed, onBeforeUnmount, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {Pause, Play, RotateCcw} from 'lucide-vue-next'
import JubeChart from '../components/JubeChart.vue'
import {useFormat} from '../composables/useFormat'
import {brushDebounceMilliseconds, playbackStepMilliseconds} from './state'
import {parseTimeStripBins, type TimeStripBin} from './timeStrip'
import type {GraphWindow} from './types'

const props = withDefaults(
  defineProps<{
    svg: string | null
    cursor: number | null
    pending?: boolean
    failed?: boolean
  }>(),
  {pending: false, failed: false},
)

const emit = defineEmits<{
  'update:window': [window: GraphWindow]
  'update:cursor': [cursor: number | null]
  reset: []
}>()

const {t} = useI18n()
const {dateTime} = useFormat()

const bins = computed<TimeStripBin[]>(() => (props.svg ? parseTimeStripBins(props.svg) : []))
const last = computed(() => Math.max(0, bins.value.length - 1))

const start = ref(0)
const end = ref(0)
const playing = ref(false)

const reducedMotion =
  typeof window !== 'undefined' && typeof window.matchMedia === 'function'
    ? window.matchMedia('(prefers-reduced-motion: reduce)').matches
    : false

let brushTimer: ReturnType<typeof setTimeout> | undefined
let playTimer: ReturnType<typeof setInterval> | undefined

watch(
  bins,
  (value) => {
    start.value = 0
    end.value = Math.max(0, value.length - 1)
    stop()
  },
  {immediate: true},
)

const cursorIndex = computed(() => {
  if (props.cursor === null) return null

  const index = bins.value.findIndex((bin) => bin.to >= props.cursor!)

  return index < 0 ? last.value : index
})

const cursorText = computed(() => {
  const index = cursorIndex.value

  if (index === null) return ''

  const bin = bins.value[index]

  return bin ? dateTime(new Date(bin.to * 1000)) : ''
})

function binText(index: number, edge: 'from' | 'to'): string {
  const bin = bins.value[index]

  return bin ? dateTime(new Date(bin[edge] * 1000)) : ''
}

function brushed(): void {
  clearTimeout(brushTimer)
  brushTimer = setTimeout(() => {
    const first = bins.value[Math.min(start.value, end.value)]
    const final = bins.value[Math.max(start.value, end.value)]

    if (!first || !final) return
    if (start.value === 0 && end.value === last.value) return

    emit('update:window', {
      from: new Date(first.from * 1000).toISOString(),
      to: new Date(final.to * 1000).toISOString(),
    })
  }, brushDebounceMilliseconds)
}

function onStart(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  start.value = Math.min(value, end.value)
  brushed()
}

function onEnd(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  end.value = Math.max(value, start.value)
  brushed()
}

function onCursor(event: Event): void {
  const index = Number((event.target as HTMLInputElement).value)
  const bin = bins.value[index]

  emit('update:cursor', bin ? bin.to : null)
}

function step(): void {
  const current = cursorIndex.value ?? -1
  const next = current + 1

  if (next > last.value) {
    stop()
    emit('update:cursor', null)

    return
  }

  emit('update:cursor', bins.value[next]!.to)
}

function play(): void {
  if (reducedMotion || bins.value.length === 0) return

  playing.value = true
  step()
  playTimer = setInterval(step, playbackStepMilliseconds)
}

function stop(): void {
  playing.value = false
  clearInterval(playTimer)
  playTimer = undefined
}

function toggle(): void {
  if (playing.value) stop()
  else play()
}

function clear(): void {
  stop()
  emit('update:cursor', null)
}

onBeforeUnmount(() => {
  clearTimeout(brushTimer)
  clearInterval(playTimer)
})
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="graph-time-strip">
    <h3 class="sr-only">{{ t('graph.time.label') }}</h3>

    <div
      v-if="pending && svg === null"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('graph.time.loading')"
    ></div>
    <p v-else-if="failed && svg === null" key="failed" class="text-xs" role="alert">
      {{ t('graph.time.failed') }}
    </p>
    <template v-else-if="svg !== null">
      <JubeChart :svg="svg" :label="t('graph.time.label')"/>

      <div class="flex flex-wrap items-center gap-3 text-xs" v-if="bins.length > 0">
        <label class="flex items-center gap-2">
          <span class="text-secondary">{{ t('graph.time.from') }}</span>
          <input
            type="range"
            class="range range-xs w-40"
            min="0"
            :max="last"
            :value="start"
            :aria-valuetext="binText(start, 'from')"
            data-testid="graph-brush-start"
            @input="onStart"
          />
        </label>
        <label class="flex items-center gap-2">
          <span class="text-secondary">{{ t('graph.time.to') }}</span>
          <input
            type="range"
            class="range range-xs w-40"
            min="0"
            :max="last"
            :value="end"
            :aria-valuetext="binText(end, 'to')"
            data-testid="graph-brush-end"
            @input="onEnd"
          />
        </label>
        <button
          type="button"
          class="btn btn-xs"
          data-testid="graph-window-reset"
          @click="emit('reset')"
        >
          <RotateCcw class="size-3" aria-hidden="true"/>
          {{ t('graph.time.reset') }}
        </button>

        <div class="ms-auto flex items-center gap-2">
          <button
            type="button"
            class="btn btn-xs"
            :disabled="reducedMotion"
            :aria-pressed="playing"
            :title="reducedMotion ? t('graph.time.reduced') : undefined"
            data-testid="graph-play"
            @click="toggle"
          >
            <Pause v-if="playing" key="pause" class="size-3" aria-hidden="true"/>
            <Play v-else key="play" class="size-3" aria-hidden="true"/>
            {{ playing ? t('graph.time.pause') : t('graph.time.play') }}
          </button>
          <label class="flex items-center gap-2">
            <span class="text-secondary">{{ t('graph.time.cursor') }}</span>
            <input
              type="range"
              class="range range-xs range-primary w-40"
              min="0"
              :max="last"
              :value="cursorIndex ?? last"
              :aria-valuetext="cursorText"
              data-testid="graph-cursor"
              @input="onCursor"
            />
          </label>
          <span
            v-if="cursor !== null"
            key="cursor"
            class="badge badge-ghost badge-sm font-mono"
            data-testid="graph-cursor-text"
          >{{ cursorText }}</span
          >
          <button
            v-if="cursor !== null"
            key="clear"
            type="button"
            class="btn btn-xs"
            data-testid="graph-cursor-clear"
            @click="clear"
          >
            {{ t('graph.time.clear') }}
          </button>
        </div>
      </div>
      <p v-if="reducedMotion" key="reduced" class="text-xs text-secondary">
        {{ t('graph.time.reduced') }}
      </p>
    </template>
  </section>
</template>
