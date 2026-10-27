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
import { directionFor } from '../i18n'
import { dependentRows, useRows } from '../ruleTools/model'
import { ruleToolsApi } from '../ruleTools/api'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number; ruleType: string; ruleId: number | null }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))
const saved = computed(() => props.ruleId !== null)

const uses = useQuery({
  queryKey: computed(() =>
    queryKeys.ruleDependencies(props.modelId, props.ruleType, props.ruleId ?? 0),
  ),
  queryFn: () => ruleToolsApi.dependencies(props.modelId, props.ruleType, props.ruleId!),
  enabled: saved,
  retry: false,
})

const usedBy = useQuery({
  queryKey: computed(() =>
    queryKeys.ruleDependents(props.modelId, props.ruleType, props.ruleId ?? 0),
  ),
  queryFn: () => ruleToolsApi.dependents(props.modelId, props.ruleType, props.ruleId!),
  enabled: saved,
  retry: false,
})

const useGrid = computed(() => useRows(uses.data.value?.dependencies ?? []))
const usedByGrid = computed(() => dependentRows(usedBy.data.value?.dependents ?? []))

const useColumns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('ruleTools.deps.name'), minWidth: 180 },
  {
    field: 'entity',
    title: t('ruleTools.deps.entity'),
    minWidth: 220,
    format: (value) => (value ? String(value) : t('ruleTools.deps.doesNotExist')),
  },
  { field: 'how', title: t('ruleTools.deps.how'), width: 160 },
  { field: 'line', title: t('ruleTools.deps.line'), align: 'end', mono: true, width: 80 },
])

const usedByColumns = computed<GridColumn[]>(() => [
  { field: 'entity', title: t('ruleTools.deps.entity'), minWidth: 220 },
  { field: 'through', title: t('ruleTools.deps.through'), minWidth: 200 },
  { field: 'depth', title: t('ruleTools.deps.depth'), align: 'end', mono: true, width: 90 },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="rule-dependencies">
    <p v-if="!saved" key="unsaved" class="text-sm text-secondary">
      {{ t('ruleTools.deps.saveFirst') }}
    </p>
    <ProblemAlert
      v-else-if="uses.isError.value || usedBy.isError.value"
      key="failed"
      :reasons="[t('ruleTools.deps.failed')]"
    />
    <template v-else>
      <section class="flex flex-col gap-1">
        <h3 class="text-sm font-semibold">{{ t('ruleTools.deps.uses') }}</h3>
        <DataGrid
          :columns="useColumns"
          :rows="useGrid"
          :direction="direction"
          :empty-text="t('ruleTools.deps.nothing')"
          :label="t('ruleTools.deps.uses')"
          auto-height
        />
      </section>
      <section class="flex flex-col gap-1">
        <h3 class="text-sm font-semibold">{{ t('ruleTools.deps.usedBy') }}</h3>
        <DataGrid
          :columns="usedByColumns"
          :rows="usedByGrid"
          :direction="direction"
          :empty-text="t('ruleTools.deps.nothingUses')"
          :label="t('ruleTools.deps.usedBy')"
          auto-height
        />
      </section>
    </template>
  </div>
</template>
