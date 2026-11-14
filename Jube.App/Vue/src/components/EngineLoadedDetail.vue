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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {directionFor} from '../i18n'
import {useGridExpandLabels} from '../composables/useGridExpandLabels'
import type {EngineLoaded} from '../integrity/types'
import {entityKindLabel} from '../integrity/labels'
import DataGrid from './DataGrid.vue'
import type {GridColumn} from './dataGridTypes'
import EngineEntitiesDetail from './EngineEntitiesDetail.vue'

const props = defineProps<{ loaded: EngineLoaded[] }>()

const {t, te, locale} = useI18n()
const expandLabels = useGridExpandLabels()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() => props.loaded.map((entry) => ({...entry, id: entry.kind})))

const columns = computed<GridColumn[]>(() => [
  {
    field: 'kind',
    title: t('integrity.engine.kind'),
    minWidth: 200,
    format: (value) => entityKindLabel(t, te, String(value)),
  },
  {field: 'count', title: t('integrity.engine.count'), align: 'end', mono: true, width: 120},
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
      auto-height
      :expand-labels="expandLabels"
    >
      <template #summary="{ id: id }">
        <EngineEntitiesDetail :entities="rows.find((row) => row.id === id)?.entities ?? []"/>
      </template>
    </DataGrid>
  </div>
</template>
