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
import { computed, ref, useTemplateRef } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import DataGrid from '../components/DataGrid.vue'
import type { GridBadge, GridColumn, GridSort } from '../components/dataGridTypes'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { sampleCases, type Risk } from './sampleCases'

const { t, locale } = useI18n()
const { currency, date } = useFormat()
const direction = computed(() => directionFor(locale.value))
const route = useRoute()
const router = useRouter()

const grid = useTemplateRef<{ downloadCsv: (filename: string) => void }>('grid')
const visible = ref(sampleCases.length)

const filterText = computed(() => String(route.query.q ?? ''))

const sort = computed<GridSort | undefined>(() => {
  const field = route.query.sort

  if (typeof field !== 'string') return undefined

  return { field, dir: route.query.dir === 'desc' ? 'desc' : 'asc' }
})

const columns = computed<GridColumn[]>(() => {
  void locale.value

  const risk: Record<Risk, GridBadge> = {
    low: { label: t('grid.risk.low'), tone: 'success' },
    medium: { label: t('grid.risk.medium'), tone: 'warning' },
    high: { label: t('grid.risk.high'), tone: 'error' },
  }

  return [
    { title: t('grid.columns.id'), field: 'id', mono: true },
    { title: t('grid.columns.customer'), field: 'customer' },
    {
      title: t('grid.columns.amount'),
      field: 'amount',
      align: 'end',
      mono: true,
      format: (value) => currency(Number(value), 'GBP'),
    },
    {
      title: t('grid.columns.risk'),
      field: 'risk',
      badge: (value) => risk[value as Risk],
    },
    {
      title: t('grid.columns.opened'),
      field: 'opened',
      format: (value) => date(String(value)),
    },
  ]
})

function onFilter(event: Event): void {
  const q = (event.target as HTMLInputElement).value

  void router.replace({ query: { ...route.query, q: q || undefined } })
}

function onSort(next: GridSort | undefined): void {
  const query = { ...route.query, sort: next?.field, dir: next?.dir }

  if (route.query.sort === query.sort && route.query.dir === query.dir) return

  void router.replace({ query })
}

function exportCsv(): void {
  grid.value?.downloadCsv('cases.csv')
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <h1 class="text-2xl font-semibold text-primary">{{ t('grid.title') }}</h1>
    <p>{{ t('grid.intro') }}</p>
    <div class="flex flex-wrap items-end gap-4">
      <label class="form-control flex flex-col gap-1">
        <span class="text-sm">{{ t('grid.filterLabel') }}</span>
        <input
          type="search"
          class="input input-sm"
          :value="filterText"
          :placeholder="t('grid.filterPlaceholder')"
          @input="onFilter"
        />
      </label>
      <button type="button" class="btn btn-primary btn-sm" @click="exportCsv">
        {{ t('grid.exportCsv') }}
      </button>
      <p role="status" data-testid="row-count">{{ t('grid.rows', visible) }}</p>
    </div>
    <DataGrid
      ref="grid"
      :columns="columns"
      :rows="sampleCases"
      filter-field="customer"
      :filter-text="filterText"
      :sort="sort"
      :direction="direction"
      :empty-text="t('grid.empty')"
      :label="t('grid.title')"
      @sort="onSort"
      @visible-rows="visible = $event"
    />
  </section>
</template>
