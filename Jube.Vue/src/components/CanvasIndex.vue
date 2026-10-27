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
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { activeSection, isAtEnd } from '../canvas/spy'

const props = defineProps<{ items: { id: string; label: string }[]; label: string }>()

const emit = defineEmits<{ pick: [id: string] }>()

const holdMs = 800
const active = ref<string | null>(null)
let frame = 0
let holdUntil = 0
let observer: ResizeObserver | undefined
let watched: Element | null = null

function watchColumn(): void {
  const column = props.items[0] ? document.getElementById(props.items[0].id)?.parentElement : null

  if (!column || column === watched || typeof ResizeObserver === 'undefined') return

  observer?.disconnect()
  observer = new ResizeObserver(schedule)
  observer.observe(column)
  watched = column
}

function measure(): void {
  frame = 0
  watchColumn()

  if (performance.now() < holdUntil) return

  const boxes = props.items.flatMap((item) => {
    const element = document.getElementById(item.id)

    return element ? [{ id: item.id, top: element.getBoundingClientRect().top }] : []
  })

  const first = props.items[0] ? document.getElementById(props.items[0].id) : null

  active.value = activeSection(boxes, Math.round(window.innerHeight * 0.4), isAtEnd(first))
}

function schedule(): void {
  if (frame === 0) frame = requestAnimationFrame(measure)
}

function pick(id: string): void {
  active.value = id
  holdUntil = performance.now() + holdMs
  setTimeout(schedule, holdMs + 50)
  emit('pick', id)
}

onMounted(() => {
  document.addEventListener('scroll', schedule, { capture: true, passive: true })
  window.addEventListener('resize', schedule)
  schedule()
})

onBeforeUnmount(() => {
  observer?.disconnect()
  document.removeEventListener('scroll', schedule, { capture: true })
  window.removeEventListener('resize', schedule)

  if (frame !== 0) cancelAnimationFrame(frame)
})

watch(() => props.items, schedule, { deep: true })
</script>

<template>
  <nav :aria-label="label" class="min-w-0" data-testid="canvas-index">
    <ul
      class="menu menu-sm menu-horizontal w-full flex-nowrap overflow-x-auto lg:menu-vertical lg:overflow-visible"
    >
      <li v-for="item in items" :key="item.id">
        <button
          type="button"
          :class="{ 'menu-active': active === item.id }"
          :aria-current="active === item.id ? 'location' : undefined"
          :data-testid="`canvas-index-${item.id}`"
          @click="pick(item.id)"
        >
          {{ item.label }}
        </button>
      </li>
    </ul>
  </nav>
</template>
