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
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {useRoute, useRouter} from 'vue-router'
import {
  ArrowRightLeft,
  Banknote,
  CalendarClock,
  FileText,
  Lock,
  LockOpen,
  NotebookPen,
  Search,
  RotateCcw,
  ShieldCheck,
  ShieldOff,
  Zap,
  CircleX,
} from 'lucide-vue-next'
import ConfirmDialog from '../../components/ConfirmDialog.vue'
import FieldHelp from '../../components/FieldHelp.vue'
import {useToasts} from '../../stores/toasts'
import {suppressionApi} from '../../suppression/api'
import DecideSuppressDialog from '../../suppression/DecideSuppressDialog.vue'
import DecideSuppressionApprovalDialog from '../../suppression/DecideSuppressionApprovalDialog.vue'
import CaseDiaryPanel from '../CaseDiaryPanel.vue'
import {checklistKeys} from '../checklist/api'
import ChecklistGateBanner from '../checklist/ChecklistGateBanner.vue'
import ChecklistOverrideDialog from '../checklist/ChecklistOverrideDialog.vue'
import ChecklistTile from '../checklist/ChecklistTile.vue'
import {progressOf} from '../checklist/gate'
import {handleChecklistRefusal} from '../checklist/refusal'
import {useChecklistGate} from '../checklist/useCaseChecklist'
import CaseAgentRecommendation from './CaseAgentRecommendation.vue'
import CaseLinks from './CaseLinks.vue'
import CaseReports from './CaseReports.vue'
import CaseLegalHold from './CaseLegalHold.vue'
import CaseQaReview from './CaseQaReview.vue'
import CaseTags from './CaseTags.vue'
import DecideChecklistDialog from './DecideChecklistDialog.vue'
import DecideDialog from './DecideDialog.vue'
import DecideHoldDialog from './DecideHoldDialog.vue'
import DecideFormsDialog from './DecideFormsDialog.vue'
import DecideNoteDialog from './DecideNoteDialog.vue'
import DecideResearchDialog from './DecideResearchDialog.vue'
import DecideStatusDialog from './DecideStatusDialog.vue'
import DecideTile from './DecideTile.vue'
import {caseLabel} from '../labels'
import type {CaseRow} from '../types'
import {caseDetailApi} from './api'
import {closedStatus, holdState, operationsApi} from './operationsApi'
import type {CaseDetail} from './types'

const props = withDefaults(
  defineProps<{
    detail: CaseDetail
    row: CaseRow | null
    userName: string
    canSuppress?: boolean
    canForce?: boolean
    canOverrideChecklist?: boolean
  }>(),
  {canSuppress: false, canForce: false, canOverrideChecklist: false},
)

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const route = useRoute()
const router = useRouter()

const workflow = computed(() => props.detail.caseWorkflowGuid)
const open = computed(() => props.detail.closedStatusId !== closedStatus.closed)
const mine = computed(() => props.detail.locked && props.detail.lockedUser === props.userName)
const heldByOther = computed(() => props.detail.locked && !mine.value)

type Panel =
  | 'note'
  | 'research'
  | 'status'
  | 'diary'
  | 'forms'
  | 'hold'
  | 'suppress'
  | 'suppression'
  | 'checklist'
  | 'override'
  | null

const panel = ref<Panel>(null)
const formId = ref(0)
const confirmClose = ref(false)
const closing = ref(false)
const closeReasonIds = [1, 2, 3, 4, 5, 6, 7, 8]
const closeReasonId = ref(0)
const closeNote = ref('')

const {checklist, gate} = useChecklistGate(
  () => props.detail.id,
  () => ({open: open.value, mine: mine.value, permitted: props.canOverrideChecklist}),
)

const progress = computed(() => progressOf(checklist.data.value))
const breached = computed(() => checklist.data.value?.breached ?? 0)

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

