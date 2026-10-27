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
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { useExpandHosts } from '../composables/useExpandHosts'
import type { BacktestRequest, BacktestSample } from '../ruleTools/types'
import BacktestExplain from './BacktestExplain.vue'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

const props = defineProps<{ title: string; samples: BacktestSample[]; request: BacktestRequest }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() =>
  props.samples.map((sample) => ({ ...sample, id: sample.entityAnalysisModelInstanceEntryGuid })),
)

const yes = (value: unknown) => (value ? t('ruleTools.yes') : t('ruleTools.no'))

const columns = computed<GridColumn[]>(() => [
  { field: 'entryKeyValue', title: t('ruleTools.examples.entry'), minWidth: 200 },
  {
    field: 'referenceDate',
    title: t('ruleTools.examples.referenceDate'),
    mono: true,
    width: 190,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
  { field: 'fired', title: t('ruleTools.explain.fired'), width: 90, format: yes },
  { field: 'positive', title: t('ruleTools.explain.positive'), width: 100, format: yes },
  { field: 'error', title: t('ruleTools.examples.error'), minWidth: 200 },
])
</script>

<template>
  <details
    class="collapse collapse-arrow border border-base-300 bg-base-100"
    :data-testid="`examples-${title}`"
  >
    <summary class="collapse-title text-sm font-semibold">
      {{ title }} ({{ t('ruleTools.examples.count', { count: samples.length }) }})
    </summary>
    <div class="collapse-content">
      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('ruleTools.explain.none')"
        :label="title"
        expandable
        auto-height
        :expand-labels="{
          expand: t('ruleTools.examples.explain'),
          collapse: t('ruleTools.examples.hide'),
        }"
        @expand="onExpand"
      />
      <template v-for="[id, host] in hosts" :key="String(id)">
        <Teleport :to="host">
          <BacktestExplain :request="request" :guid="String(id)" />
        </Teleport>
      </template>
    </div>
  </details>
</template>
