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
import { useQuery } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { directionFor } from '../i18n'
import { useExpandHosts } from '../composables/useExpandHosts'
import { integrityApi } from '../integrity/api'
import { findingRows } from '../integrity/model'
import type { Severity } from '../integrity/types'
import DataGrid from './DataGrid.vue'
import type { GridBadge, GridColumn, GridTone } from './dataGridTypes'
import IntegrityChecksDetail from './IntegrityChecksDetail.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ modelId: number }>()

const { t, locale } = useI18n()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))

const report = useQuery({
  queryKey: computed(() => queryKeys.integrityReport(props.modelId)),
  queryFn: () => integrityApi.report(props.modelId),
  retry: false,
})

const rows = computed(() => (report.data.value ? findingRows(report.data.value) : []))
const forbidden = computed(
  () => report.error.value instanceof ApiError && report.error.value.kind === 'forbidden',
)

const tones: Record<Severity, GridTone> = { Error: 'error', Warning: 'warning', Info: 'info' }

function worstBadge(value: unknown): GridBadge {
  const severity = (value as number) === 0 ? 'Error' : (value as number) === 1 ? 'Warning' : 'Info'

  return {
    label:
      severity === 'Error'
        ? t('integrity.severity.error')
        : severity === 'Warning'
          ? t('integrity.severity.warning')
          : t('integrity.severity.info'),
    tone: tones[severity],
  }
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'worstRank',
    title: t('integrity.findings.col.severity'),
    width: 120,
    badge: worstBadge,
  },
  { field: 'kind', title: t('integrity.findings.col.kind'), width: 190 },
  { field: 'name', title: t('integrity.findings.col.entity'), minWidth: 200 },
  { field: 'categories', title: t('integrity.findings.col.categories'), minWidth: 160 },
  {
    field: 'errors',
    title: t('integrity.findings.col.errors'),
    align: 'end',
    mono: true,
    width: 90,
  },
  {
    field: 'warnings',
    title: t('integrity.findings.col.warnings'),
    align: 'end',
    mono: true,
    width: 110,
  },
  {
    field: 'infos',
    title: t('integrity.findings.col.infos'),
    align: 'end',
    mono: true,
    width: 130,
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="integrity-findings">
    <ProblemAlert
      v-if="forbidden"
      key="forbidden"
      :reasons="[t('integrity.forbidden')]"
      data-testid="integrity-error"
    />
    <ProblemAlert
      v-else-if="report.isError.value"
      key="failed"
      :reasons="[t('integrity.failed')]"
      data-testid="integrity-error"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('integrity.findings.none')"
      :label="t('integrity.findings.label')"
      row-key="id"
      expandable
      auto-height
      :expand-labels="{ expand: t('integrity.showChecks'), collapse: t('integrity.hideChecks') }"
      @expand="onExpand"
    />

    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <IntegrityChecksDetail :checks="rows.find((row) => row.id === id)?.checks ?? []" />
      </Teleport>
    </template>
  </div>
</template>
