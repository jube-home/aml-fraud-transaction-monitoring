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
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import { describeApiError } from '../api/problems'
import { directionFor } from '../i18n'
import { fromLocalInput, toLocalInput } from '../composables/localDateTime'
import { useFormat } from '../composables/useFormat'
import { integrityApi } from '../integrity/api'
import DataGrid from './DataGrid.vue'
import type { GridBadge, GridColumn } from './dataGridTypes'
import PanelCard from './PanelCard.vue'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(defineProps<{ allowed?: boolean; modelId?: number }>(), {
  allowed: true,
  modelId: undefined,
})

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const queryClient = useQueryClient()
const id = useId()

const direction = computed(() => directionFor(locale.value))
const picked = ref(toLocalInput(new Date()))
const problems = ref<string[]>([])

const schedule = useQuery({
  queryKey: computed(() => queryKeys.integritySchedule(props.modelId)),
  queryFn: () => integrityApi.schedule(props.modelId),
  retry: false,
})

const nodes = useQuery({
  queryKey: queryKeys.integritySyncNodes(),
  queryFn: integrityApi.nodeStatus,
  retry: false,
})

watch(
  () => schedule.data.value?.scheduleDate,
  (value) => {
    if (value) picked.value = toLocalInput(value)
  },
)

function failure(error: unknown): string[] {
  if (
    error instanceof ApiError &&
    JSON.stringify(error.body ?? '').includes('ScheduleDateTooFarInFuture')
  ) {
    return [t('integrity.sync.tooFar')]
  }

  return describeApiError(error, {
    failed: t('integrity.sync.failed'),
    refused: t('integrity.sync.refused'),
  })
}

const send = useMutation({
  mutationFn: (scheduleDate: string | null) =>
    integrityApi.setSchedule(scheduleDate, props.modelId),
  onSuccess: async () => {
    problems.value = []
    await queryClient.invalidateQueries({ queryKey: ['integrity'] })
  },
  onError: (error) => {
    problems.value = failure(error)
  },
})

function scheduleAt(): void {
  const date = fromLocalInput(picked.value)

  if (!date) {
    problems.value = [t('integrity.sync.chooseFirst')]

    return
  }

  send.mutate(date.toISOString())
}

function refresh(): void {
  problems.value = []
  void queryClient.invalidateQueries({ queryKey: ['integrity'] })
}

const when = (value: unknown) => (value ? dateTime(new Date(value as string)) : '')

function stateBadge(value: unknown): GridBadge {
  return value === 'unavailable'
    ? { label: t('integrity.sync.state.unavailable'), tone: 'error' }
    : value === 'pending'
      ? { label: t('integrity.sync.state.pending'), tone: 'warning' }
      : { label: t('integrity.sync.state.synchronised'), tone: 'success' }
}

const rows = computed(() =>
  (nodes.data.value ?? []).map((node) => ({
    ...node,
    state: node.instanceAvailable
      ? node.synchronisationPending
        ? 'pending'
        : 'synchronised'
      : 'unavailable',
  })),
)

const columns = computed<GridColumn[]>(() => [
  { field: 'instance', title: t('integrity.sync.col.instance'), minWidth: 180 },
  {
    field: 'heartbeatDate',
    title: t('integrity.sync.col.heartbeat'),
    mono: true,
    width: 190,
    format: when,
  },
  {
    field: 'synchronisedDate',
    title: t('integrity.sync.col.synchronised'),
    mono: true,
    width: 190,
    format: when,
  },
  {
    field: 'state',
    title: t('integrity.sync.col.state'),
    width: 160,
    badge: stateBadge,
  },
])
</script>

<template>
  <PanelCard
    :title="modelId === undefined ? t('integrity.sync.tenantTitle') : t('integrity.sync.title')"
    compact
  >
    <div class="flex min-w-0 flex-col gap-3" data-testid="model-synchronisation">
      <p class="text-sm text-secondary">
        {{ modelId === undefined ? t('integrity.sync.tenantHint') : t('integrity.sync.hint') }}
      </p>

      <form
        v-if="allowed"
        key="form"
        class="flex flex-wrap items-end gap-x-4 gap-y-3"
        @submit.prevent="scheduleAt"
      >
        <label class="flex flex-col gap-1" :for="`${id}-when`">
          <span class="label text-xs">{{ t('integrity.sync.when') }}</span>
          <input
            :id="`${id}-when`"
            v-model="picked"
            type="datetime-local"
            class="input input-sm font-mono"
            data-testid="sync-when"
          />
        </label>
        <div class="flex gap-2">
          <button
            type="submit"
            class="btn btn-sm"
            :disabled="send.isPending.value"
            data-testid="sync-schedule"
          >
            {{
              modelId === undefined
                ? t('integrity.sync.scheduleTenant')
                : t('integrity.sync.schedule')
            }}
          </button>
          <button
            type="button"
            class="btn btn-sm"
            :disabled="send.isPending.value"
            data-testid="sync-now"
            @click="send.mutate(null)"
          >
            {{ modelId === undefined ? t('integrity.sync.nowTenant') : t('integrity.sync.now') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-ghost"
            data-testid="sync-refresh"
            @click="refresh"
          >
            {{ t('integrity.sync.refresh') }}
          </button>
        </div>
      </form>

      <ProblemAlert
        v-if="problems.length > 0"
        key="problems"
        :reasons="problems"
        data-testid="sync-problems"
      />

      <DataGrid
        :columns="columns"
        :rows="rows"
        :direction="direction"
        :empty-text="t('integrity.sync.empty')"
        :label="t('integrity.sync.title')"
        row-key="id"
        auto-height
      />
    </div>
  </PanelCard>
</template>
