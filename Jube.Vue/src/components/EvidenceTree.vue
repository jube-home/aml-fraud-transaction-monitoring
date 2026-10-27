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
import { computed, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { ChevronsDownUp, ChevronsUpDown } from 'lucide-vue-next'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { countLeaves, evidenceRows, type LeafText } from '../evidence/model'
import DataGrid from './DataGrid.vue'
import type { GridColumn } from './dataGridTypes'

const props = defineProps<{ evidence: unknown; label?: string }>()

const { t, locale } = useI18n()
const { evidenceNumber } = useFormat()

const grid = useTemplateRef<{ expandAll: () => void; collapseAll: () => void }>('grid')

const direction = computed(() => directionFor(locale.value))

const text = computed<LeafText>(() => ({
  yes: t('evidence.yes'),
  no: t('evidence.no'),
  none: t('evidence.none'),
  empty: t('evidence.empty'),
  number: evidenceNumber,
}))

const rows = computed(() => evidenceRows(props.evidence, text.value))
const leaves = computed(() => countLeaves(rows.value))

const columns = computed<GridColumn[]>(() => [
  { field: 'key', title: t('evidence.col.key'), minWidth: 260 },
  { field: 'value', title: t('evidence.col.value'), mono: true, minWidth: 260 },
])
</script>

<template>
  <section class="flex flex-col gap-2" data-testid="evidence-tree">
    <div class="flex flex-wrap items-center gap-2">
      <h3 class="text-sm font-semibold">{{ label ?? t('evidence.title') }}</h3>
      <span class="badge badge-ghost badge-sm font-mono" data-testid="evidence-count">
        {{ t('evidence.values', leaves) }}
      </span>
      <div v-if="rows.length > 0" class="ms-auto flex gap-1">
        <button
          type="button"
          class="btn btn-xs"
          data-testid="evidence-expand"
          @click="grid?.expandAll()"
        >
          <ChevronsUpDown aria-hidden="true" class="size-4" />
          {{ t('evidence.expandAll') }}
        </button>
        <button
          type="button"
          class="btn btn-xs"
          data-testid="evidence-collapse"
          @click="grid?.collapseAll()"
        >
          <ChevronsDownUp aria-hidden="true" class="size-4" />
          {{ t('evidence.collapseAll') }}
        </button>
      </div>
    </div>
    <p v-if="rows.length === 0" class="text-sm" data-testid="evidence-empty">
      {{ t('evidence.emptyState') }}
    </p>
    <DataGrid
      v-else
      ref="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :tree="true"
      :empty-text="t('evidence.emptyState')"
      :label="label ?? t('evidence.title')"
    />
  </section>
</template>
