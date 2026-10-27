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
import { computed } from 'vue'

const props = defineProps<{ values: readonly number[]; label: string }>()

const bars = computed(() => {
  const top = Math.max(...props.values, 1)

  return props.values.map((value) => Math.max(6, Math.round((value / top) * 100)))
})
</script>

<template>
  <div class="flex h-10 w-full items-end gap-0.5" role="img" :aria-label="label">
    <span
      v-for="(height, index) in bars"
      :key="index"
      class="flex-1 rounded-xs"
      :class="index === bars.length - 1 ? 'bg-primary' : 'bg-base-300'"
      :style="`height: ${height}%`"
    ></span>
  </div>
</template>
