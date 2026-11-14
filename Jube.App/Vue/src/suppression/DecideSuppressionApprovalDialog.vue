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
import {RouterLink, useRoute} from 'vue-router'
import {ShieldCheck} from 'lucide-vue-next'
import ProblemAlert from '../components/ProblemAlert.vue'
import DecideDialog from '../cases/detail/DecideDialog.vue'
import {fromLocalInput, isFuture, toLocalInput} from '../composables/localDateTime'
import {useFormat} from '../composables/useFormat'
import {useToasts} from '../stores/toasts'
import {suppressionApi} from './api'
import SuppressionCounts from './SuppressionCounts.vue'
import SuppressionStanding from './SuppressionStanding.vue'
import {decision, defaultExpiry, isolate, refusalText, targetName} from './state'
import type {CaseSuppression, SuppressionDecision, SuppressionTarget} from './types'

const props = defineProps<{
  open: boolean
  items: CaseSuppression[]
  initialId: number | null
}>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()
const {dateTime} = useFormat()
const route = useRoute()
const client = useQueryClient()
const toasts = useToasts()

const selectedId = ref(0)
const note = ref('')
const resubmitExpiry = ref('')
const problem = ref('')

watch(
  () => [props.open, props.initialId] as const,
  ([open, initial]) => {
    if (!open) return

    selectedId.value = initial ?? props.items[0]?.id ?? 0
    note.value = ''
    problem.value = ''
    resubmitExpiry.value = toLocalInput(defaultExpiry())
  },
  {immediate: true},
)

const detail = useQuery({
  queryKey: computed(() => ['service', 'CaseSuppression', 'detail', selectedId.value] as const),
  queryFn: () => suppressionApi.get(selectedId.value),
  enabled: computed(() => props.open && selectedId.value > 0),
  retry: false,
})

const current = computed(() => detail.data.value ?? null)
const expiryDate = computed(() => fromLocalInput(resubmitExpiry.value))
const expiryInvalid = computed(() => resubmitExpiry.value !== '' && !isFuture(expiryDate.value))

function journalLink(caseId: number, archiveId: number) {
  return {
    path: `/cases/${caseId}`,
    query: {...route.query, section: 'journal', jt: String(archiveId)},
  }
}

function decide(run: (body: SuppressionDecision) => Promise<CaseSuppression>, done: string) {
  return useMutation({
    mutationFn: () =>
      run({
        id: selectedId.value,
        note: note.value.trim() === '' ? null : note.value.trim(),
        expiryDate: expiryDate.value?.toISOString() ?? null,
      }),
    onSuccess: () => {
      toasts.success(done)
      void client.invalidateQueries({queryKey: ['service']})
      emit('close')
    },
    onError: (error) => {
      problem.value = refusalText(error, t('suppression.failed'))
    },
  })
}

const approve = decide(suppressionApi.approve, t('suppression.approval.approved'))
const reject = decide(suppressionApi.reject, t('suppression.approval.rejected'))
const retract = decide(suppressionApi.retract, t('suppression.approval.retracted'))
const resubmit = decide(suppressionApi.resubmit, t('suppression.approval.resubmitted'))

const busy = computed(
  () =>
    approve.isPending.value ||
    reject.isPending.value ||
    retract.isPending.value ||
    resubmit.isPending.value,
)

function decisionText(id: number): string {
  switch (id) {
    case decision.approved:
      return t('suppression.approval.decision.approved')
    case decision.rejected:
      return t('suppression.approval.decision.rejected')
    case decision.retracted:
      return t('suppression.approval.decision.retracted')
    default:
      return t('suppression.approval.decision.resubmitted')
  }
}

function changeLine(item: SuppressionTarget): string {
  const values = {
    target: targetName(item.entityAnalysisModelName, item.ruleName),
    key: isolate(current.value?.overrideKey),
    value: isolate(current.value?.overrideKeyValue),
    date: dateTime(current.value?.expiryDate ?? ''),
  }

  return item.overrideKindId === 1
    ? t('suppression.approval.forceLine', values)
    : t('suppression.approval.suppressLine', values)
}
</script>

