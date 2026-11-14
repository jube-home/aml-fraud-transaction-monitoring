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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {AlarmClock, CircleAlert} from 'lucide-vue-next'
import {durationText, slaView} from './slaText'
import type {ChecklistItem} from './types'

const props = defineProps<{ item: ChecklistItem }>()

const {t} = useI18n()

const view = computed(() => slaView(props.item))

const text = computed(() => {
  const current = view.value

  if (current === null) return ''

  return current.minutes === null
    ? t(current.key)
    : t(current.key, {time: durationText(t, current.minutes)})
})

const classes = computed(() => {
  switch (view.value?.tone) {
    case 'error':
      return 'badge badge-sm badge-error'
    default:
      return 'badge badge-sm badge-outline'
  }
})
</script>

<template>
  <span
    v-if="view"
    key="chip"
    class="shrink-0 gap-1"
    :class="classes"
    :data-testid="`checklist-sla-${item.id}`"
    :data-sla="view.tone"
  >
    <CircleAlert v-if="view.tone === 'error'" key="late" class="size-3" aria-hidden="true"/>
    <AlarmClock v-else key="clock" class="size-3" aria-hidden="true"/>
    {{ text }}
  </span>
</template>
