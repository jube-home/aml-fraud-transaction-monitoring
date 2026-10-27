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
import { directionFor } from '../i18n'
import { validationText } from '../ruleTools/model'
import { ruleToolsApi } from '../ruleTools/api'
import type { BacktestRequest } from '../ruleTools/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ request: BacktestRequest; guid: string }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))

const explanation = useQuery({
  queryKey: computed(() => ['ruleTools', 'explain', props.request, props.guid] as const),
  queryFn: () => ruleToolsApi.explain(props.request, props.guid),
  retry: false,
  staleTime: 5 * 60_000,
})

const data = computed(() => explanation.data.value)
const rows = computed(() =>
  (data.value?.values ?? []).map((value, index) => ({ ...value, id: `${value.name}|${index}` })),
)

const yes = (value: boolean) => (value ? t('ruleTools.yes') : t('ruleTools.no'))

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('ruleTools.explain.read'), minWidth: 220 },
  {
    field: 'value',
    title: t('ruleTools.explain.onThis'),
    minWidth: 260,
    format: (value) =>
      value === null || value === undefined ? t('ruleTools.explain.noValue') : String(value),
  },
])
</script>

<template>
  <div class="flex flex-col gap-2" data-testid="backtest-explain">
    <p v-if="explanation.isPending.value" key="loading" class="text-sm" role="status">
      {{ t('ruleTools.loading') }}
    </p>
    <ProblemAlert
      v-else-if="explanation.isError.value"
      key="failed"
      :reasons="[t('ruleTools.explain.failed')]"
    />
    <ProblemAlert
      v-else-if="data && !data.valid"
      key="invalid"
      :reasons="[validationText(data.errors)]"
    />
    <template v-else-if="data">
      <p class="flex flex-wrap gap-x-4 gap-y-1 text-sm">
        <span>{{ t('ruleTools.explain.passedFilter') }}: {{ yes(data.passedFilter) }}</span>
        <span>{{ t('ruleTools.explain.fired') }}: {{ yes(data.fired) }}</span>
        <span v-if="data.positive !== null"
          >{{ t('ruleTools.explain.positive') }}: {{ yes(data.positive) }}</span
        >
      </p>
      <p v-if="data.filterError" key="fe" class="text-sm text-error">
        {{ t('ruleTools.explain.filterError') }}: {{ data.filterError }}
      </p>
      <p v-if="data.ruleError" key="re" class="text-sm text-error">
        {{ t('ruleTools.explain.ruleError') }}: {{ data.ruleError }}
      </p>
      <p class="text-xs text-secondary">
        {{ t('ruleTools.explain.tags') }}:
        {{ data.tags.length > 0 ? data.tags.join(', ') : t('ruleTools.explain.none') }}
      </p>
      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('ruleTools.explain.none')"
        :label="t('ruleTools.explain.values')"
        auto-height
      />
    </template>
  </div>
</template>
