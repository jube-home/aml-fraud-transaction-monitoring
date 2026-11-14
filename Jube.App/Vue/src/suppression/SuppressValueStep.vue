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
import type {GridActionEvent, GridColumn} from '../components/dataGridTypes'
import {useFormat} from '../composables/useFormat'
import {isWide, isolate, laneNames, otherKeys, widerKnown, widerNumber} from './state'
import type {CandidateValue} from './types'

const props = defineProps<{
  values: CandidateValue[]
  loading: boolean
  failed: boolean
  overrideKey: string
}>()

const emit = defineEmits<{ select: [value: string] }>()

const {t, locale} = useI18n()
const {date} = useFormat()

const columns = computed<GridColumn[]>(() => [
  {
    field: 'value',
    title: isolate(props.overrideKey),
    minWidth: 160,
    mono: true,
    format: (value) => isolate(value == null ? '' : String(value)),
  },
  {field: 'count', title: t('suppression.value.inCase'), width: 130, align: 'end', mono: true},
  {
    field: 'firstSeenDate',
    title: t('suppression.value.first'),
    width: 130,
    mono: true,
    format: (value) => (value ? date(new Date(String(value))) : ''),
  },
  {
    field: 'lastSeenDate',
    title: t('suppression.value.last'),
    width: 130,
    mono: true,
    format: (value) => (value ? date(new Date(String(value))) : ''),
  },
  {
    field: 'merged',
    title: t('suppression.value.merged'),
    minWidth: 140,
    format: (value) => isolate(value == null ? '' : String(value)),
  },
  {
    field: 'reach',
    title: t('suppression.value.reach'),
    minWidth: 220,
    badge: (value) => {
      const row = value as { known: boolean; others: number; capped: boolean; wide: boolean }

      if (!row.known) return {label: t('suppression.value.notIndexed'), tone: 'neutral'}

      const label = t(
        'suppression.value.otherKeys',
        {n: widerNumber(row.others, row.capped, locale.value)},
        row.others,
      )

      return {
        label: row.wide ? t('suppression.value.wide', {label}) : label,
        tone: row.wide ? 'warning' : 'neutral',
      }
    },
  },
  {
    field: 'open',
    title: '',
    hiddenTitle: t('suppression.value.actions'),
    width: 110,
    actions: () => [{id: 'review', label: t('suppression.value.review')}],
  },
])

const rows = computed(() =>
  props.values.map((value) => ({
    ...value,
    id: value.value,
    merged: laneNames(value),
    reach: {
      known: widerKnown(value),
      others: otherKeys(value),
      capped: value.widerCapped,
      wide: isWide(value),
    },
  })),
)

function act(event: GridActionEvent): void {
  emit('select', (event.row as CandidateValue).value)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-2" data-testid="suppress-values">
    <p class="text-sm">{{ t('suppression.value.help', {key: isolate(overrideKey)}) }}</p>
    <div v-if="loading" key="loading" class="skeleton h-40 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <p v-else-if="failed" key="failed" role="alert" class="text-sm text-error">
      {{ t('suppression.value.failed') }}
    </p>
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :empty-text="t('suppression.value.empty')"
      :label="t('suppression.value.label')"
      row-key="value"
      auto-height
      @action="act"
    />
  </div>
</template>
