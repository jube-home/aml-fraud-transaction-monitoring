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
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import DataGrid from '../components/DataGrid.vue'
import JubeChart from '../components/JubeChart.vue'
import JubeMarkup from './JubeMarkup.vue'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { adoptStylesheet } from './chartStylesheet'
import { gridColumns, gridRows } from './state'
import { cardState, markupKind, type DatasourceCard } from './types'

const props = defineProps<{ card: DatasourceCard | undefined; loading?: boolean }>()

const { t, locale } = useI18n()
const format = useFormat()
const pagerLabels = usePagerLabels()

const columns = computed(() =>
  props.card?.grid
    ? gridColumns(props.card.grid, {
        yes: t('datasource.card.yes'),
        no: t('datasource.card.no'),
        number: format.number,
        dateTime: format.dateTime,
      })
    : [],
)

const rows = computed(() => (props.card?.grid ? gridRows(props.card.grid) : []))

const direction = computed(() => (locale.value === 'ar' ? 'rtl' : 'ltr'))

watch(
  () => props.card?.style,
  (style) => {
    if (style) adoptStylesheet(style)
  },
  { immediate: true },
)
</script>

<template>
  <div
    class="flex flex-col gap-3"
    data-testid="datasource-card"
    :aria-busy="loading ? 'true' : 'false'"
  >
    <div
      v-if="!card"
      key="skeleton"
      class="skeleton h-48 w-full"
      data-testid="datasource-card-loading"
    ></div>

    <template v-else>
      <p class="flex flex-wrap items-center gap-2" role="status">
        <span
          v-if="card.stateId === cardState.ready"
          class="badge badge-sm badge-success font-mono"
          >{{ t('datasource.card.ready') }}</span
        >
        <span v-else-if="card.stateId === cardState.empty" class="badge badge-sm font-mono">{{
          t('datasource.card.empty')
        }}</span>
        <span v-else class="badge badge-sm badge-error font-mono">{{
          t('datasource.card.error')
        }}</span>
        <span class="badge badge-sm badge-soft font-mono" data-testid="datasource-card-rows">{{
          t('datasource.card.rows', { count: format.number(card.rowCount) })
        }}</span>
        <span class="badge badge-sm badge-soft font-mono">{{
          t('datasource.card.milliseconds', { count: format.number(card.executionMilliseconds) })
        }}</span>
        <span
          v-if="card.truncated"
          key="truncated"
          class="badge badge-sm badge-warning font-mono"
          data-testid="datasource-card-truncated"
          >{{ t('datasource.card.truncated') }}</span
        >
      </p>

      <p
        v-if="card.stateId !== cardState.ready && card.message"
        key="message"
        role="alert"
        :class="['alert text-sm', card.stateId === cardState.error ? 'alert-error' : 'alert-info']"
        data-testid="datasource-card-message"
      >
        {{ card.message }}
      </p>

      <JubeChart
        v-if="card.display && card.display.markupKindId === markupKind.svg"
        key="chart"
        :svg="card.display.markup"
        :label="t('datasource.card.chartLabel', { name: card.name })"
      />
      <JubeMarkup
        v-else-if="card.display"
        key="markup"
        :html="card.display.markup"
        :label="t('datasource.card.templateLabel', { name: card.name })"
      />

      <DataGrid
        v-if="card.grid"
        key="grid"
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('datasource.card.noRows')"
        :label="t('datasource.card.gridLabel', { name: card.name })"
        :page-size="10"
        :pager-labels="pagerLabels"
        auto-height
      />
    </template>
  </div>
</template>
