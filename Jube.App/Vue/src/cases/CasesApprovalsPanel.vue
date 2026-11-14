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
import {useQuery} from '@tanstack/vue-query'
import {useRouter} from 'vue-router'
import DataGrid from '../components/DataGrid.vue'
import type {GridActionEvent, GridColumn} from '../components/dataGridTypes'
import PanelCard from '../components/PanelCard.vue'
import {useFormat} from '../composables/useFormat'
import {caseLabel} from './labels'
import {suppressionApi} from '../suppression/api'
import type {CaseSuppression} from '../suppression/types'

const {t} = useI18n()
const {dateTime} = useFormat()
const router = useRouter()

const awaiting = useQuery({
  queryKey: ['service', 'CaseSuppression', 'awaiting'] as const,
  queryFn: () => suppressionApi.awaiting(),
  staleTime: 15_000,
  retry: false,
})

function waiting(created: string | null): string {
  if (!created) return ''

  const days = Math.floor((Date.now() - new Date(created).getTime()) / 86_400_000)

  return days < 1 ? t('suppression.panel.today') : t('suppression.panel.days', {n: days}, days)
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'kind',
    title: t('suppression.panel.kind'),
    width: 150,
    badge: () => ({label: t('suppression.panel.suppression'), tone: 'neutral'}),
  },
  {field: 'request', title: t('suppression.panel.request'), minWidth: 240, mono: true},
  {field: 'targets', title: t('suppression.panel.targets'), minWidth: 220},
  {field: 'createdUser', title: t('suppression.panel.maker'), width: 150},
  {field: 'caseCode', title: t('suppression.panel.case'), width: 100, mono: true},
  {
    field: 'createdDate',
    title: t('suppression.panel.requested'),
    width: 190,
    mono: true,
    format: (value) => (value ? dateTime(new Date(String(value))) : ''),
  },
  {field: 'waiting', title: t('suppression.panel.waiting'), width: 130},
  {
    field: 'open',
    title: '',
    hiddenTitle: t('suppression.panel.actions'),
    width: 100,
    actions: () => [{id: 'open', label: t('suppression.panel.open'), tone: 'primary'}],
  },
])

const rows = computed(() =>
  (awaiting.data.value ?? []).map((item) => ({
    ...item,
    kind: 'suppression',
    request: `${item.overrideKey} = ${item.overrideKeyValue}`,
    targets: item.targets
      .map((target) =>
        target.ruleName
          ? `${target.entityAnalysisModelName ?? ''} / ${target.ruleName}`
          : (target.entityAnalysisModelName ?? ''),
      )
      .join(', '),
    caseCode: caseLabel(item.caseId),
    waiting: waiting(item.createdDate),
  })),
)

function open(event: GridActionEvent): void {
  const item = event.row as CaseSuppression

  void router.push({path: `/cases/${item.caseId}`, query: {suppression: String(item.id)}})
}
</script>

<template>
  <PanelCard :title="t('suppression.panel.title')" compact>
    <div data-testid="cases-approvals">
      <p class="mb-2 text-xs text-base-content/70">{{ t('suppression.panel.hint') }}</p>
      <p v-if="awaiting.isError.value" key="failed" role="alert" class="text-sm text-error">
        {{ t('suppression.panel.failed') }}
      </p>
      <DataGrid
        v-else
        key="grid"
        :columns="columns"
        :rows="rows"
        :empty-text="t('suppression.panel.empty')"
        :label="t('suppression.panel.title')"
        auto-height
        @action="open"
      />
    </div>
  </PanelCard>
</template>
