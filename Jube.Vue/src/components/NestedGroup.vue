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
import { inject, provide, useId } from 'vue'

defineProps<{
  label: string
}>()

const enabled = defineModel<boolean>({ default: false })
const panelId = useId()
const depth = inject<number>('nestDepth', 0) + 1

provide('nestDepth', depth)

const panelClass =
  depth === 1
    ? 'rounded-box border border-base-300 border-s-4 border-s-accent bg-base-200 p-4'
    : 'border-s-4 border-s-accent ps-4'
</script>

<template>
  <div class="flex flex-col gap-3 sm:col-span-2">
    <label class="label cursor-pointer justify-start gap-3">
      <input
        v-model="enabled"
        type="checkbox"
        class="toggle toggle-primary toggle-sm"
        :aria-controls="panelId"
        :aria-expanded="enabled"
      />
      <span>{{ label }}</span>
    </label>
    <div v-if="enabled" :id="panelId" :class="['grid gap-x-6 gap-y-4 sm:grid-cols-2', panelClass]">
      <slot />
    </div>
  </div>
</template>
