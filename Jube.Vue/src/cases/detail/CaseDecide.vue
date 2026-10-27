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
import { useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import {
  ArrowRightLeft,
  Banknote,
  CalendarClock,
  FileText,
  Lock,
  LockOpen,
  NotebookPen,
  RotateCcw,
  Zap,
  CircleX,
} from 'lucide-vue-next'
import ConfirmDialog from '../../components/ConfirmDialog.vue'
import FieldHelp from '../../components/FieldHelp.vue'
import { useToasts } from '../../stores/toasts'
import CaseDiaryPanel from '../CaseDiaryPanel.vue'
import CaseSoon from './CaseSoon.vue'
import DecideDialog from './DecideDialog.vue'
import DecideHoldDialog from './DecideHoldDialog.vue'
import DecideFormsDialog from './DecideFormsDialog.vue'
import DecideNoteDialog from './DecideNoteDialog.vue'
import DecideStatusDialog from './DecideStatusDialog.vue'
import DecideTile from './DecideTile.vue'
import { caseLabel } from '../labels'
import type { CaseRow } from '../types'
import { caseDetailApi } from './api'
import { closedStatus, holdState, operationsApi } from './operationsApi'
import type { CaseDetail } from './types'

const props = defineProps<{ detail: CaseDetail; row: CaseRow | null; userName: string }>()

const { t } = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const workflow = computed(() => props.detail.caseWorkflowGuid)
const open = computed(() => props.detail.closedStatusId !== closedStatus.closed)
const mine = computed(() => props.detail.locked && props.detail.lockedUser === props.userName)
const heldByOther = computed(() => props.detail.locked && !mine.value)

type Panel = 'note' | 'status' | 'diary' | 'forms' | 'hold' | null

const panel = ref<Panel>(null)
const formId = ref(0)
const confirmClose = ref(false)
const checks = ref<Record<string, boolean>>({ identity: false, source: false, sar: false })

const statuses = useQuery({
  queryKey: computed(() => ['service', 'Case', 'statuses', workflow.value] as const),
  queryFn: () => caseDetailApi.statuses(workflow.value),
  staleTime: 60_000,
})

const actions = useQuery({
  queryKey: computed(() => ['service', 'Case', 'actions', workflow.value] as const),
  queryFn: () => operationsApi.actions(workflow.value),
  staleTime: 60_000,
})

const macros = useQuery({
  queryKey: computed(() => ['service', 'Case', 'macros', workflow.value] as const),
  queryFn: () => operationsApi.macros(workflow.value),
  staleTime: 60_000,
})

const forms = useQuery({
  queryKey: computed(() => ['service', 'Case', 'forms', workflow.value] as const),
  queryFn: () => operationsApi.forms(workflow.value),
  staleTime: 60_000,
})

const holds = useQuery({
  queryKey: computed(() => ['service', 'Case', 'holds', props.detail.id] as const),
  queryFn: () => operationsApi.holds(props.detail.id),
  staleTime: 15_000,
  retry: false,
})

const heldCount = computed(
  () => (holds.data.value ?? []).filter((hold) => hold.stateId === holdState.held).length,
)

function openForm(id: number): void {
  formId.value = id
  panel.value = 'forms'
}

const formName = computed(
  () => (forms.data.value ?? []).find((choice) => choice.id === formId.value)?.name ?? '',
)

const statusName = computed(
  () =>
    (statuses.data.value ?? []).find(
      (choice) => choice.guid === props.detail.caseWorkflowStatusGuid,
    )?.name ?? '',
)

function refresh(): void {
  void client.invalidateQueries({ queryKey: ['service', 'Case'] })
}

const change = useMutation({
  mutationFn: (patch: Parameters<typeof operationsApi.change>[1]) =>
    operationsApi.change(props.detail.id, patch),
  onSuccess: () => {
    toasts.success(t('caseDetail.decide.saved'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const runMacro = useMutation({
  mutationFn: (macroId: number) => operationsApi.runMacro(props.detail.id, macroId),
  onSuccess: () => {
    toasts.success(t('caseDetail.decide.macroRan'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

function take(): void {
  change.mutate({ locked: true, lockedUser: props.userName })
}

function release(): void {
  change.mutate({ locked: false, lockedUser: null })
}

function close(): void {
  confirmClose.value = false
  change.mutate({ closedStatusId: closedStatus.closed })
}

function reopen(): void {
  change.mutate({ closedStatusId: closedStatus.reopened })
}
</script>

<template>
  <aside
    class="flex min-w-0 flex-col gap-4 rounded-box border border-base-300 p-4"
    :aria-label="t('caseDetail.decide.title')"
    data-testid="case-decide"
  >
    <div class="flex items-center gap-1">
      <h3 class="text-sm font-semibold">{{ t('caseDetail.decide.title') }}</h3>
      <FieldHelp :label="t('caseDetail.decide.title')" :text="t('caseDetail.help.decide.title')" />
    </div>

    <section
      class="flex items-center gap-3 rounded-box bg-base-200 p-3"
      data-testid="case-decide-owner"
    >
      <span class="grid size-9 shrink-0 place-items-center rounded-field bg-base-100">
        <Lock v-if="detail.locked" class="size-5" aria-hidden="true" />
        <LockOpen v-else class="size-5" aria-hidden="true" />
      </span>
      <div class="flex min-w-0 flex-1 flex-col">
        <span v-if="mine" key="mine" class="text-sm font-medium">
          {{ t('caseDetail.decide.heldByYou') }}
        </span>
        <span v-else-if="heldByOther" key="other" class="truncate text-sm font-medium">
          {{ t('caseDetail.header.heldBy', { user: detail.lockedUser ?? '' }) }}
        </span>
        <span v-else key="free" class="text-sm font-medium">{{ t('caseDetail.header.free') }}</span>
        <span v-if="heldByOther" key="supervisor" class="text-xs text-base-content/70">
          {{ t('caseDetail.decide.supervisor') }}
        </span>
      </div>
      <button
        v-if="!detail.locked"
        key="take"
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!open || change.isPending.value"
        data-testid="case-take"
        @click="take()"
      >
        {{ t('caseDetail.decide.take') }}
      </button>
      <button
        v-else-if="mine"
        key="release"
        type="button"
        class="btn btn-sm"
        :disabled="change.isPending.value"
        data-testid="case-release"
        @click="release()"
      >
        {{ t('caseDetail.decide.release') }}
      </button>
    </section>

    <div class="flex flex-col gap-2" data-testid="case-decide-actions">
      <DecideTile
        :icon="NotebookPen"
        :label="t('caseDetail.decide.noteTitle')"
        testid="case-decide-tab-note"
        @select="panel = 'note'"
      />
      <DecideTile
        :icon="ArrowRightLeft"
        :label="t('caseDetail.decide.statusTitle')"
        :hint="statusName"
        testid="case-decide-tab-status"
        @select="panel = 'status'"
      />
      <DecideTile
        v-if="row"
        key="diary"
        :icon="CalendarClock"
        :label="t('caseDetail.decide.diaryTitle')"
        testid="case-decide-tab-diary"
        @select="panel = 'diary'"
      />
      <DecideTile
        v-if="heldCount > 0"
        key="hold"
        :icon="Banknote"
        :label="t('caseDetail.hold.title')"
        :hint="t('caseDetail.hold.tileHint', { n: heldCount })"
        testid="case-decide-tab-hold"
        @select="panel = 'hold'"
      />
      <DecideTile
        v-for="form in forms.data.value ?? []"
        :key="`form-${form.id}`"
        :icon="FileText"
        :label="form.name ?? ''"
        :testid="`case-form-${form.id}`"
        @select="openForm(form.id)"
      />
      <DecideTile
        v-for="macro in macros.data.value ?? []"
        :key="macro.id"
        :icon="Zap"
        :label="macro.name ?? ''"
        :hint="macro.description ?? ''"
        :disabled="runMacro.isPending.value"
        :testid="`case-macro-${macro.id}`"
        @select="runMacro.mutate(macro.id)"
      />
    </div>

    <div class="border-t border-base-300 pt-3">
      <DecideTile
        v-if="open"
        key="close"
        :icon="CircleX"
        :label="t('caseDetail.decide.close')"
        tone="error"
        :disabled="change.isPending.value"
        testid="case-close"
        @select="confirmClose = true"
      />
      <DecideTile
        v-else
        key="reopen"
        :icon="RotateCcw"
        :label="t('caseDetail.decide.reopen')"
        :disabled="change.isPending.value"
        testid="case-reopen"
        @select="reopen()"
      />
    </div>

    <fieldset class="fieldset" data-testid="case-checklist">
      <legend class="fieldset-legend">{{ t('caseDetail.checklist.title') }}</legend>
      <label class="label gap-2 text-sm">
        <input v-model="checks.identity" type="checkbox" class="checkbox checkbox-sm" />
        {{ t('caseDetail.checklist.identity') }}
      </label>
      <label class="label gap-2 text-sm">
        <input v-model="checks.source" type="checkbox" class="checkbox checkbox-sm" />
        {{ t('caseDetail.checklist.source') }}
      </label>
      <label class="label gap-2 text-sm">
        <input v-model="checks.sar" type="checkbox" class="checkbox checkbox-sm" />
        {{ t('caseDetail.checklist.sar') }}
      </label>
    </fieldset>

    <details class="collapse collapse-arrow border border-base-300">
      <summary class="collapse-title min-h-0 py-2 text-sm font-medium">
        {{ t('caseDetail.decide.plannedTitle') }}
      </summary>
      <div class="collapse-content" data-testid="case-decide-planned">
        <CaseSoon area="decide" />
      </div>
    </details>

    <DecideNoteDialog
      :open="panel === 'note'"
      :detail="detail"
      :actions="actions.data.value ?? []"
      :statuses="statuses.data.value ?? []"
      @close="panel = null"
    />
    <DecideStatusDialog
      :open="panel === 'status'"
      :detail="detail"
      :statuses="statuses.data.value ?? []"
      :editable="open"
      @close="panel = null"
    />
    <DecideDialog
      :open="panel === 'diary'"
      :title="t('caseDetail.decide.diaryTitle')"
      :icon="CalendarClock"
      testid="case-diary-dialog"
      @close="panel = null"
    >
      <CaseDiaryPanel v-if="row" :row="row" />
    </DecideDialog>
    <DecideHoldDialog :open="panel === 'hold'" :detail="detail" @close="panel = null" />
    <DecideFormsDialog
      :open="panel === 'forms'"
      :detail="detail"
      :form-id="formId"
      :title="formName"
      @close="panel = null"
    />

    <ConfirmDialog
      id="case-close"
      :open="confirmClose"
      :title="t('caseDetail.decide.closeTitle', { id: caseLabel(detail.id) })"
      :body="t('caseDetail.decide.closeBody')"
      :confirm-label="t('caseDetail.decide.close')"
      :cancel-label="t('caseDetail.decide.cancel')"
      @confirm="close()"
      @cancel="confirmClose = false"
    />
  </aside>
</template>
