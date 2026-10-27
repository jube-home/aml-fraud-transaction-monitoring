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
import { Banknote } from 'lucide-vue-next'
import { hasPermission, meQueryOptions } from '../../auth/me'
import { useFormat } from '../../composables/useFormat'
import { useToasts } from '../../stores/toasts'
import DecideDialog from './DecideDialog.vue'
import HoldLegs from './HoldLegs.vue'
import { rangeText, stateClass, stateText } from './holdText'
import { holdState, operationsApi, type HoldView } from './operationsApi'
import type { CaseDetail } from './types'

const props = defineProps<{ open: boolean; detail: CaseDetail }>()

const emit = defineEmits<{ close: [] }>()

const releasePermission = 68

const { t } = useI18n()
const format = useFormat()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)

const holds = useQuery({
  queryKey: computed(() => ['service', 'Case', 'holds', props.detail.id] as const),
  queryFn: () => operationsApi.holds(props.detail.id),
  enabled: computed(() => props.open),
  retry: false,
})

const canDecide = computed(() => hasPermission(me.data.value, releasePermission))
const ordered = computed(() =>
  [...(holds.data.value ?? [])].sort(
    (a, b) =>
      Number(b.stateId === holdState.held) - Number(a.stateId === holdState.held) ||
      Date.parse(b.stampedDate) - Date.parse(a.stampedDate),
  ),
)

const deciding = ref<{ guid: string; release: boolean } | null>(null)
const reason = ref('')

watch(
  () => props.open,
  () => {
    deciding.value = null
    reason.value = ''
  },
)

function begin(hold: HoldView, release: boolean): void {
  deciding.value = { guid: hold.entityAnalysisModelInstanceEntryGuid, release }
  reason.value = ''
}

const decide = useMutation({
  mutationFn: (request: { guid: string; release: boolean }) =>
    request.release
      ? operationsApi.releaseHold(props.detail.id, request.guid, reason.value.trim())
      : operationsApi.refuseHold(props.detail.id, request.guid, reason.value.trim()),
  onSuccess: (_, request) => {
    toasts.success(
      t(request.release ? 'caseDetail.hold.releasedToast' : 'caseDetail.hold.refusedToast'),
    )
    deciding.value = null
    reason.value = ''
    void client.invalidateQueries({ queryKey: ['service', 'Case'] })
  },
  onError: (error) => {
    toasts.apiError(error)
    void client.invalidateQueries({ queryKey: ['service', 'Case', 'holds', props.detail.id] })
  },
})

function confirm(): void {
  if (deciding.value && reason.value.trim() !== '') decide.mutate(deciding.value)
}

