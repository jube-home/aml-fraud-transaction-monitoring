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
import { ChevronRight } from 'lucide-vue-next'

withDefaults(
  defineProps<{
    id: string
    title: string
    hint?: string
    pill?: string
    icon: Component
    openLabel: string
  }>(),
  { hint: '', pill: '' },
)

const emit = defineEmits<{ open: [] }>()
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" :data-testid="`perf-card-${id}`">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h3 class="card-title text-base">
          <component :is="icon" class="size-5 shrink-0 text-secondary" aria-hidden="true" />
          {{ title }}
        </h3>
        <span
          v-if="pill"
          key="pill"
          class="badge badge-sm shrink-0 font-mono"
          :data-testid="`perf-pill-${id}`"
          >{{ pill }}</span
        >
      </div>
      <p v-if="hint" key="hint" class="text-sm text-secondary">{{ hint }}</p>
      <div class="card-actions mt-auto justify-end">
        <button
          type="button"
          class="btn btn-sm"
          :data-testid="`perf-open-${id}`"
          @click="emit('open')"
        >
          {{ openLabel }}
          <ChevronRight aria-hidden="true" class="size-3.5 rtl:rotate-180" />
        </button>
      </div>
    </div>
  </div>
</template>