<template>
  <DecideDialog
    :open="open"
    wide
    :title="t('suppression.approval.title')"
    :icon="ShieldCheck"
    testid="case-suppression-approval-dialog"
    @close="emit('close')"
  >
    <fieldset v-if="items.length > 1" key="choose" class="fieldset">
      <label class="label" for="suppression-choose">{{ t('suppression.approval.choose') }}</label>
      <select
        id="suppression-choose"
        v-model.number="selectedId"
        class="select select-sm w-full"
        data-testid="suppression-choose"
      >
        <option v-for="item in items" :key="item.id" :value="item.id">
          {{
            t('suppression.pair', {
              key: isolate(item.overrideKey),
              value: isolate(item.overrideKeyValue),
            })
          }}
        </option>
      </select>
    </fieldset>

    <div v-if="detail.isPending.value" key="loading" class="skeleton h-40 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <p
      v-else-if="detail.isError.value || !current"
      key="failed"
      role="alert"
      class="text-sm text-error"
    >
      {{ t('suppression.approval.failedLoad') }}
    </p>
    <template v-else>
      <section
        class="flex flex-col gap-2 rounded-box border border-base-300 bg-base-200 p-3 text-sm text-base-content"
      >
        <div class="flex flex-wrap items-center gap-2">
          <span class="font-mono font-semibold" data-testid="suppression-approval-key">
            {{
              t('suppression.pair', {
                key: isolate(current.overrideKey),
                value: isolate(current.overrideKeyValue),
              })
            }}
          </span>
          <SuppressionStanding :standing="current.standingId"/>
          <span class="font-mono text-xs">
            {{
              t('suppression.card.requestedBy', {
                user: isolate(current.createdUser),
                date: dateTime(current.createdDate ?? current.expiryDate),
              })
            }}
          </span>
          <span v-if="current.stateId === 1" key="progress" class="text-xs">
            {{
              t('suppression.card.progress', {
                n: current.approvalsRecorded,
                total: current.approvalsRequired,
              })
            }}
          </span>
        </div>
        <div class="grid gap-3 sm:grid-cols-2">
          <SuppressionCounts
            :heading="t('suppression.card.then')"
            :counts="current.then"
            testid="suppression-approval-then"
          />
          <SuppressionCounts
            v-if="current.now"
            key="now"
            :heading="t('suppression.card.now')"
            :counts="current.now"
            testid="suppression-approval-now"
          />
        </div>
      </section>

      <section class="flex flex-col gap-1" :aria-label="t('suppression.approval.change')">
        <h4 class="text-sm font-semibold">{{ t('suppression.approval.change') }}</h4>
        <ul class="flex flex-col gap-1 text-sm" data-testid="suppression-approval-changes">
          <li v-for="item in current.targets" :key="item.id">
            {{ changeLine(item) }}
          </li>
        </ul>
      </section>

      <section class="flex flex-col gap-1" :aria-label="t('suppression.approval.reason')">
        <h4 class="text-sm font-semibold">{{ t('suppression.approval.reason') }}</h4>
        <p class="whitespace-pre-line text-sm" dir="auto" data-testid="suppression-approval-reason">
          {{ current.reason }}
        </p>
      </section>

      <section
        v-if="current.transactions.length > 0"
        key="evidence"
        class="flex flex-col gap-1"
        :aria-label="t('suppression.approval.evidence')"
      >
        <h4 class="text-sm font-semibold">{{ t('suppression.approval.evidence') }}</h4>
        <ul class="flex flex-wrap gap-2" data-testid="suppression-approval-evidence">
          <li v-for="item in current.transactions.slice(0, 30)" :key="item.id">
            <RouterLink
              class="badge badge-ghost badge-sm font-mono"
              :to="journalLink(current.caseId, item.archiveId)"
              @click="emit('close')"
            >
              {{ item.archiveId }}
            </RouterLink>
          </li>
        </ul>
      </section>

      <section
        v-if="current.approvals.length > 0"
        key="history"
        class="flex flex-col gap-1"
        :aria-label="t('suppression.approval.history')"
      >
        <h4 class="text-sm font-semibold">{{ t('suppression.approval.history') }}</h4>
        <ul class="flex flex-col gap-1 text-sm" data-testid="suppression-approval-history">
          <li v-for="entry in current.approvals" :key="entry.id" class="flex flex-wrap gap-2">
            <span class="badge badge-ghost badge-sm">{{ decisionText(entry.decisionId) }}</span>
            <span>{{ isolate(entry.createdUser) }}</span>
            <span class="font-mono text-xs">{{
                dateTime(entry.createdDate ?? current.expiryDate)
              }}</span>
            <span v-if="entry.note" key="note" dir="auto">{{ entry.note }}</span>
          </li>
        </ul>
      </section>

      <fieldset
        v-if="current.canDecide || current.canRetract || current.canResubmit"
        key="act"
        class="fieldset"
      >
        <label class="label" for="suppression-note">{{ t('suppression.approval.note') }}</label>
        <textarea
          id="suppression-note"
          v-model="note"
          class="textarea min-h-20 w-full"
          dir="auto"
          maxlength="1000"
          data-testid="suppression-note"
        ></textarea>
      </fieldset>

      <fieldset v-if="current.canResubmit" key="resubmitExpiry" class="fieldset">
        <label class="label" for="suppression-resubmit-expiry">
          {{ t('suppression.approval.newExpiry') }}
        </label>
        <input
          id="suppression-resubmit-expiry"
          v-model="resubmitExpiry"
          type="datetime-local"
          class="input input-sm font-mono"
          data-testid="suppression-resubmit-expiry"
        />
        <p v-if="expiryInvalid" class="label text-error" role="alert">
          {{ t('suppression.confirm.expiryInvalid') }}
        </p>
      </fieldset>

      <p
        v-if="!current.canDecide && current.stateId === 1 && !current.canRetract"
        key="cannot"
        class="text-sm"
      >
        {{ t('suppression.approval.cannotDecide') }}
      </p>

      <ProblemAlert
        v-if="problem"
        key="problem"
        :reasons="[problem]"
        data-testid="suppression-problem"
      />

      <div class="flex flex-wrap justify-end gap-2">
        <button type="button" class="btn btn-sm" @click="emit('close')">
          {{ t('caseDetail.decide.cancel') }}
        </button>
        <button
          v-if="current.canRetract"
          key="retract"
          type="button"
          class="btn btn-sm"
          :disabled="busy"
          data-testid="suppression-retract"
          @click="retract.mutate()"
        >
          {{ t('suppression.approval.retract') }}
        </button>
        <button
          v-if="current.canResubmit"
          key="resubmit"
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="busy || expiryInvalid"
          data-testid="suppression-resubmit"
          @click="resubmit.mutate()"
        >
          {{ t('suppression.approval.resubmit') }}
        </button>
        <button
          v-if="current.canDecide"
          key="reject"
          type="button"
          class="btn btn-sm btn-error"
          :disabled="busy"
          data-testid="suppression-reject"
          @click="reject.mutate()"
        >
          {{ t('suppression.approval.reject') }}
        </button>
        <button
          v-if="current.canDecide"
          key="approve"
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="busy"
          data-testid="suppression-approve"
          @click="approve.mutate()"
        >
          {{ t('suppression.approval.approve') }}
        </button>
      </div>
    </template>
  </DecideDialog>
</template>