function overdue(hold: HoldView): boolean {
  return hold.deadlineDate !== null && Date.parse(hold.deadlineDate) < Date.now()
}
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.hold.dialogTitle')"
    :icon="Banknote"
    testid="case-hold-dialog"
    @close="emit('close')"
  >
    <p class="text-sm text-base-content/70">{{ t('caseDetail.hold.intro') }}</p>
    <div v-if="holds.isPending.value" key="loading" class="skeleton h-32 w-full"></div>
    <div v-else-if="holds.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('caseDetail.hold.failed') }}</span>
    </div>
    <p v-else-if="ordered.length === 0" key="none" class="text-sm" data-testid="case-hold-none">
      {{ t('caseDetail.hold.none') }}
    </p>
    <ul v-else key="list" class="flex min-w-0 flex-col gap-3" role="list">
      <li
        v-for="hold in ordered"
        :key="hold.entityAnalysisModelInstanceEntryGuid"
        class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-4"
        :data-testid="`case-hold-${hold.entityAnalysisModelInstanceEntryGuid}`"
      >
        <header class="flex flex-wrap items-center gap-2">
          <span
            class="badge badge-sm"
            :class="stateClass(hold.stateId)"
            :data-testid="`case-hold-state-${hold.entityAnalysisModelInstanceEntryGuid}`"
            >{{ stateText(t, hold.stateId) }}</span
          >
          <h4 class="min-w-0 text-sm font-semibold">
            {{ hold.rangeName }}
            <span class="font-mono text-xs font-normal text-base-content/70">{{
              t('caseDetail.hold.elevation', {
                n: hold.elevation,
                range: rangeText(hold.rangeMinimum, hold.rangeMaximum),
              })
            }}</span>
          </h4>
          <span v-if="hold.entryKeyValue" class="ms-auto font-mono text-xs">{{
            hold.entryKeyValue
          }}</span>
        </header>
        <dl class="grid grid-cols-[max-content_1fr] gap-x-3 gap-y-1 text-sm">
          <dt class="text-xs text-base-content/70">{{ t('caseDetail.hold.heldSince') }}</dt>
          <dd class="text-xs">{{ format.dateTime(hold.stampedDate) }}</dd>
          <template v-if="hold.deadlineDate && hold.stateId === 1">
            <dt class="text-xs text-base-content/70">
              {{ overdue(hold) ? t('caseDetail.hold.overdue') : t('caseDetail.hold.deadline') }}
            </dt>
            <dd class="text-xs" :class="overdue(hold) ? 'font-medium text-error' : ''">
              {{ format.dateTime(hold.deadlineDate) }}
            </dd>
          </template>
          <template v-if="hold.stateId === 1">
            <dt class="text-xs text-base-content/70">{{ t('caseDetail.hold.hook') }}</dt>
            <dd class="text-xs">
              {{ hold.releaseHookName ?? t('caseDetail.hold.noHook') }}
            </dd>
          </template>
        </dl>
        <p v-if="hold.decision" key="decision" class="text-sm" data-testid="case-hold-decision">
          {{
            t('caseDetail.hold.decision', {
              state: stateText(t, hold.stateId),
              user: hold.decision.actor ?? '',
              when: format.dateTime(hold.decision.date),
            })
          }}
          <span class="block text-xs text-base-content/70">{{
            t('caseDetail.hold.decisionReason', { reason: hold.decision.reason ?? '' })
          }}</span>
        </p>
        <HoldLegs
          :legs="hold.legs"
          :testid="`case-hold-legs-${hold.entityAnalysisModelInstanceEntryGuid}`"
        />
        <template v-if="hold.stateId === 1">
          <p v-if="!canDecide" key="permission" class="text-xs text-base-content/70">
            {{ t('caseDetail.hold.needPermission') }}
          </p>
          <div
            v-else-if="deciding?.guid !== hold.entityAnalysisModelInstanceEntryGuid"
            key="choose"
          >
            <div class="flex flex-wrap items-center gap-2">
              <button
                type="button"
                class="btn btn-sm btn-primary"
                :disabled="hold.releaseHookName === null"
                :data-testid="`case-hold-release-${hold.entityAnalysisModelInstanceEntryGuid}`"
                @click="begin(hold, true)"
              >
                {{ t('caseDetail.hold.release') }}
              </button>
              <button
                type="button"
                class="btn btn-sm"
                :data-testid="`case-hold-refuse-${hold.entityAnalysisModelInstanceEntryGuid}`"
                @click="begin(hold, false)"
              >
                {{ t('caseDetail.hold.refuse') }}
              </button>
            </div>
            <p v-if="hold.releaseHookName === null" key="nohook" class="mt-1 text-xs text-error">
              {{ t('caseDetail.hold.noHookNote') }}
            </p>
          </div>
          <fieldset v-else key="reason" class="fieldset">
            <legend class="fieldset-legend">
              {{
                deciding.release
                  ? t('caseDetail.hold.releaseTitle')
                  : t('caseDetail.hold.refuseTitle')
              }}
            </legend>
            <p class="text-xs text-base-content/70">
              {{
                deciding.release
                  ? t('caseDetail.hold.releaseNote')
                  : hold.refuseCallsHook
                    ? t('caseDetail.hold.refuseCalls')
                    : t('caseDetail.hold.refuseRecords')
              }}
            </p>
            <label class="flex flex-col gap-1">
              <span class="label">{{ t('caseDetail.hold.reason') }}</span>
              <textarea
                v-model="reason"
                class="textarea textarea-sm h-24 w-full"
                maxlength="1000"
                data-testid="case-hold-reason"
              ></textarea>
            </label>
            <p class="label">{{ t('caseDetail.hold.reasonHint') }}</p>
            <div class="flex items-center justify-end gap-2">
              <button
                type="button"
                class="btn btn-sm"
                :disabled="decide.isPending.value"
                @click="deciding = null"
              >
                {{ t('caseDetail.hold.back') }}
              </button>
              <button
                type="button"
                class="btn btn-sm"
                :class="deciding.release ? 'btn-primary' : 'btn-error'"
                :disabled="decide.isPending.value || reason.trim() === ''"
                data-testid="case-hold-confirm"
                @click="confirm()"
              >
                {{
                  deciding.release
                    ? t('caseDetail.hold.releaseConfirm')
                    : t('caseDetail.hold.refuseConfirm')
                }}
              </button>
            </div>
          </fieldset>
        </template>
      </li>
    </ul>
  </DecideDialog>
</template>
