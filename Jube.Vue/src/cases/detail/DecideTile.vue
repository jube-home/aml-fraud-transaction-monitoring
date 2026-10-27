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
import type { Component } from 'vue'

withDefaults(
  defineProps<{
    icon: Component
    label: string
    hint?: string
    testid: string
    disabled?: boolean
    tone?: 'neutral' | 'error'
  }>(),
  { hint: '', disabled: false, tone: 'neutral' },
)

const emit = defineEmits<{ select: [] }>()
</script>

<template>
  <button
    type="button"
    class="btn btn-ghost h-auto min-h-0 justify-start gap-3 rounded-box border border-base-300 px-3 py-2 text-start font-medium"
    :class="tone === 'error' ? 'text-error' : ''"
    :disabled="disabled"
    :data-testid="testid"
    @click="emit('select')"
  >
    <span class="grid size-9 shrink-0 place-items-center rounded-field bg-base-200">
      <component :is="icon" class="size-5" aria-hidden="true" />
    </span>
    <span class="flex min-w-0 flex-col">
      <span class="truncate text-sm">{{ label }}</span>
      <span v-if="hint" key="hint" class="truncate text-xs font-normal text-base-content/70">
        {{ hint }}
      </span>
    </span>
  </button>
</template>
