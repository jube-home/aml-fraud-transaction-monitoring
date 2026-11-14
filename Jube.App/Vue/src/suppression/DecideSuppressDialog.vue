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
import {useRoute} from 'vue-router'
import {ShieldOff} from 'lucide-vue-next'
import OverrideManager from '../components/OverrideManager.vue'
import ProblemAlert from '../components/ProblemAlert.vue'
import DecideDialog from '../cases/detail/DecideDialog.vue'
import type {CaseDetail} from '../cases/detail/types'
import {parseJournalState} from '../cases/journal/state'
import {fromLocalInput, isFuture, toLocalInput} from '../composables/localDateTime'
import {overrideApi} from '../overrides/api'
import {createOverrideSelection} from '../overrides/selection'
import {useToasts} from '../stores/toasts'
import {suppressionApi} from './api'
import SuppressConfirmStep from './SuppressConfirmStep.vue'
import SuppressContextStrip from './SuppressContextStrip.vue'
import SuppressEvidence from './SuppressEvidence.vue'
import SuppressKeyStep from './SuppressKeyStep.vue'
import SuppressValueStep from './SuppressValueStep.vue'
import {defaultExpiry, mergeParameter, path, refusalText} from './state'
import type {SuppressionRequest} from './types'

type Step = 'key' | 'values' | 'transactions' | 'targets' | 'confirm'

const props = defineProps<{ open: boolean; detail: CaseDetail; canForce: boolean }>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()
const route = useRoute()
const client = useQueryClient()
const toasts = useToasts()

const step = ref<Step>('key')
const keyName = ref('')
const value = ref('')
const ticked = ref<number[]>([])
const tickedDefaulted = ref(false)
const showEvidence = ref(false)
const reason = ref('')
const reasonEdited = ref(false)
const expiry = ref('')
const problem = ref('')
const expanded = ref('')
const selection = createOverrideSelection()

const caseId = computed(() => props.detail.id)
const caseKeyValue = computed(() => props.detail.caseKeyValue ?? '')
const merge = computed(() => mergeParameter(parseJournalState(route.query).merge))

function reset(): void {
  step.value = 'key'
  keyName.value = ''
  value.value = ''
  ticked.value = []
  tickedDefaulted.value = false
  showEvidence.value = false
  reason.value = ''
  reasonEdited.value = false
  expiry.value = toLocalInput(defaultExpiry())
  problem.value = ''
  expanded.value = ''
  selection.targets.value = []
}

watch(
  () => props.open,
  (open) => {
    if (open) reset()
  },
  {immediate: true},
)

const keys = useQuery({
  queryKey: computed(() => ['service', 'CaseSuppression', 'keys', caseId.value] as const),
  queryFn: () => suppressionApi.keys(caseId.value),
  enabled: computed(() => props.open),
  retry: false,
})

const isCaseKey = computed(
  () => (keys.data.value ?? []).find((entry) => entry.name === keyName.value)?.isCaseKey ?? false,
)

const resolvedValue = computed(() => (isCaseKey.value ? caseKeyValue.value : value.value))

const values = useQuery({
  queryKey: computed(
    () =>
      ['service', 'CaseSuppression', 'values', caseId.value, keyName.value, merge.value] as const,
  ),
  queryFn: () => suppressionApi.values(caseId.value, keyName.value, merge.value),
  enabled: computed(() => props.open && keyName.value !== '' && step.value !== 'key'),
  retry: false,
})

const transactions = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'CaseSuppression',
        'transactions',
        caseId.value,
        keyName.value,
        resolvedValue.value,
        merge.value,
      ] as const,
  ),
  queryFn: () =>
    suppressionApi.transactions(caseId.value, keyName.value, resolvedValue.value, merge.value),
  enabled: computed(
    () => props.open && keyName.value !== '' && resolvedValue.value !== '' && step.value !== 'key',
  ),
  retry: false,
})

const models = useQuery({
  queryKey: computed(
    () =>
      [
        'override',
        'models',
        {overrideKey: keyName.value, overrideKeyValue: resolvedValue.value},
      ] as const,
  ),
  queryFn: () =>
    overrideApi.models({overrideKey: keyName.value, overrideKeyValue: resolvedValue.value}),
  enabled: computed(() => props.open && step.value === 'confirm'),
  retry: false,
})

watch(
  () => transactions.data.value,
  (rows) => {
    if (!rows || isCaseKey.value || tickedDefaulted.value) return

    ticked.value = rows.map((row) => row.archiveId)
    tickedDefaulted.value = true
  },
)

