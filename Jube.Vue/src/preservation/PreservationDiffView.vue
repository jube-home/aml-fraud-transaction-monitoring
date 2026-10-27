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
import DataGrid from '../components/DataGrid.vue'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { entryColumns, entryRows, sectionColumns } from './state'
import type { Diff } from './types'

const props = defineProps<{ diff: Diff; caption?: string }>()

const { t, locale } = useI18n()
const format = useFormat()
const basePagerLabels = usePagerLabels()

const pagerLabels = computed(() => ({
  ...basePagerLabels.value,
  navigation: `${basePagerLabels.value.navigation}: ${props.caption ?? t('preservation.preview.entries')}`,
}))

const direction = computed(() => (locale.value === 'ar' ? 'rtl' : 'ltr'))

const figures = computed(() => [
  { key: 'added', label: t('preservation.preview.added'), value: props.diff.added },
  { key: 'removed', label: t('preservation.preview.removed'), value: props.diff.removed },
  { key: 'changed', label: t('preservation.preview.changed'), value: props.diff.changed },
  { key: 'unchanged', label: t('preservation.preview.unchanged'), value: props.diff.unchanged },
])

const sectionColumnsList = computed(() =>
  sectionColumns({
    section: t('preservation.preview.section'),
    added: t('preservation.preview.added'),
    removed: t('preservation.preview.removed'),
    changed: t('preservation.preview.changed'),
    unchanged: t('preservation.preview.unchanged'),
  }),
)

const entryColumnsList = computed(() =>
  entryColumns({
    section: t('preservation.preview.section'),
    path: t('preservation.preview.path'),
    kind: t('preservation.preview.kind'),
    detail: t('preservation.preview.detail'),
    added: t('preservation.preview.added'),
    removed: t('preservation.preview.removed'),
    changed: t('preservation.preview.changed'),
  }),
)

const rows = computed(() => entryRows(props.diff))
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-diff">
    <p v-if="diff.identical" key="identical" class="text-sm" data-testid="preview-identical">
      {{ t('preservation.preview.identical') }}
    </p>

    <dl class="flex flex-wrap gap-6" :aria-label="t('preservation.preview.counts')">
      <div v-for="figure in figures" :key="figure.key" class="flex flex-col">
        <dt class="label text-xs">{{ figure.label }}</dt>
        <dd class="font-mono text-2xl font-semibold" :data-testid="`preview-${figure.key}`">
          {{ format.number(figure.value) }}
        </dd>
      </div>
    </dl>

    <div v-if="diff.sections.length > 0" key="sections" class="flex flex-col gap-2">
      <h3 class="text-sm font-semibold text-primary">{{ t('preservation.preview.bySection') }}</h3>
      <DataGrid
        :columns="sectionColumnsList"
        :rows="diff.sections"
        :direction="direction"
        :empty-text="t('preservation.preview.noSections')"
        :label="t('preservation.preview.bySection')"
        auto-height
      />
    </div>

    <div v-if="rows.length > 0" key="entries" class="flex flex-col gap-2">
      <h3 class="text-sm font-semibold text-primary">{{ t('preservation.preview.entries') }}</h3>
      <p
        v-if="diff.truncated"
        key="truncated"
        class="text-sm text-secondary"
        data-testid="preview-truncated"
      >
        {{ t('preservation.preview.truncated') }}
      </p>
      <DataGrid
        :columns="entryColumnsList"
        :rows="rows"
        :direction="direction"
        :empty-text="t('preservation.preview.noEntries')"
        :label="t('preservation.preview.entries')"
        :page-size="25"
        :pager-labels="pagerLabels"
        auto-height
      />
    </div>

    <p v-if="diff.referencesIgnored > 0" key="references" class="text-xs text-secondary">
      {{ t('preservation.preview.references', { count: diff.referencesIgnored }) }}
    </p>
  </div>
</template>
