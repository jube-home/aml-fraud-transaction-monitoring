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
import FieldHelp from './FieldHelp.vue'

const props = withDefaults(
  defineProps<{ id: string; title: string; hint?: string; help?: string; open?: boolean }>(),
  { open: true },
)

const emit = defineEmits<{ 'update:open': [value: boolean] }>()

const root = useTemplateRef<HTMLElement>('root')
const near = ref(typeof IntersectionObserver === 'undefined')
let observer: IntersectionObserver | undefined

onMounted(() => {
  if (near.value || !root.value) return

  observer = new IntersectionObserver(
    (entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        near.value = true
        observer?.disconnect()
      }
    },
    { rootMargin: '600px 0px' },
  )
  observer.observe(root.value)
})

onBeforeUnmount(() => observer?.disconnect())

function toggle(event: Event): void {
  const opened = (event.target as HTMLDetailsElement).open

  if (opened !== props.open) emit('update:open', opened)
}
</script>

<template>
  <section
    :id="id"
    ref="root"
    class="relative scroll-mt-36"
    :aria-labelledby="`${id}-title`"
    :data-testid="`canvas-${id}`"
  >
    <details
      class="collapse collapse-arrow rounded-box border border-base-300 bg-base-100"
      :open="open"
      @toggle="toggle"
    >
      <summary class="collapse-title flex flex-wrap items-baseline gap-x-3 gap-y-1 py-3">
        <h3 :id="`${id}-title`" class="text-sm font-semibold text-primary">{{ title }}</h3>
        <span v-if="hint" key="hint" class="text-xs font-normal text-secondary">{{ hint }}</span>
      </summary>
      <div class="collapse-content flex min-w-0 flex-col gap-4">
        <slot v-if="open && near" key="content" />
        <div v-else key="pending" class="skeleton h-32 w-full" aria-hidden="true"></div>
      </div>
    </details>
    <div v-if="help" key="help" class="absolute end-12 top-2.5">
      <FieldHelp :label="title" :text="help" />
    </div>
  </section>
</template>
