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
import { computed, nextTick, onBeforeUnmount, onMounted, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Circle } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import { impactKeys, laneKeys, laneTone, toneClass, triggerKeys } from './labels'
import { defaultTimelineOptions, layoutTimeline, scrollDelta, tickFormat } from './riskTimeline'
import type { CaseRiskHistoryItem } from './types'

const props = defineProps<{ items: CaseRiskHistoryItem[] }>()

const laneHeight = 64
const stemTail = 20

const { t, locale } = useI18n()
const { number, percent, dateTime } = useFormat()

const scroller = useTemplateRef<HTMLElement>('scroller')
const containerWidth = ref(0)
let observer: ResizeObserver | null = null

const layout = computed(() =>
  layoutTimeline(
    props.items.map((item) => Date.parse(item.createdDate)),
    defaultTimelineOptions,
    containerWidth.value,
  ),
)

const ordered = computed(() =>
  layout.value.markers.map((marker) => ({ marker, item: props.items[marker.index]! })),
)

const picked = ref<number | null>(null)

const selectedPosition = computed(() => {
  const found = ordered.value.findIndex((entry) => entry.item.sequence === picked.value)

  return found === -1 ? ordered.value.length - 1 : found
})

const selected = computed(() => ordered.value[selectedPosition.value])

const previous = computed(() =>
  selectedPosition.value > 0 ? ordered.value[selectedPosition.value - 1]!.item : null,
)

const tickLabels = computed(() => {
  const format = new Intl.DateTimeFormat(
    locale.value === 'en' ? 'en-GB' : locale.value,
    tickFormat[layout.value.unit],
  )

  return layout.value.ticks.map((tick) => ({ x: tick.x, text: format.format(tick.at) }))
})

const groupLabel = computed(() => {
  const first = ordered.value[0]
  const last = ordered.value[ordered.value.length - 1]

  return t('cases.peek.timelineLabel', {
    count: number(ordered.value.length),
    from: first ? dateTime(first.item.createdDate) : '',
    to: last ? dateTime(last.item.createdDate) : '',
  })
})

function riskText(item: CaseRiskHistoryItem): string {
  return [
    percent(item.likelihood),
    t(impactKeys[item.impact] ?? 'cases.impact.moderate'),
    number(item.priority),
  ].join(' · ')
}

function dotClass(lane: number): string {
  const tone = laneTone(lane)

  if (tone === 'error') return 'text-error'
  if (tone === 'warning') return 'text-warning'

  return 'text-base-content'
}

function select(sequence: number) {
  picked.value = sequence
}

function revealSelected() {
  const node = scroller.value
  const button = node?.querySelector<HTMLElement>('button[aria-pressed="true"]')

  if (!node || !button) return

  const view = node.getBoundingClientRect()
  const box = button.getBoundingClientRect()

  node.scrollLeft += scrollDelta(box.left, box.right, view.left, view.right)
}

function measure() {
  containerWidth.value = scroller.value?.clientWidth ?? 0
}

onMounted(() => {
  measure()

  if (typeof ResizeObserver !== 'undefined' && scroller.value) {
    observer = new ResizeObserver(measure)
    observer.observe(scroller.value)
  }

  nextTick(revealSelected)
})

onBeforeUnmount(() => {
  observer?.disconnect()
  observer = null
})

watch(
  () => [selected.value?.item.sequence, layout.value.width],
  () => nextTick(revealSelected),
)
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="risk-timeline">
    <div
      ref="scroller"
      role="group"
      tabindex="0"
      :aria-label="groupLabel"
      class="max-w-full overflow-x-auto overscroll-x-contain pb-1 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      data-testid="risk-timeline-scroll"
    >
      <div class="relative" :style="{ width: `${layout.width}px` }">
        <div class="relative h-6 border-b border-base-content/40" aria-hidden="true">
          <span
            v-for="tick in tickLabels"
            :key="tick.x"
            class="absolute bottom-0 border-s border-base-content/40 ps-1 text-xs whitespace-nowrap text-base-content/70"
            :style="{ insetInlineStart: `${tick.x}px` }"
          >
            {{ tick.text }}
          </span>
        </div>
        <ol
          role="list"
          class="relative m-0 p-0"
          :style="{ height: `${layout.lanes * laneHeight + stemTail}px` }"
        >
          <li
            v-for="entry in ordered"
            :key="entry.item.sequence"
            class="absolute top-0"
            :style="{ insetInlineStart: `${entry.marker.x}px` }"
            data-testid="risk-timeline-marker"
            :data-lane="entry.marker.lane"
            :data-x="entry.marker.x"
          >
            <Circle
              :size="12"
              :class="['absolute top-0 -ms-1.5 -mt-1.5 fill-current', dotClass(entry.item.lane)]"
              aria-hidden="true"
            />
            <span
              class="absolute top-0 border-s border-base-content/40"
              :style="{ height: `${entry.marker.lane * laneHeight + stemTail}px` }"
              aria-hidden="true"
            />
            <button
              type="button"
              :aria-pressed="entry.item.sequence === selected?.item.sequence"
              :class="[
                'absolute z-10 flex flex-col items-start gap-0.5 rounded-box border bg-base-100 px-2 py-1 text-start focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary',
                entry.item.sequence === selected?.item.sequence
                  ? 'border-primary ring-2 ring-primary'
                  : 'border-base-300',
              ]"
              :style="{
                top: `${entry.marker.lane * laneHeight + stemTail}px`,
                width: `${defaultTimelineOptions.labelWidth}px`,
              }"
              @click="select(entry.item.sequence)"
              @focus="select(entry.item.sequence)"
            >
              <span :class="toneClass[laneTone(entry.item.lane)]">
                {{ t(laneKeys[entry.item.lane] ?? 'cases.lane.standard') }}
              </span>
              <time
                class="font-mono text-xs leading-tight text-base-content/70"
                :datetime="entry.item.createdDate"
              >
                {{ dateTime(entry.item.createdDate) }}
              </time>
            </button>
          </li>
        </ol>
      </div>
    </div>
    <div
      v-if="selected"
      class="flex flex-col gap-1 rounded-box border border-base-300 p-2"
      role="region"
      :aria-label="t('cases.peek.timelineSelected')"
      aria-live="polite"
      data-testid="risk-timeline-detail"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span :class="toneClass[laneTone(selected.item.lane)]">
          {{ t(laneKeys[selected.item.lane] ?? 'cases.lane.standard') }}
        </span>
        <span class="text-sm">
          {{ t(triggerKeys[selected.item.trigger] ?? 'cases.trigger.sweep') }}
        </span>
      </div>
      <div class="font-mono text-xs text-base-content/70">
        {{ dateTime(selected.item.createdDate) }}
      </div>
      <div class="font-mono text-xs">{{ riskText(selected.item) }}</div>
      <div v-if="previous" key="change" class="font-mono text-xs">
        {{
          t('cases.peek.timelinePriority', {
            from: number(previous.priority),
            to: number(selected.item.priority),
          })
        }}
      </div>
    </div>
  </div>
</template>
