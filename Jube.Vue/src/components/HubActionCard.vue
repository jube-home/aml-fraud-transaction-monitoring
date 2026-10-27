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
import { ChevronRight } from 'lucide-vue-next'

defineProps<{
  id: string
  title: string
  hint: string
  pill?: string
  tone?: 'neutral' | 'success' | 'warning' | 'error'
  openLabel: string
  heading?: 'h2' | 'h4'
}>()

const emit = defineEmits<{ open: [] }>()

const tones = {
  neutral: 'badge-neutral',
  success: 'badge-success',
  warning: 'badge-warning',
  error: 'badge-error',
} as const
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" :data-testid="`hub-${id}`">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h2 v-if="heading === 'h2'" class="card-title text-base">{{ title }}</h2>
        <h4 v-else class="card-title text-base">{{ title }}</h4>
        <span
          v-if="pill"
          key="pill"
          class="badge badge-sm font-mono"
          :class="tones[tone ?? 'neutral']"
          :data-testid="`hub-pill-${id}`"
          >{{ pill }}</span
        >
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ hint }}</p>
      <div class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`hub-open-${id}`"
          @click="emit('open')"
        >
          {{ openLabel }}
          <ChevronRight aria-hidden="true" class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>
