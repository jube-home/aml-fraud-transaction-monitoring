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
import DataGrid from '../components/DataGrid.vue'
import type {GridColumn} from '../components/dataGridTypes'
import {useFormat} from '../composables/useFormat'
import {tickCell} from './cells'
import {isolate} from './state'
import type {CandidateTransaction} from './types'

const props = defineProps<{
  rows: CandidateTransaction[]
  ticked: number[]
  tickable: boolean
  loading: boolean
  failed: boolean
}>()

const emit = defineEmits<{ toggle: [archiveId: number, on: boolean]; all: []; none: [] }>()

const {t} = useI18n()
const {dateTime} = useFormat()

const columns = computed<GridColumn[]>(() => {
  const list: GridColumn[] = []

  if (props.tickable) {
    list.push({
      field: 'ticked',
      title: t('suppression.evidence.tick'),
      width: 80,
      draw: (row) =>
        tickCell(
          props.ticked.includes((row as CandidateTransaction).archiveId),
          t('suppression.evidence.tickRow'),
          (on) => emit('toggle', (row as CandidateTransaction).archiveId, on),
        ),
    })
  }

  list.push(
    {
      field: 'referenceDate',
      title: t('suppression.evidence.when'),
      width: 190,
      mono: true,
      format: (value) => (value ? dateTime(new Date(String(value))) : ''),
    },
    {
      field: 'responseElevation',
      title: t('suppression.evidence.elevation'),
      width: 110,
      align: 'end',
      mono: true,
    },
    {field: 'activations', title: t('suppression.evidence.fired'), minWidth: 220},
    {
      field: 'laneKey',
      title: t('suppression.evidence.lane'),
      width: 150,
      format: (value) => isolate(value == null ? '' : String(value)),
    },
  )

  return list
})

const rows = computed(() => props.rows.map((row) => ({...row, id: row.archiveId})))
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="suppress-evidence">
    <div v-if="tickable" key="actions" class="flex flex-wrap items-center gap-2 text-sm">
      <span data-testid="suppress-evidence-count">
        {{ t('suppression.evidence.ticked', {n: ticked.length, total: rows.length}) }}
      </span>
      <button
        type="button"
        class="btn btn-xs"
        data-testid="suppress-evidence-all"
        @click="emit('all')"
      >
        {{ t('suppression.evidence.all') }}
      </button>
      <button
        type="button"
        class="btn btn-xs"
        data-testid="suppress-evidence-none"
        @click="emit('none')"
      >
        {{ t('suppression.evidence.none') }}
      </button>
    </div>
    <div v-if="loading" key="loading" class="skeleton h-32 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <p v-else-if="failed" key="failed" role="alert" class="text-sm text-error">
      {{ t('suppression.evidence.failed') }}
    </p>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :empty-text="t('suppression.evidence.empty')"
      :label="t('suppression.evidence.label')"
      row-key="archiveId"
      auto-height
    />
  </div>
</template>