const priorities = useQuery({
  queryKey: computed(() => ['service', 'Case', 'priorities', workflow.value] as const),
  queryFn: () => operationsApi.priorities(workflow.value),
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

const suppressions = useQuery({
  queryKey: computed(() => ['service', 'CaseSuppression', 'byCase', props.detail.id] as const),
  queryFn: () => suppressionApi.listByCase(props.detail.id),
  staleTime: 15_000,
  retry: false,
})

const actionable = computed(() =>
  (suppressions.data.value ?? []).filter(
    (item) => item.canDecide || item.canRetract || item.canResubmit,
  ),
)

const approvalId = ref<number | null>(null)

const requestedId = computed(() => {
  const raw = Array.isArray(route.query.suppression)
    ? route.query.suppression[0]
    : route.query.suppression
  const id = Number(raw)

  return Number.isInteger(id) && id > 0 ? id : null
})

watch(
  () => [requestedId.value, suppressions.data.value] as const,
  ([id, list]) => {
    if (id === null || !list || panel.value === 'suppression') return

    if (list.some((item) => item.id === id)) {
      approvalId.value = id
      panel.value = 'suppression'
    }
  },
  {immediate: true},
)

function closeApproval(): void {
  panel.value = null
  approvalId.value = null

  if (requestedId.value !== null) {
    void router.replace({query: {...route.query, suppression: undefined}})
  }
}

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
  void client.invalidateQueries({queryKey: ['service', 'Case']})
  void client.invalidateQueries({queryKey: checklistKeys.all})
}

function openChecklist(): void {
  confirmClose.value = false
  panel.value = 'checklist'
}

function openOverride(): void {
  confirmClose.value = false
  panel.value = 'override'
}

const change = useMutation({
  mutationFn: (patch: Parameters<typeof operationsApi.change>[1]) =>
    operationsApi.change(props.detail.id, patch),
  onSuccess: () => {
    toasts.success(t('caseDetail.decide.saved'))
    refresh()
  },
  onError: (error) => {
    const handled = handleChecklistRefusal(error, {
      client,
      error: toasts.error,
      fallback: t('caseDetail.checklist.gate.refused'),
    })

    if (handled) confirmClose.value = closing.value
    else toasts.apiError(error)
  },
  onSettled: () => {
    closing.value = false
  },
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
  change.mutate({locked: true, lockedUser: props.userName})
}

function release(): void {
  change.mutate({locked: false, lockedUser: null})
}

function prefillClose(note: string): void {
  if (!open.value) return

  if (closeNote.value.trim() === '') closeNote.value = note
  confirmClose.value = true
}

function close(): void {
  confirmClose.value = false
  closing.value = true
  change.mutate({
    closedStatusId: closedStatus.closed,
    closedReasonId: closeReasonId.value,
    closedReasonNote: closeNote.value.trim() === '' ? null : closeNote.value.trim(),
  })
}

function reopen(): void {
  change.mutate({closedStatusId: closedStatus.reopened})
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
      <FieldHelp :label="t('caseDetail.decide.title')" :text="t('caseDetail.help.decide.title')"/>
    </div>

    <section
      class="flex items-center gap-3 rounded-box bg-base-200 p-3"
      data-testid="case-decide-owner"
    >
      <span class="grid size-9 shrink-0 place-items-center rounded-field bg-base-100">
        <Lock v-if="detail.locked" class="size-5" aria-hidden="true"/>
        <LockOpen v-else class="size-5" aria-hidden="true"/>
      </span>
      <div class="flex min-w-0 flex-1 flex-col">
        <span v-if="mine" key="mine" class="text-sm font-medium">
          {{ t('caseDetail.decide.heldByYou') }}
        </span>
        <span v-else-if="heldByOther" key="other" class="truncate text-sm font-medium">
          {{ t('caseDetail.header.heldBy', {user: detail.lockedUser ?? ''}) }}
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
        :title="t('caseDetail.decide.tile.take.tip')"
        data-testid="case-take"
        @click="take()"
      >
        {{ t('caseDetail.decide.tile.take.label') }}
      </button>
      <button
        v-else-if="mine"
        key="release"
        type="button"
        class="btn btn-sm"
        :disabled="change.isPending.value"
        :title="t('caseDetail.decide.tile.release.tip')"
        data-testid="case-release"
        @click="release()"
      >
        {{ t('caseDetail.decide.tile.release.label') }}
      </button>
    </section>

    <div class="flex flex-col gap-2" data-testid="case-decide-actions">
      <DecideTile
        :icon="NotebookPen"
        :label="t('caseDetail.decide.tile.note.label')"
        :tip="t('caseDetail.decide.tile.note.tip')"
        testid="case-decide-tab-note"
        @select="panel = 'note'"
      />
      <DecideTile
        :icon="Search"
        :label="t('caseDetail.decide.tile.research.label')"
        :tip="t('caseDetail.decide.tile.research.tip')"
        testid="case-decide-tab-research"
        @select="panel = 'research'"
      />
      <DecideTile
        v-if="canSuppress"
        key="suppress"
        :icon="ShieldOff"
        :label="t('caseDetail.decide.tile.override.label')"
        :tip="t('caseDetail.decide.tile.override.tip')"
        testid="case-decide-tab-suppress"
        @select="panel = 'suppress'"
      />
      <DecideTile
        v-if="actionable.length > 0"
        key="suppression"
        :icon="ShieldCheck"
        :label="t('caseDetail.decide.tile.overrideAwaiting.label')"
        :tip="t('caseDetail.decide.tile.overrideAwaiting.tip')"
        :hint="t('suppression.tile.hint', { n: actionable.length }, actionable.length)"
        testid="case-decide-tab-suppression"
        @select="panel = 'suppression'"
      />
      <DecideTile
        :icon="ArrowRightLeft"
        :label="t('caseDetail.decide.tile.status.label')"
        :tip="t('caseDetail.decide.tile.status.tip')"
        :hint="statusName"
        testid="case-decide-tab-status"
        @select="panel = 'status'"
      />
      <ChecklistTile
        :done="progress.done"
        :required="progress.required"
        :breached="breached"
        @select="panel = 'checklist'"
      />
      <DecideTile
        v-if="row"
        key="diary"
        :icon="CalendarClock"
        :label="t('caseDetail.decide.tile.diary.label')"
        :tip="t('caseDetail.decide.tile.diary.tip')"
        testid="case-decide-tab-diary"
        @select="panel = 'diary'"
      />
      <DecideTile
        v-if="heldCount > 0"
        key="hold"
        :icon="Banknote"
        :label="t('caseDetail.decide.tile.hold.label')"
        :tip="t('caseDetail.decide.tile.hold.tip')"
        :hint="t('caseDetail.hold.tileHint', { n: heldCount })"
        testid="case-decide-tab-hold"
        @select="panel = 'hold'"
      />
      <DecideTile
        v-for="form in forms.data.value ?? []"
        :key="`form-${form.id}`"
        :icon="FileText"
        :label="form.name ?? ''"
        :tip="t('caseDetail.decide.tile.form', { name: form.name ?? '' })"
        :testid="`case-form-${form.id}`"
        @select="openForm(form.id)"
      />
      <DecideTile
        v-for="macro in macros.data.value ?? []"
        :key="macro.id"
        :icon="Zap"
        :label="macro.name ?? ''"
        :hint="macro.description ?? ''"
        :tip="macro.description ?? ''"
        :disabled="runMacro.isPending.value"
        :testid="`case-macro-${macro.id}`"
        @select="runMacro.mutate(macro.id)"
      />
    </div>

    <CaseTags :case-id="detail.id"/>

    <CaseAgentRecommendation :case-id="detail.id" @accepted="prefillClose"/>
    <CaseQaReview :case-id="detail.id"/>
    <CaseReports :case-id="detail.id"/>
    <CaseLegalHold :case-id="detail.id"/>
    <CaseLinks :case-id="detail.id"/>

    <div class="border-t border-base-300 pt-3">
      <DecideTile
        v-if="open"
        key="close"
        :icon="CircleX"
        :label="t('caseDetail.decide.tile.close.label')"
        :tip="t('caseDetail.decide.tile.close.tip')"
        tone="error"
        :disabled="change.isPending.value"
        testid="case-close"
        @select="confirmClose = true"
      />
      <DecideTile
        v-else
        key="reopen"
        :icon="RotateCcw"
        :label="t('caseDetail.decide.tile.reopen.label')"
        :tip="t('caseDetail.decide.tile.reopen.tip')"
        :disabled="change.isPending.value"
        testid="case-reopen"
        @select="reopen()"
      />
    </div>

    <DecideNoteDialog
      :open="panel === 'note'"
      :detail="detail"
      :actions="actions.data.value ?? []"
      :priorities="priorities.data.value ?? []"
      @close="panel = null"
    />
    <DecideResearchDialog :open="panel === 'research'" :detail="detail" @close="panel = null"/>
    <DecideSuppressDialog
      v-if="canSuppress"
      key="suppressDialog"
      :open="panel === 'suppress'"
      :detail="detail"
      :can-force="canForce"
      @close="panel = null"
    />
    <DecideSuppressionApprovalDialog
      :open="panel === 'suppression'"
      :items="actionable"
      :initial-id="approvalId"
      @close="closeApproval()"
    />
    <DecideStatusDialog
      :open="panel === 'status'"
      :detail="detail"
      :statuses="statuses.data.value ?? []"
      :editable="open"
      :user-name="userName"
      :can-override-checklist="canOverrideChecklist"
      @close="panel = null"
      @open-checklist="openChecklist()"
      @override-checklist="openOverride()"
    />
    <DecideChecklistDialog
      :open="panel === 'checklist'"
      :detail="detail"
      :user-name="userName"
      @close="panel = null"
    />
    <ChecklistOverrideDialog
      :open="panel === 'override'"
      :case-id="detail.id"
      @close="panel = null"
    />
    <DecideDialog
      :open="panel === 'diary'"
      :title="t('caseDetail.decide.diaryTitle')"
      :icon="CalendarClock"
      testid="case-diary-dialog"
      @close="panel = null"
    >
      <CaseDiaryPanel v-if="row" :row="row"/>
    </DecideDialog>
    <DecideHoldDialog :open="panel === 'hold'" :detail="detail" @close="panel = null"/>
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
      :confirm-disabled="gate.blocked"
      @confirm="close()"
      @cancel="confirmClose = false"
    >
      <div class="flex flex-col gap-2 pb-2" data-testid="case-close-reason">
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('caseDetail.decide.closeReason') }}</span>
          <select
            v-model="closeReasonId"
            class="select select-sm w-full"
            data-testid="case-close-reason-select"
          >
            <option :value="0">{{ t('caseDetail.decide.closeReasonNone') }}</option>
            <option v-for="id in closeReasonIds" :key="id" :value="id">
              {{ t(`caseDetail.decide.closeReasons.${id}`) }}
            </option>
          </select>
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('caseDetail.decide.closeNote') }}</span>
          <textarea
            v-model="closeNote"
            class="textarea w-full"
            rows="2"
            maxlength="1000"
            data-testid="case-close-note"
          ></textarea>
        </label>
      </div>
      <ChecklistGateBanner
        :names="gate.names"
        :override="gate.override"
        :can-override="gate.canOverride"
        testid="case-close-gate"
        @open-checklist="openChecklist()"
        @override="openOverride()"
      />
    </ConfirmDialog>
  </aside>
</template>