const current = computed(() =>
  (values.data.value ?? []).find((entry) => entry.value === resolvedValue.value),
)

const names = computed<Record<string, string>>(() =>
  Object.fromEntries(
    (models.data.value ?? []).map((model) => [model.entityAnalysisModelGuid, model.name]),
  ),
)

const steps = computed<Step[]>(() =>
  isCaseKey.value
    ? ['key', 'targets', 'confirm']
    : ['key', 'values', 'transactions', 'targets', 'confirm'],
)

const expiryDate = computed(() => fromLocalInput(expiry.value))
const expiryInvalid = computed(() => expiry.value !== '' && !isFuture(expiryDate.value))

const caseCode = computed(() => `C-${props.detail.id}`)

function request(text: string): SuppressionRequest {
  return {
    caseId: caseId.value,
    pathId: isCaseKey.value ? path.caseKey : path.transaction,
    overrideKey: keyName.value,
    overrideKeyValue: isCaseKey.value ? null : value.value,
    reason: text,
    expiryDate: expiryDate.value?.toISOString() ?? null,
    targets: selection.targets.value.map((target) => ({
      entityAnalysisModelGuid: target.entityAnalysisModelGuid,
      ruleName: target.ruleName,
      overrideKindId: target.overrideKind,
    })),
    archiveIds: ticked.value,
    merge: merge.value,
  }
}

const draft = useMutation({
  mutationFn: () => suppressionApi.reasonDraft(request('-')),
  onSuccess: (result) => {
    if (!reasonEdited.value) reason.value = result.reason
  },
})

watch(
  () => [step.value, expiry.value] as const,
  ([next]) => {
    if (next === 'confirm' && !reasonEdited.value && !expiryInvalid.value) draft.mutate()
  },
)

const save = useMutation({
  mutationFn: () => suppressionApi.request(request(reason.value.trim())),
  onSuccess: () => {
    toasts.success(t('suppression.requested'))
    void client.invalidateQueries({queryKey: ['service']})
    emit('close')
  },
  onError: (error) => {
    problem.value = refusalText(error, t('suppression.failed'))
  },
})

function editReason(text: string): void {
  reason.value = text
  reasonEdited.value = true
}

function chooseKey(name: string): void {
  keyName.value = name
  value.value = ''
  tickedDefaulted.value = false
  ticked.value = []
  selection.targets.value = []
}

function toggle(archiveId: number, on: boolean): void {
  ticked.value = on
    ? [...new Set([...ticked.value, archiveId])]
    : ticked.value.filter((id) => id !== archiveId)
}

function chooseValue(next: string): void {
  value.value = next
  tickedDefaulted.value = false
  ticked.value = []
  step.value = 'transactions'
}

const canNext = computed(() => {
  if (step.value === 'key') {
    return (keys.data.value ?? []).some((entry) => entry.name === keyName.value && entry.enabled)
  }

  if (step.value === 'transactions') return ticked.value.length > 0

  if (step.value === 'targets') return selection.targets.value.length > 0

  return false
})

const canSave = computed(
  () =>
    reason.value.trim() !== '' &&
    expiry.value !== '' &&
    !expiryInvalid.value &&
    selection.targets.value.length > 0 &&
    !save.isPending.value,
)

function next(): void {
  problem.value = ''

  if (step.value === 'key') step.value = isCaseKey.value ? 'targets' : 'values'
  else if (step.value === 'transactions') step.value = 'targets'
  else if (step.value === 'targets') step.value = 'confirm'
}

function back(): void {
  problem.value = ''

  const list = steps.value
  const index = list.indexOf(step.value)

  if (index > 0) step.value = list[index - 1]!
}

function labelOf(id: Step): string {
  switch (id) {
    case 'key':
      return t('suppression.step.key')
    case 'values':
      return t('suppression.step.values')
    case 'transactions':
      return t('suppression.step.transactions')
    case 'targets':
      return t('suppression.step.targets')
    default:
      return t('suppression.step.confirm')
  }
}
</script>

