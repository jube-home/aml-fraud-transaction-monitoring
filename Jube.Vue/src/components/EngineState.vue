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
import { useFormat } from '../composables/useFormat'
import { useExpandHosts } from '../composables/useExpandHosts'
import { integrityApi } from '../integrity/api'
import { engineModelState, engineRows } from '../integrity/model'
import DataGrid from './DataGrid.vue'
import type { GridBadge, GridColumn } from './dataGridTypes'
import EngineLoadedDetail from './EngineLoadedDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))

const state = useQuery({
  queryKey: computed(() => queryKeys.integrityEngine(props.modelId)),
  queryFn: () => integrityApi.engine(props.modelId),
  retry: false,
})

const rows = computed(() => (state.data.value ? engineRows(state.data.value) : []))

const when = (value: unknown) => (value ? dateTime(new Date(value as string)) : '')

function stateBadge(value: unknown): GridBadge {
  const model = engineModelState({ started: value as boolean | null })

  return model === 'started'
    ? { label: t('integrity.engine.state.started'), tone: 'success' }
    : model === 'loaded'
      ? { label: t('integrity.engine.state.loaded'), tone: 'warning' }
      : { label: t('integrity.engine.state.notLoaded'), tone: 'neutral' }
}

const columns = computed<GridColumn[]>(() => [
  { field: 'instance', title: t('integrity.engine.col.instance'), minWidth: 180 },
  {
    field: 'heartbeatDate',
    title: t('integrity.engine.col.reported'),
    mono: true,
    width: 190,
    format: when,
  },
  {
    field: 'synchronisedDate',
    title: t('integrity.engine.col.synchronised'),
    mono: true,
    width: 190,
    format: when,
  },
  { field: 'started', title: t('integrity.engine.col.model'), width: 200, badge: stateBadge },
  {
    field: 'loadedTotal',
    title: t('integrity.engine.col.loaded'),
    align: 'end',
    mono: true,
    width: 150,
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="engine-state">
    <ProblemAlert
      v-if="state.isError.value"
      key="failed"
      :reasons="[t('integrity.engine.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('integrity.engine.empty')"
      :label="t('integrity.engine.label')"
      row-key="id"
      expandable
      auto-height
      :expand-labels="{
        expand: t('integrity.engine.showLoaded'),
        collapse: t('integrity.engine.hideLoaded'),
      }"
      @expand="onExpand"
    />
    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <EngineLoadedDetail :loaded="rows.find((row) => row.id === id)?.loaded ?? []" />
      </Teleport>
    </template>
  </div>
</template>
