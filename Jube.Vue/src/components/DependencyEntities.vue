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
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { useExpandHosts } from '../composables/useExpandHosts'
import { usePagerLabels } from '../composables/usePagerLabels'
import { integrityApi } from '../integrity/api'
import { dependencyRows } from '../integrity/model'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridBadge, GridColumn } from './dataGridTypes'
import DependencyLinksDetail from './DependencyLinksDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number }>()

const emit = defineEmits<{ select: [id: string] }>()

const { t, locale } = useI18n()
const { hosts, onExpand } = useExpandHosts()
const pagerLabels = usePagerLabels()
const id = useId()

const direction = computed(() => directionFor(locale.value))
const search = ref('')

const graph = useQuery({
  queryKey: computed(() => queryKeys.integrityGraph(props.modelId)),
  queryFn: () => integrityApi.graph(props.modelId),
  retry: false,
})

const rows = computed(() => (graph.data.value ? dependencyRows(graph.data.value) : []))

function missingBadge(value: unknown): GridBadge {
  return (value as number) > 0
    ? { label: String(value), tone: 'error' }
    : { label: '0', tone: 'neutral' }
}

const columns = computed<GridColumn[]>(() => [
  { field: 'kind', title: t('integrity.deps.col.kind'), width: 190 },
  { field: 'name', title: t('integrity.deps.col.entity'), minWidth: 200 },
  {
    field: 'active',
    title: t('integrity.deps.col.active'),
    width: 90,
    format: (value) => (value ? t('integrity.yes') : t('integrity.no')),
  },
  { field: 'usesCount', title: t('integrity.deps.col.uses'), align: 'end', mono: true, width: 90 },
  {
    field: 'usedByCount',
    title: t('integrity.deps.col.usedBy'),
    align: 'end',
    mono: true,
    width: 100,
  },
  {
    field: 'missingCount',
    title: t('integrity.deps.col.missing'),
    align: 'end',
    width: 140,
    badge: missingBadge,
  },
  {
    field: 'show',
    title: '',
    hiddenTitle: t('integrity.deps.col.actions'),
    width: 120,
    actions: () => [{ id: 'show', label: t('integrity.deps.show') }],
  },
])

function onAction(event: GridActionEvent): void {
  emit('select', (event.row as { id: string }).id)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="dependency-entities">
    <label class="flex flex-col gap-1" :for="id">
      <span class="label text-xs">{{ t('integrity.deps.filter') }}</span>
      <input
        :id="id"
        v-model="search"
        type="search"
        class="input input-sm w-full max-w-sm"
        data-testid="dependency-filter"
      />
    </label>

    <ProblemAlert v-if="graph.isError.value" key="failed" :reasons="[t('integrity.deps.failed')]" />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('integrity.deps.none')"
      :label="t('integrity.deps.entities')"
      row-key="id"
      filter-field="name"
      :filter-text="search"
      :page-size="25"
      :pager-labels="pagerLabels"
      expandable
      auto-height
      :expand-labels="{
        expand: t('integrity.deps.showLinks'),
        collapse: t('integrity.deps.hideLinks'),
      }"
      @expand="onExpand"
      @action="onAction"
    />

    <template v-for="[rowId, host] in hosts" :key="String(rowId)">
      <Teleport :to="host">
        <DependencyLinksDetail
          :uses="rows.find((row) => row.id === rowId)?.uses ?? []"
          :used-by="rows.find((row) => row.id === rowId)?.usedBy ?? []"
        />
      </Teleport>
    </template>
  </div>
</template>
