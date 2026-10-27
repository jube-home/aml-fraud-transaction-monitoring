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
import DataGrid from '../../components/DataGrid.vue'
import type { GridColumn } from '../../components/dataGridTypes'
import { usePagerLabels } from '../../composables/usePagerLabels'
import { matchesFormat } from './text'
import type { CaseFieldEntry } from './types'

const props = defineProps<{ fields: CaseFieldEntry[] }>()

const { t } = useI18n()
const pagerLabels = usePagerLabels()

function paint(entry: CaseFieldEntry, row: boolean, text: string, mono: boolean): Element {
  const cell = document.createElement('span')
  const formatted = matchesFormat(entry)

  cell.textContent = text
  cell.className = mono ? 'font-mono text-sm' : 'text-sm'

  if (formatted && (entry.cellFormatForeColor || entry.cellFormatBackColor)) {
    const applies = row || mono

    if (applies && entry.cellFormatForeColor) cell.style.color = entry.cellFormatForeColor
    if (applies && entry.cellFormatBackColor) cell.style.backgroundColor = entry.cellFormatBackColor
    if (applies) cell.classList.add('rounded-sm', 'px-1')
  }

  return cell
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'name',
    title: t('caseDetail.details.field'),
    width: 260,
    draw: (row) =>
      paint(
        row as CaseFieldEntry,
        (row as CaseFieldEntry).cellFormatForeRow || (row as CaseFieldEntry).cellFormatBackRow,
        (row as CaseFieldEntry).name ?? '',
        false,
      ),
  },
  {
    field: 'value',
    title: t('caseDetail.details.value'),
    minWidth: 240,
    draw: (row) => paint(row as CaseFieldEntry, true, (row as CaseFieldEntry).value ?? '', true),
  },
])
</script>

<template>
  <section class="flex min-w-0 flex-col gap-2" data-testid="case-details">
    <h4 class="text-sm font-semibold">{{ t('caseDetail.details.title') }}</h4>
    <p v-if="props.fields.length === 0" class="text-sm text-base-content/70" role="status">
      {{ t('caseDetail.details.empty') }}
    </p>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="props.fields"
      :empty-text="t('caseDetail.details.empty')"
      :label="t('caseDetail.details.title')"
      :page-size="100"
      :pager-labels="pagerLabels"
      row-key="name"
    />
  </section>
</template>
