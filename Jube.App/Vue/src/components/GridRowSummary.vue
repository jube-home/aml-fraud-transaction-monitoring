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
import {useI18n} from 'vue-i18n'
import type {GridSummaryFact, GridSummaryStat} from './gridRowSummaryTypes'

withDefaults(
  defineProps<{
    stats?: GridSummaryStat[]
    facts?: GridSummaryFact[]
    loading?: boolean
    failed?: boolean
    testid?: string
  }>(),
  {stats: () => [], facts: () => [], loading: false, failed: false, testid: 'grid-summary'},
)

const {t} = useI18n()
</script>

<template>
  <div class="flex flex-col gap-3 text-sm" :data-testid="testid">
    <div v-if="loading" key="loading" class="skeleton h-16 w-full"></div>
    <p v-else-if="failed" key="failed" role="alert">{{ t('grid.summaryFailed') }}</p>
    <template v-else>
      <div
        v-if="stats.length > 0"
        key="stats"
        class="stats stats-vertical w-full border border-base-300 sm:stats-horizontal"
      >
        <div v-for="stat in stats" :key="stat.label" class="stat px-4 py-2">
          <div class="stat-title">{{ stat.label }}</div>
          <div
            class="stat-value font-mono text-xl tabular-nums"
            :class="stat.tone === 'error' ? 'text-error' : ''"
          >
            {{ stat.value }}
          </div>
          <div v-if="stat.hint" key="hint" class="stat-desc">{{ stat.hint }}</div>
        </div>
      </div>
      <dl
        v-if="facts.length > 0"
        key="facts"
        class="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1"
      >
        <template v-for="fact in facts" :key="fact.label">
          <dt class="text-secondary">{{ fact.label }}</dt>
          <dd dir="auto" :class="fact.mono ? 'font-mono' : ''">{{ fact.value }}</dd>
        </template>
      </dl>
      <slot></slot>
    </template>
  </div>
</template>
