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
import { approvalKindOf, type PendingEntity } from '../integrity/model'
import DataGrid from './DataGrid.vue'
import type { GridAction, GridActionEvent, GridColumn } from './dataGridTypes'

const props = defineProps<{ entities: PendingEntity[]; busy?: boolean }>()

const emit = defineEmits<{ action: [id: string, entity: PendingEntity] }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() =>
  props.entities.map((entity) => ({ ...entity, id: `${entity.kind}|${entity.entityId}` })),
)

function actionsOf(row: object): GridAction[] {
  const entity = row as PendingEntity
  const actions: GridAction[] = [
    { id: 'approve', label: t('integrity.pending.approve'), tone: 'success', disabled: props.busy },
    { id: 'reject', label: t('integrity.pending.reject'), tone: 'error', disabled: props.busy },
  ]

  if (approvalKindOf(entity.kind)?.endpoint)
    actions.push({ id: 'review', label: t('integrity.pending.review') })

  if (approvalKindOf(entity.kind)?.valueKind !== undefined && entity.valueCount > 0) {
    actions.push({
      id: 'values',
      label: t('integrity.pending.approveValues'),
      disabled: props.busy,
    })
  }

  return actions
}

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('integrity.pending.col.name'), minWidth: 200 },
  {
    field: 'change',
    title: t('integrity.pending.col.change'),
    width: 190,
    format: (value) =>
      value === 'deletion' ? t('integrity.pending.deletion') : t('integrity.pending.edit'),
  },
  {
    field: 'version',
    title: t('integrity.pending.col.version'),
    align: 'end',
    mono: true,
    width: 100,
  },
  { field: 'makerUser', title: t('integrity.pending.col.madeBy'), width: 180 },
  {
    field: 'valueCount',
    title: t('integrity.pending.col.values'),
    align: 'end',
    mono: true,
    width: 100,
  },
  {
    field: 'actions',
    title: '',
    hiddenTitle: t('integrity.pending.col.actions'),
    width: 360,
    actions: actionsOf,
  },
])

function onAction(event: GridActionEvent): void {
  emit('action', event.id, event.row as PendingEntity)
}
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    :direction="direction"
    :empty-text="t('integrity.pending.empty')"
    :label="t('integrity.pending.entities')"
    auto-height
    @action="onAction"
  />
</template>
