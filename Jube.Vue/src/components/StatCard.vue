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
import FieldHelp from './FieldHelp.vue'

defineProps<{ title: string; valueTestid?: string; small?: boolean; help?: string }>()
</script>

<template>
  <div class="stats h-full min-w-0 overflow-visible border border-base-300 bg-base-100">
    <div class="stat gap-1 px-4 py-3">
      <div class="stat-title flex items-center gap-1">
        {{ title }}
        <FieldHelp v-if="help" :label="title" :text="help" />
      </div>
      <div
        class="stat-value font-mono tabular-nums"
        :class="small ? 'text-xl' : 'text-3xl'"
        :data-testid="valueTestid"
      >
        <slot />
      </div>
      <slot name="more" />
    </div>
  </div>
</template>
