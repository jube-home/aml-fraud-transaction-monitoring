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
import { CheckCheck, Plus } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { ApiError } from '../api/useApi'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { usePagerLabels } from '../composables/usePagerLabels'
import { useToasts } from '../stores/toasts'
import { validationReasons } from '../api/problems'
import { valueGridApi } from '../valueGrid/api'
import {
  buildRows,
  draftOf,
  draftProblems,
  matches,
  payloadOf,
  type Draft,
  type Editing,
  type ValueConfig,
  type ValueRow,
  type ValueState,
} from '../valueGrid/model'
import DataGrid from './DataGrid.vue'
import ProblemAlert from './ProblemAlert.vue'
import type { GridActionEvent, GridBadge, GridColumn, GridInputEvent } from './dataGridTypes'

const props = defineProps<{ config: ValueConfig; parentId: number }>()

const emit = defineEmits<{ changed: [] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const toasts = useToasts()
const queryClient = useQueryClient()

const direction = computed(() => directionFor(locale.value))

const base = computed(() => queryKeys.valueGrid(props.config.endpoint, props.parentId))

const values = useQuery({
  queryKey: computed(() => [...base.value, 'values']),
  queryFn: () => valueGridApi.values(props.config, props.parentId),
  retry: false,
})

const statuses = useQuery({
  queryKey: computed(() => [...base.value, 'statuses']),
  queryFn: () => valueGridApi.statuses(props.config, props.parentId),
  retry: false,
})

const search = ref('')
const editing = ref<Editing>(null)
const draft = ref<Draft>({})
const approvalNotes = ref<Record<number, string>>({})
const returnNotes = ref<Record<number, string>>({})
const reasons = ref<string[]>([])
const pendingDelete = ref<ValueRow>()
const dialog = ref<HTMLDialogElement>()

const unavailable = computed(() => statuses.isError.value)

const rows = computed<ValueRow[]>(() =>
  buildRows(
    values.data.value ?? [],
    statuses.data.value ?? [],
    unavailable.value,
    editing.value,
    props.config.valueFields,
  ).filter((row) => row.isNew || matches(row, props.config.valueFields, search.value)),
)

const hasPending = computed(() =>
  (statuses.data.value ?? []).some((status) => status.canApprove && status.pending),
)

const stateLabels: Record<ValueState, string> = {
  approved: t('valueGrid.state.approved'),
  awaiting: t('valueGrid.state.awaiting'),
  deletionAwaiting: t('valueGrid.state.deletionAwaiting'),
  rejected: t('valueGrid.state.rejected'),
  deletionRejected: t('valueGrid.state.deletionRejected'),
  unavailable: t('valueGrid.state.unavailable'),
}

const stateTone: Record<ValueState, GridBadge['tone']> = {
  approved: 'success',
  awaiting: 'warning',
  deletionAwaiting: 'warning',
  rejected: 'error',
  deletionRejected: 'error',
  unavailable: 'warning',
}

const pagerLabels = usePagerLabels()

const columns = computed<GridColumn[]>(() => [
  ...props.config.valueFields.map((field): GridColumn => ({
    field: field.field,
    title: field.title,
    minWidth: 160,
    input: (row) =>
      (row as ValueRow).editing
        ? {
            placeholder: field.title,
            label: field.title,
            value: draft.value[field.field] ?? '',
            type: field.type === 'number' ? 'number' : 'text',
          }
        : null,
  })),
  {
    field: 'deleteExpiryDate',
    title: t('valueGrid.col.expiry'),
    width: 190,
    mono: true,
    format: (value) => (value ? dateTime(String(value)) : ''),
    input: (row) =>
      (row as ValueRow).editing
        ? {
            placeholder: t('valueGrid.col.expiry'),
            label: t('valueGrid.col.expiry'),
            value: draft.value.deleteExpiryDate ?? '',
            type: 'datetime-local',
          }
        : null,
  },
  {
    field: 'state',
    title: t('valueGrid.col.state'),
    width: 210,
    badge: (value): GridBadge => ({
      label: stateLabels[value as ValueState],
      tone: stateTone[value as ValueState],
    }),
  },
  { field: 'progress', title: t('valueGrid.col.approvals'), width: 110, mono: true },
  { field: 'maker', title: t('valueGrid.col.madeBy'), width: 130 },
  {
    field: 'note',
    title: t('valueGrid.col.checkerNote'),
    minWidth: 200,
    input: (row) => {
      const value = row as ValueRow

      return value.pending && value.canApprove && !value.editing
        ? {
            placeholder: t('valueGrid.notePlaceholder'),
            label: t('valueGrid.col.checkerNote'),
            value: approvalNotes.value[value.id] ?? '',
          }
        : null
    },
  },
  {
    field: 'returnNote',
    title: t('valueGrid.col.returnNote'),
    minWidth: 200,
    input: (row) => {
      const value = row as ValueRow

      return value.rejected && !value.editing
        ? {
            placeholder: t('valueGrid.notePlaceholder'),
            label: t('valueGrid.col.returnNote'),
            value: returnNotes.value[value.id] ?? '',
            prefix: value.returnNote || undefined,
          }
        : null
    },
  },
  {
    field: '',
    title: t('valueGrid.col.actions'),
    width: 330,
    pinned: true,
    actions: (row) => {
      const value = row as ValueRow

      if (value.editing) {
        return [
          { id: 'save', label: t('valueGrid.save'), tone: 'primary' },
          { id: 'cancel', label: t('valueGrid.cancel') },
        ]
      }

      const busy = editing.value !== null
      const buttons: {
        id: string
        label: string
        tone?: 'primary' | 'success' | 'error'
        disabled?: boolean
      }[] = []

      if (value.pending && value.canApprove) {
        buttons.push(
          { id: 'approve', label: t('valueGrid.approve'), tone: 'success' },
          { id: 'reject', label: t('valueGrid.reject'), tone: 'error' },
        )
      }

      if (value.rejected) {
        buttons.push({ id: 'resubmit', label: t('valueGrid.resubmit'), tone: 'primary' })

        if (value.hasBaseline) buttons.push({ id: 'retract', label: t('valueGrid.retract') })
      }

      buttons.push({ id: 'edit', label: t('valueGrid.edit'), disabled: busy })

      if (!value.deleted)
        buttons.push({ id: 'delete', label: t('valueGrid.delete'), disabled: busy })

      return buttons
    },
  },
])

watch(
  () => [props.config.endpoint, props.parentId],
  () => {
    editing.value = null
    search.value = ''
  },
)

watch(pendingDelete, (row) => {
  if (row) dialog.value?.showModal()
  else dialog.value?.close()
})

function failure(error: unknown): void {
  if (error instanceof ApiError && error.kind === 'validation') {
    reasons.value = validationReasons(error, t('valueGrid.refused'))

    return
  }

  toasts.apiError(error)
}

async function refresh(): Promise<void> {
  await queryClient.invalidateQueries({ queryKey: base.value })
  emit('changed')
}

const save = useMutation({
  mutationFn: () => {
    const body = payloadOf(
      props.config,
      props.parentId,
      draft.value,
      editing.value && editing.value.id !== 'new' ? editing.value.id : undefined,
    )

    return editing.value && editing.value.id === 'new'
      ? valueGridApi.create(props.config, body)
      : valueGridApi.update(props.config, body)
  },
  onSuccess: async () => {
    reasons.value = []
    editing.value = null
    toasts.success(t('valueGrid.saved'))
    await refresh()
  },
  onError: failure,
})

const remove = useMutation({
  mutationFn: (row: ValueRow) => valueGridApi.remove(props.config, row.id),
  onSuccess: async () => {
    reasons.value = []
    toasts.success(t('valueGrid.deleted'))
    await refresh()
  },
  onError: failure,
})

const decide = useMutation({
  mutationFn: (variables: { row: ValueRow; action: 'Approve' | 'Reject' }) => {
    const status = (statuses.data.value ?? []).find((item) => item.entityId === variables.row.id)!

    return valueGridApi.decide(
      status,
      variables.action,
      approvalNotes.value[variables.row.id]?.trim() || null,
    )
  },
  onSuccess: async (_result, variables) => {
    reasons.value = []
    toasts.success(
      variables.action === 'Approve' ? t('valueGrid.approved') : t('valueGrid.rejected'),
    )
    await refresh()
  },
  onError: failure,
})

const act = useMutation({
  mutationFn: (variables: { row: ValueRow; action: 'Resubmit' | 'Retract' }) =>
    valueGridApi.act(
      props.config,
      variables.row.id,
      variables.action,
      returnNotes.value[variables.row.id]?.trim() || null,
    ),
  onSuccess: async (_result, variables) => {
    reasons.value = []
    toasts.success(
      variables.action === 'Resubmit' ? t('valueGrid.resubmitted') : t('valueGrid.retracted'),
    )
    await refresh()
  },
  onError: failure,
})

const approveAll = useMutation({
  mutationFn: () => valueGridApi.approveAll(props.config, props.parentId),
  onSuccess: async (result) => {
    reasons.value = result.refusals ?? []
    await refresh()
  },
  onError: failure,
})

function startEdit(row: ValueRow): void {
  reasons.value = []
  draft.value = draftOf(row, props.config.valueFields)
  editing.value = { id: row.id }
}

function startAdd(): void {
  reasons.value = []
  draft.value = draftOf(
    buildRows([], [], false, { id: 'new' }, props.config.valueFields)[0]!,
    props.config.valueFields,
  )
  editing.value = { id: 'new' }
}

function commit(): void {
  const problems = draftProblems(draft.value, props.config.valueFields)

  if (problems.length > 0) {
    reasons.value = problems.map((problem) =>
      t('valueGrid.notNumber', {
        field: props.config.valueFields.find((field) => field.field === problem.field)!.title,
      }),
    )

    return
  }

  reasons.value = []
  save.mutate()
}

function onAction(event: GridActionEvent): void {
  const row = event.row as ValueRow

  if (event.id === 'edit') startEdit(row)
  else if (event.id === 'delete') pendingDelete.value = row
  else if (event.id === 'save') commit()
  else if (event.id === 'cancel') {
    reasons.value = []
    editing.value = null
  } else if (event.id === 'approve') decide.mutate({ row, action: 'Approve' })
  else if (event.id === 'reject') decide.mutate({ row, action: 'Reject' })
  else if (event.id === 'resubmit') act.mutate({ row, action: 'Resubmit' })
  else if (event.id === 'retract') act.mutate({ row, action: 'Retract' })
}

function onInput(event: GridInputEvent): void {
  const row = event.row as ValueRow

  if (event.field === 'note') approvalNotes.value[row.id] = event.value
  else if (event.field === 'returnNote') returnNotes.value[row.id] = event.value
  else draft.value = { ...draft.value, [event.field]: event.value }
}

function confirmDelete(): void {
  const row = pendingDelete.value

  pendingDelete.value = undefined

  if (row) remove.mutate(row)
}
</script>

<template>
  <section class="flex min-h-0 flex-1 flex-col gap-3" data-testid="value-grid">
    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('valueGrid.search') }}</span>
        <input
          v-model="search"
          type="search"
          class="input input-sm w-64"
          data-testid="value-search"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="editing !== null"
        data-testid="value-add"
        @click="startAdd"
      >
        <Plus aria-hidden="true" class="size-4" />
        {{ t('valueGrid.add') }}
      </button>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!hasPending || approveAll.isPending.value"
        data-testid="value-approve-all"
        @click="approveAll.mutate()"
      >
        <CheckCheck aria-hidden="true" class="size-4" />
        {{ t('valueGrid.approveAll') }}
      </button>
    </div>

    <p v-if="unavailable" role="status" class="text-sm" data-testid="value-status-unavailable">
      {{ t('valueGrid.statusUnavailable') }}
    </p>

    <ProblemAlert v-if="reasons.length > 0" :reasons="reasons" data-testid="value-reasons" />

    <p v-if="values.isError.value" role="alert" class="alert alert-error">
      {{ t('valueGrid.loadFailed') }}
    </p>
    <p v-else-if="values.isPending.value" role="status" class="text-sm">
      {{ t('valueGrid.loading') }}
    </p>
    <DataGrid
      v-else
      :columns="columns"
      :rows="rows"
      :direction="direction"
      :page-size="25"
      :pager-labels="pagerLabels"
      :empty-text="t('valueGrid.empty')"
      :label="t('valueGrid.title')"
      @action="onAction"
      @input="onInput"
    />

    <dialog
      ref="dialog"
      class="modal"
      data-testid="value-confirm"
      @close="pendingDelete = undefined"
    >
      <div class="modal-box">
        <h3 class="text-lg font-bold">{{ t('valueGrid.confirmTitle') }}</h3>
        <p class="py-4">{{ t('valueGrid.confirmBody') }}</p>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="value-confirm-no"
            @click="pendingDelete = undefined"
          >
            {{ t('valueGrid.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-error"
            data-testid="value-confirm-yes"
            @click="confirmDelete"
          >
            {{ t('valueGrid.delete') }}
          </button>
        </div>
      </div>
      <form method="dialog" class="modal-backdrop">
        <button type="submit">{{ t('valueGrid.cancel') }}</button>
      </form>
    </dialog>
  </section>
</template>
