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
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQueryClient} from '@tanstack/vue-query'
import {ArrowRightLeft, ChevronsUp} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import {handleChecklistRefusal} from '../checklist/refusal'
import ChecklistGateBanner from '../checklist/ChecklistGateBanner.vue'
import {useChecklistGate} from '../checklist/useCaseChecklist'
import DecideDialog from './DecideDialog.vue'
import {escalationTarget} from './decide'
import {closedStatus, operationsApi, type CaseRecord} from './operationsApi'
import type {CaseDetail, CaseStatusChoice} from './types'

const props = defineProps<{
  open: boolean
  detail: CaseDetail
  statuses: CaseStatusChoice[]
  editable: boolean
  userName: string
  canOverrideChecklist?: boolean
}>()

const emit = defineEmits<{ close: []; openChecklist: []; overrideChecklist: [] }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const status = ref(props.detail.caseWorkflowStatusGuid)

const {gate} = useChecklistGate(
  () => props.detail.id,
  () => ({
    open: props.detail.closedStatusId !== closedStatus.closed,
    mine: props.detail.locked && props.detail.lockedUser === props.userName,
    permitted: props.canOverrideChecklist ?? false,
  }),
)

watch(
  () => props.open,
  (open) => {
    if (open) {
      status.value = props.detail.caseWorkflowStatusGuid
    }
  },
)

const current = computed(() =>
  props.statuses.find((choice) => choice.guid === props.detail.caseWorkflowStatusGuid),
)
const target = computed(() => escalationTarget(props.statuses, props.detail.caseWorkflowStatusGuid))
const statusChanged = computed(() => status.value !== props.detail.caseWorkflowStatusGuid)
const gated = computed(() => gate.value.blocked)

const change = useMutation({
  mutationFn: (patch: Partial<CaseRecord>) => operationsApi.change(props.detail.id, patch),
  onSuccess: () => {
    toasts.success(t('caseDetail.decide.saved'))
    void client.invalidateQueries({queryKey: ['service', 'Case']})
    emit('close')
  },
  onError: (error) => {
    const handled = handleChecklistRefusal(error, {
      client,
      error: toasts.error,
      fallback: t('caseDetail.checklist.gate.refused'),
    })

    if (!handled) toasts.apiError(error)
  },
})

function save(): void {
  change.mutate({caseWorkflowStatusGuid: status.value})
}

function escalate(): void {
  if (target.value) change.mutate({caseWorkflowStatusGuid: target.value.guid})
}
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.decide.statusTitle')"
    :icon="ArrowRightLeft"
    testid="case-status-dialog"
    @close="emit('close')"
  >
    <ChecklistGateBanner
      :names="gate.names"
      :override="gate.override"
      :can-override="gate.canOverride"
      testid="case-status-gate"
      @open-checklist="emit('openChecklist')"
      @override="emit('overrideChecklist')"
    />

    <div class="flex items-center justify-between gap-3" data-testid="case-decide-status">
      <span class="text-sm text-base-content/70">{{ t('caseDetail.decide.currentStatus') }}</span>
      <span class="badge badge-sm font-mono">{{ current?.name ?? '' }}</span>
    </div>

    <div class="flex items-center justify-between gap-3 rounded-box border border-base-300 p-3">
      <div class="flex min-w-0 flex-col">
        <span class="text-sm font-medium">{{ t('caseDetail.decide.escalate') }}</span>
        <span v-if="target" key="target" class="truncate text-xs text-base-content/70">
          {{ t('caseDetail.decide.escalateTo', {status: target.name ?? ''}) }}
        </span>
        <span v-else key="none" class="text-xs text-base-content/70">
          {{ t('caseDetail.decide.noEscalation') }}
        </span>
      </div>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!target || !editable || gated || change.isPending.value"
        data-testid="case-status-escalate"
        @click="escalate()"
      >
        <ChevronsUp class="size-4" aria-hidden="true"/>
        {{ t('caseDetail.decide.escalate') }}
      </button>
    </div>

    <fieldset class="fieldset">
      <legend class="fieldset-legend">{{ t('caseDetail.decide.moveTo') }}</legend>
      <ul class="flex flex-col gap-1">
        <li v-for="choice in statuses" :key="choice.guid">
          <label
            class="flex cursor-pointer items-center gap-3 rounded-field border border-base-300 px-3 py-2 text-sm"
          >
            <input
              v-model="status"
              type="radio"
              name="case-status-choice"
              class="radio radio-sm"
              :value="choice.guid"
              :disabled="!editable"
              :data-testid="`case-status-option-${choice.guid}`"
            />
            <span class="flex-1">{{ choice.name }}</span>
          </label>
        </li>
      </ul>
    </fieldset>

    <div class="flex justify-end gap-2">
      <button type="button" class="btn btn-sm" @click="emit('close')">
        {{ t('caseDetail.decide.cancel') }}
      </button>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!statusChanged || gated || change.isPending.value"
        data-testid="case-status-move"
        @click="save()"
      >
        {{ t('caseDetail.decide.saveChanges') }}
      </button>
    </div>
  </DecideDialog>
</template>