<template>
  <DecideDialog
    :open="open"
    wide
    :title="t('suppression.title')"
    :icon="ShieldOff"
    testid="case-suppress-dialog"
    @close="emit('close')"
  >
    <ol class="steps steps-horizontal w-full" data-testid="suppress-steps">
      <li
        v-for="entry in steps"
        :key="entry"
        class="step text-xs"
        :class="steps.indexOf(entry) <= steps.indexOf(step) ? 'step-primary' : ''"
        :aria-current="entry === step ? 'step' : undefined"
      >
        {{ labelOf(entry) }}
      </li>
    </ol>

    <SuppressContextStrip
      :case-code="caseCode"
      :case-key="detail.caseKey ?? ''"
      :case-key-value="caseKeyValue"
      :override-key="step === 'key' || keyName === '' ? null : keyName"
      :override-value="step === 'key' || step === 'values' ? null : resolvedValue"
      :in-case="step === 'key' || step === 'values' ? null : (current?.count ?? null)"
      :first="current?.firstSeenDate ?? null"
      :last="current?.lastSeenDate ?? null"
      :wider-count-state-id="current?.widerCountStateId ?? null"
      :wider-keys="current?.widerKeys ?? null"
      :wider-transactions="current?.widerTransactions ?? null"
      :wider-capped="current?.widerCapped ?? false"
      :evidence-open="showEvidence"
      :evidence-count="transactions.data.value?.length ?? 0"
      @toggle="showEvidence = !showEvidence"
    />

    <SuppressEvidence
      v-if="showEvidence && step !== 'transactions'"
      key="evidence"
      :rows="transactions.data.value ?? []"
      :ticked="ticked"
      :tickable="false"
      :loading="transactions.isPending.value && transactions.fetchStatus.value !== 'idle'"
      :failed="transactions.isError.value"
    />

    <SuppressKeyStep
      v-if="step === 'key'"
      key="keys"
      :keys="keys.data.value ?? []"
      :loading="keys.isPending.value"
      :failed="keys.isError.value"
      :model-value="keyName"
      @update:model-value="chooseKey"
    />

    <SuppressValueStep
      v-else-if="step === 'values'"
      key="values"
      :values="values.data.value ?? []"
      :loading="values.isPending.value"
      :failed="values.isError.value"
      :override-key="keyName"
      @select="chooseValue"
    />

    <div v-else-if="step === 'transactions'" key="transactions" class="flex flex-col gap-2">
      <p class="text-sm">{{ t('suppression.step.transactionsHelp') }}</p>
      <SuppressEvidence
        :rows="transactions.data.value ?? []"
        :ticked="ticked"
        tickable
        :loading="transactions.isPending.value"
        :failed="transactions.isError.value"
        @toggle="toggle"
        @all="ticked = (transactions.data.value ?? []).map((row) => row.archiveId)"
        @none="ticked = []"
      />
    </div>

    <div v-else-if="step === 'targets'" key="targets" class="flex flex-col gap-2">
      <p class="text-sm">{{ t('suppression.step.targetsHelp') }}</p>
      <OverrideManager
        :override-key="keyName"
        :override-key-value="resolvedValue"
        :show-selector="false"
        :show-trail="false"
        :can-force="canForce"
        :selection="selection"
        :expanded-model="expanded"
        @expand="expanded = $event"
      />
    </div>

    <SuppressConfirmStep
      v-else
      key="confirm"
      :override-key="keyName"
      :override-value="resolvedValue"
      :targets="selection.targets.value"
      :names="names"
      :keys="keys.data.value ?? []"
      :reason="reason"
      :expiry="expiry"
      :drafting="draft.isPending.value"
      :expiry-invalid="expiryInvalid"
      @update:reason="editReason"
      @update:expiry="expiry = $event"
    />

    <ProblemAlert
      v-if="problem"
      key="problem"
      :reasons="[problem]"
      data-testid="suppress-problem"
    />

    <div class="flex flex-wrap justify-end gap-2">
      <button type="button" class="btn btn-sm" @click="emit('close')">
        {{ t('caseDetail.decide.cancel') }}
      </button>
      <button
        v-if="step !== 'key'"
        key="back"
        type="button"
        class="btn btn-sm"
        data-testid="suppress-back"
        @click="back()"
      >
        {{ t('suppression.back') }}
      </button>
      <button
        v-if="step !== 'confirm' && step !== 'values'"
        key="next"
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!canNext"
        data-testid="suppress-next"
        @click="next()"
      >
        {{ t('suppression.next') }}
      </button>
      <button
        v-if="step === 'confirm'"
        key="request"
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!canSave"
        data-testid="suppress-request"
        @click="save.mutate()"
      >
        {{ t('suppression.request') }}
      </button>
    </div>
  </DecideDialog>
</template>
