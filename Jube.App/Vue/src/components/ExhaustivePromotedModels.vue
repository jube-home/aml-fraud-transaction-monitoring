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
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {describeApiError} from '../api/problems'
import {ApiError} from '../api/useApi'
import {queryKeys} from '../api/queryKeys'
import {useGridExpandLabels} from '../composables/useGridExpandLabels'
import {useFormat} from '../composables/useFormat'
import {directionFor} from '../i18n'
import {hasPermission, meQueryOptions} from '../auth/me'
import {exhaustiveApi, promotedActivatePermission} from '../exhaustive/api'
import {modelTypeLabel} from '../exhaustive/model'
import type {PromotedModel} from '../exhaustive/types'
import {ref} from 'vue'
import DataGrid from './DataGrid.vue'
import type {GridColumn} from './dataGridTypes'
import ExhaustivePrescriptions from './ExhaustivePrescriptions.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = defineProps<{ instanceId: number; version: string; allowed?: boolean }>()

const {t, locale} = useI18n()
const {dateTime, fixed} = useFormat()
const expandLabels = useGridExpandLabels()
const queryClient = useQueryClient()

const direction = computed(() => directionFor(locale.value))
const problems = ref<string[]>([])

const promoted = useQuery({
  queryKey: computed(() => queryKeys.exhaustive(props.instanceId, 'promoted', props.version)),
  queryFn: () => exhaustiveApi.promoted(props.instanceId),
  retry: false,
})

const rows = computed(() => promoted.data.value ?? [])
const me = useQuery(meQueryOptions)
const canActivate = computed(
  () => props.allowed !== false && hasPermission(me.data.value, promotedActivatePermission),
)

const activate = useMutation({
  mutationFn: (input: { id: number; active: boolean }) =>
    exhaustiveApi.setPromotedActive(input.id, input.active),
  onSuccess: () => {
    problems.value = []
  },
  onError: (error) => {
    if (error instanceof ApiError && error.kind === 'conflict') {
      problems.value = [t('exhaustive.promoted.instanceLive')]

      return
    }

    problems.value = describeApiError(error, {
      failed: t('exhaustive.promoted.activateFailed'),
      refused: t('exhaustive.promoted.refused'),
    })
  },
  onSettled: () =>
    queryClient.invalidateQueries({queryKey: ['exhaustive', props.instanceId, 'promoted']}),
})

function toggleFor(row: object, allowed: boolean): Element {
  const model = row as PromotedModel
  const box = document.createElement('input')

  box.type = 'checkbox'
  box.className = 'toggle toggle-sm'
  box.checked = model.active
  box.disabled = !allowed
  box.setAttribute(
    'aria-label',
    t('exhaustive.promoted.activate', {score: fixed(model.score, 4)}),
  )
  box.dataset.testid = `promoted-active-${model.id}`
  box.addEventListener('change', () => activate.mutate({id: model.id, active: box.checked}))

  return box
}

const columns = computed<GridColumn[]>(() => {
  const allowed = canActivate.value

  return [
    {
      field: 'active',
      title: t('exhaustive.promoted.active'),
      width: 100,
      draw: (row) => toggleFor(row, allowed),
    },
    {
      field: 'kind',
      title: t('exhaustive.promoted.type'),
      minWidth: 220,
      format: (value) => modelTypeLabel(value as string | null),
    },
    {
      field: 'createdDate',
      title: t('exhaustive.promoted.created'),
      mono: true,
      width: 190,
      format: (value) => (value ? dateTime(new Date(value as string)) : ''),
    },
    {
      field: 'score',
      title: t('exhaustive.promoted.score'),
      align: 'end',
      mono: true,
      width: 110,
      format: (value) => fixed(Number(value), 4),
    },
    {
      field: 'topologyComplexity',
      title: t('exhaustive.promoted.complexity'),
      align: 'end',
      mono: true,
      width: 130,
    },
    {field: 'calibrationMethod', title: t('exhaustive.promoted.calibration'), width: 150},
  ]
})
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="exhaustive-promoted">
    <p class="text-xs text-secondary">{{ t('exhaustive.promoted.hint') }}</p>
    <ProblemAlert
      v-if="problems.length > 0"
      key="problems"
      :reasons="problems"
      data-testid="promoted-problems"
    />
    <ProblemAlert
      v-if="promoted.isError.value"
      key="failed"
      :reasons="[t('exhaustive.promoted.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('exhaustive.promoted.empty')"
      :label="t('exhaustive.promoted.title')"
      auto-height
      :expand-labels="expandLabels"
    >
      <template #summary="{ id: id }">
        <ExhaustivePrescriptions :promoted-id="Number(id)" :version="version"/>
      </template>
    </DataGrid>
  </div>
</template>
