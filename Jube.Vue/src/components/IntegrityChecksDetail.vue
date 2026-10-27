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
import { sortedChecks } from '../integrity/model'
import type { IntegrityCheck, Severity } from '../integrity/types'
import DataGrid from './DataGrid.vue'
import type { GridBadge, GridColumn, GridTone } from './dataGridTypes'

const props = defineProps<{ checks: IntegrityCheck[] }>()

const { t, locale } = useI18n()

const direction = computed(() => directionFor(locale.value))
const rows = computed(() =>
  sortedChecks(props.checks).map((check, index) => ({ ...check, id: `${check.code}|${index}` })),
)

const tones: Record<Severity, GridTone> = { Error: 'error', Warning: 'warning', Info: 'info' }

function severityBadge(value: unknown): GridBadge {
  const severity = value as Severity

  return {
    label:
      severity === 'Error'
        ? t('integrity.severity.error')
        : severity === 'Warning'
          ? t('integrity.severity.warning')
          : t('integrity.severity.info'),
    tone: tones[severity] ?? 'neutral',
  }
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'severity',
    title: t('integrity.findings.col.severity'),
    width: 120,
    badge: severityBadge,
  },
  { field: 'title', title: t('integrity.findings.col.check'), minWidth: 220 },
  { field: 'category', title: t('integrity.findings.col.category'), width: 140 },
  { field: 'message', title: t('integrity.findings.col.finding'), minWidth: 280 },
])
</script>

<template>
  <DataGrid
    :columns="columns"
    :rows="rows"
    :direction="direction"
    :empty-text="t('integrity.none')"
    :label="t('integrity.findings.detail')"
    auto-height
  />
</template>
