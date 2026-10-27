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
import { usePagerLabels } from '../composables/usePagerLabels'
import type { FormModel, RecordValues } from '../formModel/types'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridColumn, GridExpandEvent } from './dataGridTypes'

const props = defineProps<{
  model: FormModel
  rows: RecordValues[]
  canOpen: boolean
  filterText?: string
  expandable?: boolean
  autoHeight?: boolean
}>()

const emit = defineEmits<{
  open: [id: number]
  visible: [count: number]
  expand: [event: GridExpandEvent]
}>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const pagerLabels = usePagerLabels()

const filterField = computed(
  () =>
    props.model.keys.naturalKey ??
    props.model.listColumns.find((column) => !column.hidden)?.field ??
    props.model.keys.id,
)

const direction = computed(() => directionFor(locale.value))

const columns = computed<GridColumn[]>(() => [
  ...props.model.listColumns
    .filter((column) => !column.hidden)
    .map((column): GridColumn => {
      const field = props.model.fields.find((candidate) => candidate.name === column.field)
      const type = field?.type

      return {
        field: column.field,
        title: column.title,
        minWidth: 140,
        mono: type === 'integer' || type === 'number' || type === 'datetime' || type === 'guid',
        align: type === 'integer' || type === 'number' ? 'end' : undefined,
        type:
          type === 'integer' || type === 'number'
            ? 'number'
            : type === 'datetime'
              ? 'date'
              : 'text',
        badge:
          type === 'boolean'
            ? (value) =>
                value === true
                  ? { label: t('crud.yes'), tone: 'success' }
                  : { label: t('crud.no'), tone: 'neutral' }
            : undefined,
        format: type === 'datetime' ? (value) => (value ? dateTime(String(value)) : '') : undefined,
      }
    }),
  ...(props.canOpen
    ? [
        {
          field: '',
          title: t('crud.actions'),
          width: 120,
          pinned: true,
          actions: () => [{ id: 'open', label: t('crud.open') }],
        } satisfies GridColumn,
      ]
    : []),
])

function onAction(event: GridActionEvent): void {
  if (event.id !== 'open') return

  emit('open', (event.row as RecordValues)[props.model.keys.id] as number)
}
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    :direction="direction"
    :filter-field="filterField"
    :filter-text="filterText"
    :page-size="25"
    :pager-labels="pagerLabels"
    :empty-text="t('crud.empty')"
    :label="t('crud.list', { area: model.area })"
    :expandable="expandable"
    :auto-height="autoHeight"
    :expand-labels="{ expand: t('hierarchy.expandRow'), collapse: t('hierarchy.collapseRow') }"
    :row-key="model.keys.id"
    @action="onAction"
    @expand="(event) => emit('expand', event)"
    @visible-rows="(count) => emit('visible', count)"
  />
</template>
