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
import type {Component} from 'vue'
import {useI18n} from 'vue-i18n'
import {ChevronRight, Plus} from 'lucide-vue-next'

withDefaults(
  defineProps<{
    id: string
    label: string
    icon?: Component
    meta?: string
    count?: number
    pending?: boolean
    failed?: string
    pill?: string
    tone?: 'neutral' | 'success' | 'warning' | 'error'
    openable?: boolean
    addLabel?: string
    order?: number
  }>(),
  {openable: true},
)

const emit = defineEmits<{ open: []; add: [] }>()

const {t} = useI18n()

const tones = {
  neutral: 'badge-neutral',
  success: 'badge-success',
  warning: 'badge-warning',
  error: 'badge-error',
} as const
</script>

<template>
  <div
    class="relative flex min-w-0 items-center gap-2.5 rounded-field border border-base-300 bg-base-100 py-2 ps-3 pe-2 hover:border-base-content/40"
    :style="order === undefined ? undefined : { order }"
    :data-testid="`hub-${id}`"
  >
    <component :is="icon" v-if="icon" class="size-5 shrink-0 text-secondary" aria-hidden="true"/>
    <button
      v-if="openable"
      key="open"
      type="button"
      class="min-w-0 flex-1 text-start outline-none after:absolute after:inset-0 after:rounded-field focus-visible:after:outline-2 focus-visible:after:outline-offset-2"
      :data-testid="`hub-open-${id}`"
      @click="emit('open')"
    >
      <span class="block truncate text-sm font-medium" :title="label">{{ label }}</span>
      <span class="block min-h-4 truncate text-xs text-secondary" :title="meta">{{ meta }}</span>
    </button>
    <div v-else key="static" class="min-w-0 flex-1">
      <span class="block truncate text-sm font-medium">{{ label }}</span>
      <span class="block min-h-4 truncate text-xs text-secondary">{{ meta }}</span>
    </div>
    <span
      v-if="pending"
      key="pending"
      class="skeleton h-5 w-8 shrink-0"
      role="status"
      :aria-label="t('crud.loading')"
    ></span>
    <span v-else-if="failed" key="failed" class="badge badge-sm badge-error shrink-0 font-mono">{{
        failed
      }}</span>
    <span
      v-else-if="count !== undefined"
      key="count"
      class="badge badge-sm badge-ghost shrink-0 font-mono tabular-nums"
      :data-testid="`hub-total-${id}`"
    >{{ count }}</span
    >
    <span
      v-if="pill"
      key="pill"
      class="badge badge-sm shrink-0 font-mono"
      :class="tones[tone ?? 'neutral']"
      :data-testid="`hub-pill-${id}`"
    >{{ pill }}</span
    >
    <slot/>
    <button
      v-if="addLabel"
      key="add"
      type="button"
      class="btn btn-xs btn-square btn-ghost relative z-10 shrink-0"
      :aria-label="addLabel"
      :data-testid="`hub-add-${id}`"
      @click="emit('add')"
    >
      <Plus aria-hidden="true" class="size-4"/>
    </button>
    <ChevronRight
      v-if="openable"
      class="size-4 shrink-0 text-secondary rtl:rotate-180"
      aria-hidden="true"
    />
  </div>
</template>
