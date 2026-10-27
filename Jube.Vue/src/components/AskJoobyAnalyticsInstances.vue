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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { RefreshCw } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { useExpandHosts } from '../composables/useExpandHosts'
import { useFormat } from '../composables/useFormat'
import { directionFor } from '../i18n'
import { useToasts } from '../stores/toasts'
import { analyticsApi } from '../askJoobyAnalytics/api'
import { allocatedModelIds, instanceRows, type InstanceRow } from '../askJoobyAnalytics/model'
import type { TrainedModel } from '../askJoobyAnalytics/types'
import type { ValueState } from '../valueGrid/model'
import AskJoobyAnalyticsModels from './AskJoobyAnalyticsModels.vue'
import DataGrid from './DataGrid.vue'
import type { GridActionEvent, GridBadge, GridColumn, GridInputEvent } from './dataGridTypes'
import ProblemAlert from './ProblemAlert.vue'

const props = withDefaults(
  defineProps<{
    canSetActive?: boolean
    canDelete?: boolean
    canDeleteAllocated?: boolean
    modelId?: number
  }>(),
  { canSetActive: false, canDelete: false, canDeleteAllocated: false, modelId: undefined },
)

const emit = defineEmits<{ open: [model: TrainedModel, instance: InstanceRow] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const queryClient = useQueryClient()
const toasts = useToasts()
const { hosts, onExpand } = useExpandHosts()

const direction = computed(() => directionFor(locale.value))
const reasons = ref<string[]>([])
const approvalNotes = ref<Record<number, string>>({})
const returnNotes = ref<Record<number, string>>({})
const pendingDelete = ref<InstanceRow>()
const dialog = ref<HTMLDialogElement>()

const instances = useQuery({
  queryKey: queryKeys.analyticsInstances(),
  queryFn: analyticsApi.instances,
  retry: false,
})

const names = useQuery({
  queryKey: queryKeys.analyticsModelNames(),
  queryFn: analyticsApi.modelNames,
  retry: false,
})

const scoped = computed(() => {
  const all = instances.data.value ?? []

  return props.modelId === undefined
    ? all
    : all.filter(
        (instance) =>
          instance.entityAnalysisModelId === props.modelId ||
          instance.entityAnalysisModelId == null,
      )
})

const modelIds = computed(() => allocatedModelIds(scoped.value))

const statuses = useQuery({
  queryKey: computed(() => queryKeys.analyticsStatuses(modelIds.value)),
  queryFn: async () => (await Promise.all(modelIds.value.map(analyticsApi.statuses))).flat(),
  enabled: computed(() => instances.isSuccess.value),
  retry: false,
})

const rows = computed(() =>
  instanceRows(
    scoped.value,
    statuses.data.value ?? [],
    names.data.value ?? [],
    statuses.isError.value,
    t('analytics.instances.notAllocated'),
  ),
)

const hasPending = computed(() => rows.value.some((row) => row.pending && row.canApprove))

const stateLabels: Record<ValueState, string> = {
  approved: t('analytics.state.approved'),
  awaiting: t('analytics.state.awaiting'),
  deletionAwaiting: t('analytics.state.deletionAwaiting'),
  rejected: t('analytics.state.rejected'),
  deletionRejected: t('analytics.state.deletionRejected'),
  unavailable: t('analytics.state.unavailable'),
}

const stateTone: Record<ValueState, GridBadge['tone']> = {
  approved: 'success',
  awaiting: 'warning',
  deletionAwaiting: 'warning',
  rejected: 'error',
  deletionRejected: 'error',
  unavailable: 'warning',
}

function refresh(): void {
  reasons.value = []
  void queryClient.invalidateQueries({ queryKey: ['analytics'] })
}

const setActive = useMutation({
  mutationFn: (input: { id: number; active: boolean }) =>
    analyticsApi.setActive(input.id, input.active),
  onSuccess: () => {
    reasons.value = []
  },
  onError: (error) => toasts.apiError(error),
  onSettled: refresh,
})

const decide = useMutation({
  mutationFn: (input: { row: InstanceRow; action: 'Approve' | 'Reject' }) => {
    const status = (statuses.data.value ?? []).find((item) => item.entityId === input.row.id)!

    return analyticsApi.decide(
      status,
      input.action,
      approvalNotes.value[input.row.id]?.trim() || null,
    )
  },
  onSuccess: (_result, input) => {
    toasts.success(
      input.action === 'Approve'
        ? t('analytics.instances.approved')
        : t('analytics.instances.rejected'),
    )
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const act = useMutation({
  mutationFn: (input: { row: InstanceRow; action: 'Resubmit' | 'Retract' }) =>
    analyticsApi.act(input.row.id, input.action, returnNotes.value[input.row.id]?.trim() || null),
  onSuccess: (_result, input) => {
    toasts.success(
      input.action === 'Resubmit'
        ? t('analytics.instances.resubmitted')
        : t('analytics.instances.retracted'),
    )
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const remove = useMutation({
  mutationFn: (row: InstanceRow) => analyticsApi.remove(row.id),
  onSuccess: () => {
    toasts.success(t('analytics.instances.deleted'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const approveAll = useMutation({
  mutationFn: async () =>
    (await Promise.all(modelIds.value.map(analyticsApi.approveAll))).flatMap(
      (result) => result.refusals ?? [],
    ),
  onSuccess: (refusals) => {
    refresh()
    reasons.value = refusals
  },
  onError: (error) => toasts.apiError(error),
})

watch(pendingDelete, (row) => {
  if (row) dialog.value?.showModal()
  else dialog.value?.close()
})

function mayDelete(row: InstanceRow): boolean {
  return row.entityAnalysisModelId ? props.canDeleteAllocated : props.canDelete
}

function toggleFor(row: object): Element {
  const instance = row as InstanceRow
  const box = document.createElement('input')

  box.type = 'checkbox'
  box.className = 'toggle toggle-sm'
  box.checked = instance.active
  box.disabled = !props.canSetActive || instance.deleted
  box.setAttribute('aria-label', t('analytics.instances.activate', { name: instance.name }))
  box.dataset.testid = `instance-active-${instance.id}`
  box.addEventListener('change', () => setActive.mutate({ id: instance.id, active: box.checked }))

  return box
}

function onInput(event: GridInputEvent): void {
  const row = event.row as InstanceRow

  if (event.field === 'note') approvalNotes.value[row.id] = event.value
  else if (event.field === 'returnNote') returnNotes.value[row.id] = event.value
}

function onAction(event: GridActionEvent): void {
  const row = event.row as InstanceRow

  if (event.id === 'approve') decide.mutate({ row, action: 'Approve' })
  else if (event.id === 'reject') decide.mutate({ row, action: 'Reject' })
  else if (event.id === 'resubmit') act.mutate({ row, action: 'Resubmit' })
  else if (event.id === 'retract') act.mutate({ row, action: 'Retract' })
  else if (event.id === 'delete') pendingDelete.value = row
}

function confirmDelete(): void {
  const row = pendingDelete.value

  pendingDelete.value = undefined

  if (row) remove.mutate(row)
}

const yes = (value: unknown) => (value ? t('analytics.instances.yes') : t('analytics.instances.no'))

const columns = computed<GridColumn[]>(() => [
  { field: 'name', title: t('analytics.instances.name'), minWidth: 200 },
  { field: 'active', title: t('analytics.instances.active'), width: 100, draw: toggleFor },
  { field: 'allocated', title: t('analytics.instances.allocated'), minWidth: 180 },
  {
    field: 'responsePayload',
    title: t('analytics.instances.responsePayload'),
    width: 150,
    format: yes,
  },
  { field: 'reportTable', title: t('analytics.instances.reportTable'), width: 130, format: yes },
  {
    field: 'state',
    title: t('analytics.instances.approval'),
    width: 220,
    badge: (value): GridBadge => ({
      label: stateLabels[value as ValueState],
      tone: stateTone[value as ValueState],
    }),
  },
  { field: 'progress', title: t('analytics.instances.progress'), mono: true, width: 110 },
  { field: 'maker', title: t('analytics.instances.madeBy'), width: 130 },
  {
    field: 'note',
    title: t('analytics.instances.checkerNote'),
    minWidth: 200,
    input: (row) => {
      const value = row as InstanceRow

      return value.pending && value.canApprove
        ? {
            placeholder: t('analytics.instances.notePlaceholder'),
            label: t('analytics.instances.checkerNote'),
            value: approvalNotes.value[value.id] ?? '',
          }
        : null
    },
  },
  {
    field: 'returnNote',
    title: t('analytics.instances.returnNote'),
    minWidth: 200,
    input: (row) => {
      const value = row as InstanceRow

      return value.rejected
        ? {
            placeholder: t('analytics.instances.notePlaceholder'),
            label: t('analytics.instances.returnNote'),
            value: returnNotes.value[value.id] ?? '',
            prefix: value.returnNote || undefined,
          }
        : null
    },
  },
  {
    field: '',
    title: t('analytics.instances.actions'),
    width: 320,
    pinned: true,
    actions: (row) => {
      const value = row as InstanceRow
      const buttons: {
        id: string
        label: string
        tone?: 'primary' | 'success' | 'error'
        disabled?: boolean
      }[] = []

      if (value.pending && value.canApprove) {
        buttons.push(
          { id: 'approve', label: t('analytics.instances.approve'), tone: 'success' },
          { id: 'reject', label: t('analytics.instances.reject'), tone: 'error' },
        )
      }

      if (value.rejected) {
        buttons.push({ id: 'resubmit', label: t('analytics.instances.resubmit'), tone: 'primary' })

        if (value.hasBaseline)
          buttons.push({ id: 'retract', label: t('analytics.instances.retract') })
      }

      if (!value.deleted) {
        buttons.push({
          id: 'delete',
          label: t('analytics.instances.delete'),
          disabled: !mayDelete(value),
        })
      }

      return buttons
    },
  },
  {
    field: 'createdDate',
    title: t('analytics.instances.created'),
    mono: true,
    width: 190,
    format: (value) => (value ? dateTime(new Date(value as string)) : ''),
  },
])

function rowOf(id: unknown): InstanceRow | undefined {
  return rows.value.find((row) => row.id === id)
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-instances">
    <div class="flex flex-wrap items-center gap-2">
      <button
        type="button"
        class="btn btn-sm"
        :disabled="instances.isFetching.value"
        data-testid="analytics-refresh"
        @click="refresh"
      >
        <RefreshCw aria-hidden="true" class="size-4" />
        {{ t('analytics.instances.refresh') }}
      </button>
      <button
        v-if="hasPending"
        key="approve-all"
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="approveAll.isPending.value"
        data-testid="analytics-approve-all"
        @click="approveAll.mutate()"
      >
        {{ t('analytics.instances.approveAll') }}
      </button>
    </div>

    <p v-if="statuses.isError.value" key="unavailable" class="text-sm" role="status">
      {{ t('analytics.instances.statusUnavailable') }}
    </p>
    <ProblemAlert
      v-if="reasons.length > 0"
      key="reasons"
      :reasons="reasons"
      data-testid="analytics-reasons"
    />
    <ProblemAlert
      v-if="instances.isError.value"
      key="failed"
      :reasons="[t('analytics.instances.failed')]"
    />
    <DataGrid
      v-else
      key="grid"
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :empty-text="t('analytics.instances.empty')"
      :label="t('analytics.instances.title')"
      expandable
      :page-size="10"
      auto-height
      :expand-labels="{
        expand: t('analytics.instances.showModels'),
        collapse: t('analytics.instances.hideModels'),
      }"
      @input="onInput"
      @action="onAction"
      @expand="onExpand"
    />
    <template v-for="[id, host] in hosts" :key="String(id)">
      <Teleport :to="host">
        <AskJoobyAnalyticsModels
          :instance-id="Number(id)"
          @open="(model) => emit('open', model, rowOf(id)!)"
        />
      </Teleport>
    </template>

    <dialog
      ref="dialog"
      class="modal"
      data-testid="analytics-confirm"
      @close="pendingDelete = undefined"
    >
      <div class="modal-box">
        <h3 class="text-lg font-bold">
          {{ t('analytics.instances.confirmTitle', { name: pendingDelete?.name ?? '' }) }}
        </h3>
        <p class="py-4">{{ t('analytics.instances.confirmBody') }}</p>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="analytics-confirm-no"
            @click="pendingDelete = undefined"
          >
            {{ t('analytics.instances.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-error"
            data-testid="analytics-confirm-yes"
            @click="confirmDelete"
          >
            {{ t('analytics.instances.delete') }}
          </button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button type="submit">{{ t('analytics.instances.cancel') }}</button>
      </form>
    </dialog>
  </div>
</template>
