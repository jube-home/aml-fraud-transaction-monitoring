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
import { provide, useId } from 'vue'
import { useI18n } from 'vue-i18n'

defineProps<{
  title: string
  hint?: string
  alwaysOn?: boolean
}>()

const { t } = useI18n()
const enabled = defineModel<boolean>({ default: false })
const panelId = useId()

provide('nestDepth', 0)
</script>

<template>
  <div class="card card-border bg-base-100 border-base-300">
    <div class="card-body gap-4">
      <div class="flex items-start justify-between gap-4">
        <div class="flex flex-col gap-1">
          <h3 class="card-title text-base">{{ title }}</h3>
          <p v-if="hint" class="text-sm text-secondary">{{ hint }}</p>
        </div>
        <label v-if="!alwaysOn" class="flex shrink-0 items-center gap-3">
          <span :class="['badge badge-sm font-mono', enabled ? 'badge-accent' : 'badge-soft']">
            {{ enabled ? t('rule.on') : t('rule.off') }}
          </span>
          <input
            v-model="enabled"
            type="checkbox"
            class="toggle toggle-primary"
            :aria-label="title"
            :aria-controls="panelId"
            :aria-expanded="enabled"
          />
        </label>
      </div>
      <div v-if="alwaysOn || enabled" :id="panelId" class="grid gap-x-6 gap-y-4 sm:grid-cols-2">
        <slot />
      </div>
    </div>
  </div>
</template>
