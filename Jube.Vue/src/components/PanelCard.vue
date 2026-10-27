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
import { useSlots } from 'vue'
import FieldHelp from './FieldHelp.vue'

defineProps<{
  title: string
  step?: number
  flush?: boolean
  compact?: boolean
  bodyless?: boolean
  hint?: string
  fill?: boolean
  level?: 2 | 3
}>()

const slots = useSlots()
</script>

<template>
  <section :class="['card card-border border-base-300 bg-base-100', fill ? 'min-h-0' : '']">
    <header
      :class="[
        'flex flex-wrap items-center gap-3 border-t-2 border-t-accent bg-base-200',
        bodyless ? 'rounded-box' : 'rounded-t-box border-b border-b-base-300',
        compact ? 'px-4 py-2' : 'px-5 py-3',
      ]"
    >
      <span
        v-if="step !== undefined"
        key="step"
        aria-hidden="true"
        class="grid size-6 shrink-0 place-items-center rounded-full bg-primary font-mono text-xs text-primary-content"
        >{{ step }}</span
      >
      <span
        v-else-if="slots.icon"
        key="icon"
        aria-hidden="true"
        class="grid size-7 shrink-0 place-items-center rounded-field bg-primary/10 text-primary"
      >
        <slot name="icon" />
      </span>
      <h2
        v-if="level === 2"
        :class="['font-semibold text-primary', compact ? 'text-sm' : 'text-base']"
      >
        {{ title }}
      </h2>
      <h3 v-else :class="['font-semibold text-primary', compact ? 'text-sm' : 'text-base']">
        {{ title }}
      </h3>
      <FieldHelp v-if="hint" :label="title" :text="hint" />
      <slot name="actions" />
    </header>
    <div
      v-if="!bodyless"
      :class="[
        flush ? 'p-2' : compact ? 'flex flex-col gap-4 p-4' : 'flex flex-col gap-4 p-5',
        fill ? 'min-h-0 flex-1' : '',
      ]"
    >
      <slot />
    </div>
  </section>
</template>
