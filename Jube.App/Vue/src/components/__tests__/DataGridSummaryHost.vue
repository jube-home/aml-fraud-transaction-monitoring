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
import DataGrid from '../DataGrid.vue'
import type {GridColumn} from '../dataGridTypes'

defineProps<{ columns: GridColumn[]; rows: object[] }>()
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    row-key="id"
    empty-text="Nothing here"
    label="Summary grid"
    auto-height
    :expand-labels="{ expand: 'Show summary', collapse: 'Hide summary', reset: 'Reset' }"
  >
    <template #summary="{ id, row }">
      <p data-testid="summary-body">{{ id }}:{{ (row as { name: string }).name }}</p>
    </template>
  </DataGrid>
</template>
