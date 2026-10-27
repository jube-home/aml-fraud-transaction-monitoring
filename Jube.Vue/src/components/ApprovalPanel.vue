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
import { queryKeys } from '../api/queryKeys'
import { ApiError } from '../api/useApi'
import { directionFor } from '../i18n'
import { useFormat } from '../composables/useFormat'
import { useToasts } from '../stores/toasts'
import { approvalApi, type ApprovalTarget } from '../approval/api'
import {
  buildHistoryRows,
  refusalReasons,
  stateOf,
  summariseChanges,
  summariseVersions,
  type ApprovalState,
  type ApprovalKind,
  type ApprovalStatus,
  type ChangeKind,
  type HistoryRow,
  type VersionSummary,
} from '../approval/model'
import DataGrid from './DataGrid.vue'
import ProblemAlert from './ProblemAlert.vue'
import type { GridActionEvent, GridBadge, GridColumn, GridInputEvent } from './dataGridTypes'

const props = defineProps<{ endpoint: string; approval: ApprovalKind; entityId: number }>()

const emit = defineEmits<{ changed: []; status: [status: ApprovalStatus | undefined] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()
const toasts = useToasts()
const queryClient = useQueryClient()

const direction = computed(() => directionFor(locale.value))

const target = computed<ApprovalTarget>(() => ({
  endpoint: props.endpoint,
  kind: props.approval,
  entityId: props.entityId,
}))

const base = computed(() => queryKeys.approval(props.endpoint, props.entityId))

const status = useQuery({
  queryKey: computed(() => [...base.value, 'status']),
  queryFn: () => approvalApi.status(target.value!),
  retry: false,
})

const loaded = computed(() => status.data.value !== undefined)

const records = useQuery({
  queryKey: computed(() => [...base.value, 'history']),
  queryFn: () => approvalApi.history(target.value!),
  enabled: loaded,
  retry: false,
})

const versions = useQuery({
  queryKey: computed(() => [...base.value, 'versions']),
  queryFn: () => approvalApi.versions(target.value!),
  enabled: loaded,
  retry: false,
})

const compared = ref<VersionSummary>()

const changes = useQuery({
  queryKey: computed(() =>
    queryKeys.approvalCompare(
      props.endpoint,
      compared.value?.previousId ?? 0,
      compared.value?.id ?? 0,
    ),
  ),
  queryFn: () =>
    approvalApi.compare(target.value!, compared.value!.previousId!, compared.value!.id),
  enabled: computed(() => compared.value?.previousId != null),
  retry: false,
})

watch(
  () => status.data.value,
  (value) => emit('status', value),
  { immediate: true },
)

watch(
  () => [props.endpoint, props.entityId],
  () => {
    compared.value = undefined
  },
)

const checkerNote = ref('')
const returnNote = ref('')
const reasons = ref<string[]>([])

const hasBaseline = computed(
  () =>
    status.data.value?.effectiveVersion !== null &&
    status.data.value?.effectiveVersion !== undefined,
)

const state = computed<ApprovalState | undefined>(() =>
  status.data.value ? stateOf(status.data.value) : undefined,
)

const stateLabels: Record<ApprovalState, string> = {
  approved: t('approval.state.approved'),
  awaiting: t('approval.state.awaiting'),
  rejected: t('approval.state.rejected'),
  deleted: t('approval.state.deleted'),
  deletionAwaiting: t('approval.state.deletionAwaiting'),
  deletionRejected: t('approval.state.deletionRejected'),
}

const stateTone: Record<ApprovalState, string> = {
  approved: 'badge-success',
  awaiting: 'badge-warning',
  rejected: 'badge-error',
  deleted: 'badge-neutral',
  deletionAwaiting: 'badge-warning',
  deletionRejected: 'badge-error',
}

const changeLabels: Record<ChangeKind, string> = {
  created: t('approval.change.created'),
  update: t('approval.change.update'),
  deletion: t('approval.change.deletion'),
  restore: t('approval.change.restore'),
  retracted: t('approval.change.retracted'),
  resubmitted: t('approval.change.resubmitted'),
}

const doneLabels = {
  Approve: t('approval.done.Approve'),
  Reject: t('approval.done.Reject'),
  Resubmit: t('approval.done.Resubmit'),
  Retract: t('approval.done.Retract'),
}

const facts = computed(() => {
  const value = status.data.value

  if (!value) return []

  return [
    { id: 'current', label: t('approval.currentVersion'), text: String(value.currentVersion) },
    {
      id: 'running',
      label: t('approval.runningVersion'),
      text:
        value.effectiveVersion === null ? t('approval.notRunning') : String(value.effectiveVersion),
    },
    { id: 'maker', label: t('approval.madeBy'), text: value.makerUser ?? t('approval.unknown') },
    {
      id: 'approvals',
      label: t('approval.approvals'),
      text: t('approval.approvalsOf', {
        recorded: value.approvalsRecorded,
        required: value.approvalsRequired,
      }),
    },
  ]
})

const historyRows = computed<HistoryRow[]>(() =>
  status.data.value && records.data.value
    ? buildHistoryRows(status.data.value, records.data.value, versions.data.value ?? [])
    : [],
)

const versionRows = computed(() => summariseVersions(versions.data.value ?? []))
const changeRows = computed(() => summariseChanges(changes.data.value ?? []))

const historyColumns = computed<GridColumn[]>(() => [
  { field: 'version', title: t('approval.col.version'), mono: true, width: 105 },
  {
    field: 'change',
    title: t('approval.col.change'),
    width: 120,
    format: (value) => changeLabels[value as ChangeKind],
  },
  {
    field: 'decision',
    title: t('approval.col.decision'),
    width: 130,
    badge: (value): GridBadge =>
      value === ''
        ? { label: t('approval.awaitingDecision'), tone: 'warning' }
        : { label: String(value), tone: value === 'Rejected' ? 'error' : 'neutral' },
  },
  { field: 'by', title: t('approval.col.by'), width: 150 },
  {
    field: 'when',
    title: t('approval.col.when'),
    width: 170,
    mono: true,
    format: (value) => (value ? dateTime(String(value)) : ''),
  },
  {
    field: 'note',
    title: t('approval.col.checkerNote'),
    minWidth: 220,
    input: (row) =>
      (row as HistoryRow).pending
        ? {
            placeholder: t('approval.notePlaceholder'),
            label: t('approval.col.checkerNote'),
            value: checkerNote.value,
          }
        : null,
  },
  {
    field: 'returnNote',
    title: t('approval.col.returnNote'),
    minWidth: 220,
    input: (row) =>
      (row as HistoryRow).currentRejected
        ? {
            placeholder: t('approval.notePlaceholder'),
            label: t('approval.col.returnNote'),
            value: returnNote.value,
            prefix: (row as HistoryRow).returnNote || undefined,
          }
        : null,
  },
  {
    field: '',
    title: t('approval.col.actions'),
    width: 200,
    pinned: true,
    actions: (row) => {
      const history = row as HistoryRow

      if (history.pending) {
        return [
          { id: 'approve', label: t('approval.approve'), tone: 'success' },
          { id: 'reject', label: t('approval.reject'), tone: 'error' },
        ]
      }

      if (history.currentRejected) {
        return [
          { id: 'resubmit', label: t('approval.resubmit'), tone: 'primary' },
          ...(hasBaseline.value ? [{ id: 'retract', label: t('approval.retract') }] : []),
        ]
      }

      return []
    },
  },
])

const versionColumns = computed<GridColumn[]>(() => [
  { field: 'version', title: t('approval.col.version'), mono: true, width: 105 },
  { field: 'by', title: t('approval.col.by'), width: 150 },
  {
    field: 'when',
    title: t('approval.col.when'),
    width: 170,
    mono: true,
    format: (value) => (value ? dateTime(String(value)) : ''),
  },
  {
    field: '',
    title: t('approval.col.actions'),
    width: 120,
    pinned: true,
    actions: () => [{ id: 'compare', label: t('approval.compare') }],
  },
])

const changeColumns = computed<GridColumn[]>(() => [
  { field: 'field', title: t('approval.col.field'), width: 200 },
  { field: 'earlier', title: t('approval.col.earlier') },
  { field: 'version', title: t('approval.col.thisVersion') },
])

const act = useMutation({
  mutationFn: async (variables: { type: 'Approve' | 'Reject' | 'Resubmit' | 'Retract' }) => {
    const note = (
      variables.type === 'Approve' || variables.type === 'Reject' ? checkerNote : returnNote
    ).value.trim()

    if (variables.type === 'Approve' || variables.type === 'Reject') {
      await approvalApi.decide(
        target.value!,
        variables.type,
        status.data.value!.currentVersion,
        note || null,
      )
    } else {
      await approvalApi.act(target.value!, variables.type, note || null)
    }

    return variables.type
  },
  onSuccess: async (type) => {
    reasons.value = []
    checkerNote.value = ''
    returnNote.value = ''
    toasts.success(doneLabels[type])
    await queryClient.invalidateQueries({ queryKey: base.value })
    emit('changed')
  },
  onError: (error, variables) => {
    if (error instanceof ApiError && error.kind === 'validation') {
      reasons.value = refusalReasons(error.body, t('approval.refused', { action: variables.type }))

      return
    }

    toasts.apiError(error)
  },
})

function onHistoryAction(event: GridActionEvent): void {
  if (event.id === 'approve') act.mutate({ type: 'Approve' })
  else if (event.id === 'reject') act.mutate({ type: 'Reject' })
  else if (event.id === 'resubmit') act.mutate({ type: 'Resubmit' })
  else if (event.id === 'retract') act.mutate({ type: 'Retract' })
}

function onHistoryInput(event: GridInputEvent): void {
  if (event.field === 'note') checkerNote.value = event.value
  else returnNote.value = event.value
}

function onVersionAction(event: GridActionEvent): void {
  if (event.id === 'compare') compared.value = event.row as VersionSummary
}
</script>

<template>
  <section
    v-if="loaded && state"
    class="flex flex-col gap-4"
    :aria-label="t('approval.title')"
    data-testid="approval-panel"
  >
    <header class="flex flex-wrap items-center gap-3">
      <h3 class="text-base font-semibold">{{ t('approval.title') }}</h3>
      <span class="badge badge-sm font-mono" :class="stateTone[state]" data-testid="approval-state">
        {{ stateLabels[state] }}
      </span>
    </header>

    <dl
      class="grid grid-cols-2 gap-x-6 gap-y-2 text-sm md:grid-cols-4"
      data-testid="approval-facts"
    >
      <div v-for="fact in facts" :key="fact.id" class="flex flex-col">
        <dt class="label">{{ fact.label }}</dt>
        <dd class="font-mono tabular-nums">{{ fact.text }}</dd>
      </div>
    </dl>

    <ProblemAlert v-if="reasons.length > 0" :reasons="reasons" data-testid="approval-reasons" />

    <section class="flex flex-col gap-2">
      <h4 class="text-sm font-semibold">{{ t('approval.history') }}</h4>
      <p v-if="records.isError.value" class="text-sm" data-testid="approval-history-denied">
        {{ t('approval.historyDenied') }}
      </p>
      <DataGrid
        v-else-if="records.isSuccess.value"
        :columns="historyColumns"
        :rows="historyRows"
        :direction="direction"
        :empty-text="t('approval.historyEmpty')"
        :label="t('approval.history')"
        @action="onHistoryAction"
        @input="onHistoryInput"
      />
    </section>

    <section class="flex flex-col gap-2">
      <h4 class="text-sm font-semibold">{{ t('approval.versions') }}</h4>
      <p v-if="versions.isError.value" class="text-sm" data-testid="approval-versions-denied">
        {{ t('approval.versionsDenied') }}
      </p>
      <DataGrid
        v-else-if="versions.isSuccess.value"
        :columns="versionColumns"
        :rows="versionRows"
        :direction="direction"
        :empty-text="t('approval.versionsEmpty')"
        :label="t('approval.versions')"
        @action="onVersionAction"
      />
    </section>

    <section
      v-if="compared"
      class="flex flex-col gap-2"
      :aria-label="t('approval.changesTitle', { version: compared.version })"
      data-testid="approval-changes"
    >
      <h4 class="text-sm font-semibold">
        {{ t('approval.changesTitle', { version: compared.version }) }}
      </h4>
      <p v-if="compared.previousId === null" class="text-sm" data-testid="approval-first">
        {{ t('approval.firstVersion') }}
      </p>
      <p v-else-if="changes.isError.value" class="text-sm">{{ t('approval.changesFailed') }}</p>
      <DataGrid
        v-else-if="changes.isSuccess.value"
        :key="compared.id"
        :columns="changeColumns"
        :rows="changeRows"
        :direction="direction"
        :empty-text="t('approval.changesEmpty')"
        :label="t('approval.changesTitle', { version: compared.version })"
      />
    </section>
  </section>
</template>
