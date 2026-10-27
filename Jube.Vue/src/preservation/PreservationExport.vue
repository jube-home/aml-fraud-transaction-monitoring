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
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import DownloadButton from '../components/DownloadButton.vue'
import { useFormat, intlTag } from '../composables/useFormat'
import { useToasts } from '../stores/toasts'
import { preservationApi } from './api'
import { formatBytes, sectionOf, selectedBytes } from './state'
import type { MoveOptions, OptionKey } from './types'

const options = defineModel<MoveOptions>({ required: true })

defineProps<{ acceptable: boolean }>()

const { t, locale } = useI18n()
const format = useFormat()
const toasts = useToasts()

const summaryQuery = useQuery({
  queryKey: queryKeys.preservationSummary(),
  queryFn: () => preservationApi.summary(),
  staleTime: 60_000,
})

const tag = computed(() => intlTag(locale.value))

const flags = computed<{ key: OptionKey; label: string; hint: string }[]>(() => [
  {
    key: 'visualisations',
    label: t('preservation.flag.visualisations'),
    hint: t('preservation.flag.visualisationsHint'),
  },
  { key: 'roles', label: t('preservation.flag.roles'), hint: t('preservation.flag.rolesHint') },
  { key: 'lists', label: t('preservation.flag.lists'), hint: t('preservation.flag.listsHint') },
  {
    key: 'dictionaries',
    label: t('preservation.flag.dictionaries'),
    hint: t('preservation.flag.dictionariesHint'),
  },
  {
    key: 'exhaustive',
    label: t('preservation.flag.exhaustive'),
    hint: t('preservation.flag.exhaustiveHint'),
  },
  {
    key: 'askJoobyAnalytics',
    label: t('preservation.flag.askJoobyAnalytics'),
    hint: t('preservation.flag.askJoobyAnalyticsHint'),
  },
])

const always = computed(() => [
  { key: 'models', label: t('preservation.flag.models') },
  { key: 'approvals', label: t('preservation.flag.approvals') },
])

const total = computed(() =>
  formatBytes(selectedBytes(summaryQuery.data.value, options.value), tag.value),
)

function describe(key: string): string {
  const section = sectionOf(summaryQuery.data.value, key)

  return section
    ? t('preservation.summary.line', {
        count: format.number(section.count),
        rows: format.number(section.rows),
      })
    : ''
}

function size(key: string): string {
  const section = sectionOf(summaryQuery.data.value, key)

  return section ? formatBytes(section.bytes, tag.value) : ''
}

function setFlag(key: OptionKey, event: Event): void {
  options.value = { ...options.value, [key]: (event.target as HTMLInputElement).checked }
}

function download() {
  return preservationApi.exportPackage(options.value)
}

function failed(): void {
  toasts.error(t('preservation.export.failed'))
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-export">
    <p class="text-sm text-secondary">{{ t('preservation.export.intro') }}</p>

    <div
      v-if="summaryQuery.isPending.value"
      key="loading"
      class="skeleton h-32 w-full"
      data-testid="export-loading"
    ></div>

    <p
      v-else-if="summaryQuery.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="export-summary-failed"
    >
      {{ t('preservation.export.summaryFailed') }}
    </p>

    <ul
      v-else
      key="flags"
      class="flex flex-col divide-y divide-base-300 rounded-box border border-base-300"
    >
      <li
        v-for="item in always"
        :key="item.key"
        class="flex flex-wrap items-center gap-3 px-4 py-3"
        :data-testid="`flag-${item.key}`"
      >
        <span class="min-w-48 flex-1 text-sm font-medium">{{ item.label }}</span>
        <span class="badge badge-sm badge-soft">{{ t('preservation.flag.always') }}</span>
        <span class="text-xs text-secondary">{{ describe(item.key) }}</span>
        <span class="badge badge-sm font-mono">{{ size(item.key) }}</span>
      </li>
      <li
        v-for="flag in flags"
        :key="flag.key"
        class="flex flex-wrap items-center gap-3 px-4 py-3"
        :data-testid="`flag-${flag.key}`"
      >
        <label class="flex min-w-48 flex-1 items-center gap-3 text-sm font-medium">
          <input
            type="checkbox"
            class="toggle toggle-sm toggle-primary"
            :checked="options[flag.key]"
            :data-testid="`flag-${flag.key}-toggle`"
            @change="setFlag(flag.key, $event)"
          />
          <span class="flex flex-col">
            {{ flag.label }}
            <span class="text-xs font-normal text-secondary">{{ flag.hint }}</span>
          </span>
        </label>
        <span class="text-xs text-secondary" :data-testid="`flag-${flag.key}-count`">{{
          describe(flag.key)
        }}</span>
        <span class="badge badge-sm font-mono" :data-testid="`flag-${flag.key}-size`">{{
          size(flag.key)
        }}</span>
      </li>
    </ul>

    <div class="flex flex-wrap items-center gap-3">
      <DownloadButton
        :label="t('preservation.export.download')"
        :fetcher="download"
        fallback-name="export.jemp"
        :disabled="summaryQuery.isPending.value || !acceptable"
        @failed="failed"
      />
      <span class="text-sm text-secondary" data-testid="export-total">
        {{ t('preservation.export.total', { size: total }) }}
      </span>
    </div>
  </div>
</template>
