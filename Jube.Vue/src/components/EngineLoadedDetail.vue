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
import { useExpandHosts } from '../composables/useExpandHosts'
import type { EngineLoaded } from '../integrity/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'
import EngineEntitiesDetail from './EngineEntitiesDetail.vue'

const props = defineProps<{ loaded: EngineLoaded[] }>()

const { t, locale } = useI18n()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() => props.loaded.map((entry) => ({ ...entry, id: entry.kind })))

const columns = computed<GridColumn[]>(() => [
  { field: 'kind', title: t('integrity.engine.kind'), minWidth: 200 },
  { field: 'count', title: t('integrity.engine.count'), align: 'end', mono: true, width: 120 },
])
</script>

<template>
  <div class="flex flex-col gap-2">
    <h4 class="text-sm font-semibold">{{ t('integrity.engine.loadedFor') }}</h4>
    <DataGrid
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('integrity.none')"
      :label="t('integrity.engine.loadedFor')"
      expandable
      auto-height
      :expand-labels="{
        expand: t('integrity.engine.showEntities'),
        collapse: t('integrity.engine.hideEntities'),
      }"
      @expand="onExpand"
    />
    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <EngineEntitiesDetail :entities="rows.find((row) => row.id === id)?.entities ?? []" />
      </Teleport>
    </template>
  </div>
</template>
