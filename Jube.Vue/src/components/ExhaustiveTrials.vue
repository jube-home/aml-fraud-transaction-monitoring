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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { exhaustiveApi } from '../exhaustive/api'
import { modelTypeLabel } from '../exhaustive/model'
import type { Trial } from '../exhaustive/types'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridColumn } from './dataGridTypes'
import EvidenceTree from './EvidenceTree.vue'
import ProblemAlert from './ProblemAlert.vue'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ instanceId: number; version: string }>()

const { t, locale } = useI18n()
const { dateTime, fixed } = useFormat()

const direction = computed(() => directionFor(locale.value))
const selected = ref<Trial | null>(null)

const trials = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'trials', props.version)),
  queryFn: () => exhaustiveApi.trials(props.instanceId),
  retry: false,
})

const detail = useQuery({
  queryKey: computed(() =>
    queryKeys.exhaustive(props.instanceId, 'trial', selected.value?.id ?? 0, props.version),
  ),
  queryFn: () => exhaustiveApi.trialDetail(selected.value!.id),
  enabled: computed(() => selected.value !== null),
  retry: false,
})

const rows = computed(() => trials.data.value ?? [])

function act(event: GridActionEvent): void {
  if (event.id === 'show') selected.value = event.row as Trial
}

const columns = computed<GridColumn[]>(() => [
  {
    field: 'kind',
    title: t('exhaustive.trials.type'),
    minWidth: 240,
    format: (value) => modelTypeLabel(value as string | null),
  },
  {
    field: 'createdDate',
    title: t('exhaustive.trials.created'),
    mono: true,
    width: 190,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
  {
    field: 'score',
    title: t('exhaustive.trials.score'),
    align: 'end',
    mono: true,
    width: 110,
    format: (value) => (typeof value === 'number' ? fixed(value, 4) : ''),
  },
  {
    field: 'promoted',
    title: t('exhaustive.trials.promoted'),
    width: 120,
    badge: (value) =>
      value
        ? { label: t('exhaustive.trials.yes'), tone: 'success' }
        : { label: t('exhaustive.trials.no'), tone: 'neutral' },
  },
  {
    field: 'evidence',
    title: t('exhaustive.trials.evidence'),
    width: 150,
    actions: () => [{ id: 'show', label: t('exhaustive.trials.show') }],
  },
])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="exhaustive-trials">
    <ProblemAlert
      v-if="trials.isError.value"
      key="failed"
      :reasons="[t('exhaustive.trials.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('exhaustive.trials.empty')"
      :label="t('exhaustive.trials.title')"
      :page-size="25"
      auto-height
      @action="act"
    />

    <p v-if="selected === null" key="none" class="text-sm text-secondary">
      {{ t('exhaustive.trials.choose') }}
    </p>
    <section v-else key="evidence" class="flex min-w-0 flex-col gap-3" data-testid="trial-evidence">
      <h4 class="text-sm font-semibold">
        {{
          t('exhaustive.trials.evidenceTitle', {
            type: modelTypeLabel(selected.kind),
            score: selected.score === null ? '–' : fixed(selected.score, 4),
          })
        }}
      </h4>
      <ProblemAlert
        v-if="detail.isError.value"
        key="detail-failed"
        :reasons="[t('exhaustive.trials.detailFailed')]"
      />
      <ServerChart
        :url="`/api/Chart/Exhaustive/TrialDiagram/${selected.id}`"
        :label="t('exhaustive.trials.diagram')"
        :version="version"
      />
      <EvidenceTree
        v-if="detail.data.value?.evidence"
        key="tree"
        :evidence="detail.data.value.evidence"
        :label="t('exhaustive.trials.evidenceTree')"
      />
    </section>
  </div>
</template>
