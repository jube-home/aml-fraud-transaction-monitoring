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
import type { DependencyLink } from '../integrity/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

defineProps<{ uses: DependencyLink[]; usedBy: DependencyLink[] }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))

function rowsOf(links: DependencyLink[]) {
  return links.map((link, index) => ({ ...link, id: `${link.entity}|${index}` }))
}

const columns = computed<GridColumn[]>(() => [
  { field: 'entity', title: t('integrity.deps.col.entity'), minWidth: 220 },
  { field: 'through', title: t('integrity.deps.col.through'), minWidth: 220 },
  {
    field: 'how',
    title: t('integrity.deps.col.how'),
    width: 140,
    format: (value) =>
      value === 'setting' ? t('integrity.deps.how.setting') : t('integrity.deps.how.ruleText'),
  },
])
</script>

<template>
  <div class="flex flex-col gap-3">
    <section class="flex flex-col gap-1">
      <h4 class="text-sm font-semibold">{{ t('integrity.deps.uses') }}</h4>
      <DataGrid
        :columns="columns"
        :rows="rowsOf(uses)"
        :direction="direction"
        :empty-text="t('integrity.none')"
        :label="t('integrity.deps.uses')"
        auto-height
      />
    </section>
    <section class="flex flex-col gap-1">
      <h4 class="text-sm font-semibold">{{ t('integrity.deps.usedBy') }}</h4>
      <DataGrid
        :columns="columns"
        :rows="rowsOf(usedBy)"
        :direction="direction"
        :empty-text="t('integrity.none')"
        :label="t('integrity.deps.usedBy')"
        auto-height
      />
    </section>
  </div>
</template>
