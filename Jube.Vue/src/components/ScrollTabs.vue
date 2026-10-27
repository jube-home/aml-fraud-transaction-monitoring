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
import { onBeforeUnmount, onMounted, ref, useTemplateRef } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'

defineProps<{ label: string }>()

const strip = useTemplateRef<HTMLDivElement>('strip')
const canBack = ref(false)
const canForward = ref(false)

let observer: ResizeObserver | undefined

function measure(): void {
  const element = strip.value

  if (!element) return

  const max = element.scrollWidth - element.clientWidth
  const at = Math.abs(element.scrollLeft)

  canBack.value = at > 1
  canForward.value = at < max - 1
}

function nudge(direction: 1 | -1): void {
  const element = strip.value

  if (!element) return

  const rtl = getComputedStyle(element).direction === 'rtl' ? -1 : 1

  element.scrollBy({ left: direction * rtl * element.clientWidth * 0.6, behavior: 'smooth' })
}

function onWheel(event: WheelEvent): void {
  const element = strip.value

  if (!element || Math.abs(event.deltaX) >= Math.abs(event.deltaY)) return
  if (element.scrollWidth <= element.clientWidth) return

  element.scrollLeft += event.deltaY
  event.preventDefault()
}

onMounted(() => {
  measure()

  if (strip.value && typeof ResizeObserver !== 'undefined') {
    observer = new ResizeObserver(measure)
    observer.observe(strip.value)
  }
})

onBeforeUnmount(() => observer?.disconnect())
</script>

<template>
  <div class="relative min-w-0 border-b border-base-300">
    <div
      ref="strip"
      role="tablist"
      :aria-label="label"
      class="tabs tabs-border flex-nowrap overflow-x-auto overflow-y-hidden [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      @scroll.passive="measure"
      @wheel="onWheel"
    >
      <slot />
    </div>
    <button
      v-if="canBack"
      key="back"
      type="button"
      tabindex="-1"
      aria-hidden="true"
      class="absolute inset-y-0 start-0 flex w-8 items-center justify-start bg-linear-to-r from-base-100 to-transparent text-secondary hover:text-primary rtl:bg-linear-to-l"
      data-testid="tabs-back"
      @click="nudge(-1)"
    >
      <ChevronLeft class="size-4 rtl:rotate-180" />
    </button>
    <button
      v-if="canForward"
      key="forward"
      type="button"
      tabindex="-1"
      aria-hidden="true"
      class="absolute inset-y-0 end-0 flex w-8 items-center justify-end bg-linear-to-l from-base-100 to-transparent text-secondary hover:text-primary rtl:bg-linear-to-r"
      data-testid="tabs-forward"
      @click="nudge(1)"
    >
      <ChevronRight class="size-4 rtl:rotate-180" />
    </button>
  </div>
</template>
