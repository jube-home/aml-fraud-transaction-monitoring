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
import type { InstanceRow } from '../ruleTools/model'
import DataGrid from './DataGrid.vue'
import type { GridAction, GridActionEvent, GridBadge, GridColumn, GridTone } from './dataGridTypes'

defineProps<{ rows: InstanceRow[] }>()

const emit = defineEmits<{ show: [id: number]; stop: [id: number] }>()

const { t, locale } = useI18n()
const { dateTime, date } = useFormat()

const direction = computed(() => directionFor(locale.value))

const tones: Record<string, GridTone> = {
  Succeeded: 'success',
  Failed: 'error',
  Running: 'info',
  Pending: 'neutral',
  Cancelling: 'warning',
  Cancelled: 'neutral',
}

function statusBadge(value: unknown): GridBadge {
  return { label: String(value), tone: tones[String(value)] ?? 'neutral' }
}

const percent = (value: unknown) =>
  value === null || value === undefined ? '' : `${((value as number) * 100).toFixed(1)}%`
const count = (value: unknown) =>
  value === null || value === undefined ? '' : (value as number).toLocaleString(locale.value)

const windowLabel = (_: unknown, row?: object) => {
  const request = (row as InstanceRow | undefined)?.request

  return request?.from || request?.to
    ? `${request.from ? date(request.from) : t('ruleTools.start')} – ${request.to ? date(request.to) : t('ruleTools.now')}`
    : t('ruleTools.allArchive')
}

function actionsOf(row: object): GridAction[] {
  const instance = row as InstanceRow
  const actions: GridAction[] = [{ id: 'show', label: t('ruleTools.show') }]

  if (instance.stoppable) actions.push({ id: 'stop', label: t('ruleTools.stop'), tone: 'error' })

  return actions
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'createdDate',
    title: t('ruleTools.col.submitted'),
    mono: true,
    width: 170,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
  {
    field: 'edited',
    title: t('ruleTools.col.rule'),
    width: 110,
    format: (value) => (value ? t('ruleTools.edited') : t('ruleTools.asSubmitted')),
  },
  { field: 'createdUser', title: t('ruleTools.col.by'), width: 130 },
  { field: 'status', title: t('ruleTools.col.status'), width: 130, badge: statusBadge },
  {
    field: 'progress',
    title: t('ruleTools.col.progress'),
    align: 'end',
    mono: true,
    width: 100,
    format: percent,
  },
  {
    field: 'scanned',
    title: t('ruleTools.col.read'),
    align: 'end',
    mono: true,
    width: 100,
    format: count,
  },
  {
    field: 'evaluated',
    title: t('ruleTools.col.evaluated'),
    align: 'end',
    mono: true,
    width: 110,
    format: count,
  },
  {
    field: 'fired',
    title: t('ruleTools.col.fired'),
    align: 'end',
    mono: true,
    width: 90,
    format: count,
  },
  {
    field: 'truePositives',
    title: t('ruleTools.col.tp'),
    align: 'end',
    mono: true,
    width: 80,
    format: count,
  },
  {
    field: 'falsePositives',
    title: t('ruleTools.col.fp'),
    align: 'end',
    mono: true,
    width: 80,
    format: count,
  },
  {
    field: 'falseNegatives',
    title: t('ruleTools.col.fn'),
    align: 'end',
    mono: true,
    width: 80,
    format: count,
  },
  {
    field: 'trueNegatives',
    title: t('ruleTools.col.tn'),
    align: 'end',
    mono: true,
    width: 90,
    format: count,
  },
  {
    field: 'precision',
    title: t('ruleTools.col.precision'),
    align: 'end',
    mono: true,
    width: 110,
    format: percent,
  },
  {
    field: 'recall',
    title: t('ruleTools.col.recall'),
    align: 'end',
    mono: true,
    width: 100,
    format: percent,
  },
  {
    field: 'window',
    title: t('ruleTools.col.window'),
    width: 190,
    format: (value) => windowLabel(value),
  },
  {
    field: 'actions',
    title: '',
    hiddenTitle: t('ruleTools.col.actions'),
    width: 170,
    actions: actionsOf,
  },
])

function onAction(event: GridActionEvent): void {
  const id = (event.row as InstanceRow).id

  if (event.id === 'stop') emit('stop', id)
  else emit('show', id)
}
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    :direction="direction"
    :empty-text="t('ruleTools.noBacktests')"
    :label="t('ruleTools.instances')"
    auto-height
    @action="onAction"
  />
</template>
