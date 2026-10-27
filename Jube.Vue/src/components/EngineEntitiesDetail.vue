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
import type { EngineLoadedEntity } from '../integrity/types'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

const props = defineProps<{ entities: EngineLoadedEntity[] }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() =>
  props.entities.map((entity, index) => ({ ...entity, key: `${entity.name}|${index}` })),
)

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('integrity.engine.name'), minWidth: 200 },
  { field: 'id', title: t('integrity.engine.id'), align: 'end', mono: true, width: 100 },
  {
    field: 'guid',
    title: t('integrity.engine.guid'),
    mono: true,
    minWidth: 300,
    format: (value) => (value ? String(value) : t('integrity.engine.notInDatabase')),
  },
])
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    :direction="direction"
    :empty-text="t('integrity.none')"
    :label="t('integrity.engine.entities')"
    row-key="key"
    auto-height
  />
</template>
