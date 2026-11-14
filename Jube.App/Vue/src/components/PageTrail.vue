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
import { computed, useId } from 'vue'
import { useI18n } from 'vue-i18n'

export interface Crumb {
  id: string | number
  label: string
  backLabel?: string
  testid?: string
}

const props = withDefaults(defineProps<{ crumbs: Crumb[]; label: string; keepTail?: number }>(), {
  keepTail: 2,
})

const { t } = useI18n()

const emit = defineEmits<{ select: [id: string | number] }>()

const uid = useId()
const popoverId = `trail-${uid}`
const anchorName = `--trail-${uid}`

const collapses = computed(() => props.crumbs.length > props.keepTail + 2)
const hidden = computed(() => (collapses.value ? props.crumbs.slice(1, props.crumbs.length - props.keepTail) : []))
const visible = computed(() => {
  if (!collapses.value) return props.crumbs.map((crumb, index) => ({ crumb, index }))
  const tailStart = props.crumbs.length - props.keepTail
  return props.crumbs
    .map((crumb, index) => ({ crumb, index }))
    .filter(({ index }) => index === 0 || index >= tailStart)
})

function pick(event: Event, id: string | number) {
  ;(event.currentTarget as HTMLElement).closest<HTMLElement>('[popover]')?.hidePopover()
  emit('select', id)
}
</script>

<template>
  <nav :aria-label="label" class="min-w-0">
    <div class="breadcrumbs overflow-x-clip overflow-y-visible text-sm">
      <ul class="min-w-0">
        <template v-for="{ crumb, index } in visible" :key="crumb.id">
          <li v-if="collapses && index === crumbs.length - keepTail" class="shrink-0">
            <button
              type="button"
              class="btn btn-ghost btn-xs"
              :aria-label="t('pageTrail.more')"
              :popovertarget="popoverId"
              :style="{ anchorName }"
              data-testid="trail-more"
            >
              …
            </button>
            <ul
              :id="popoverId"
              class="dropdown menu menu-sm bg-base-100 rounded-box z-10 w-64 p-2 shadow-sm"
              popover
              :style="{ positionAnchor: anchorName }"
            >
              <li v-for="h in hidden" :key="h.id">
                <button type="button" :title="h.label" :aria-label="h.backLabel" :data-testid="h.testid" @click="pick($event, h.id)">
                  <span class="truncate">{{ h.label }}</span>
                </button>
              </li>
            </ul>
          </li>
          <li class="min-w-0" :class="index === crumbs.length - 1 ? '' : 'shrink-0'">
            <span
              v-if="index === crumbs.length - 1"
              class="inline-flex min-w-0 items-center gap-2 font-semibold text-primary"
              aria-current="page"
              :title="crumb.label"
              :data-testid="crumb.testid"
            >
              <span class="status status-primary shrink-0" aria-hidden="true"></span>
              <span class="max-w-[28ch] truncate">{{ crumb.label }}</span>
            </span>
            <button
              v-else
              type="button"
              class="link link-hover inline-flex min-w-0 items-center gap-2"
              :title="crumb.label"
              :aria-label="crumb.backLabel"
              :data-testid="crumb.testid"
              @click="emit('select', crumb.id)"
            >
              <span class="max-w-[18ch] truncate">{{ crumb.label }}</span>
            </button>
          </li>
        </template>
      </ul>
    </div>
  </nav>
</template>
