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
import {computed, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {Bot} from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import {useToasts} from '../../stores/toasts'
import {
  agentApi,
  agentKey,
  currentRun,
  maximumOverrideReason,
  overriddenState,
  pendingState,
  acceptedState,
} from './agentApi'
import {acceptedNote} from './agentNote'

const props = defineProps<{ caseId: number }>()
const emit = defineEmits<{ accepted: [note: string] }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const runs = useQuery({
  queryKey: computed(() => agentKey(props.caseId)),
  queryFn: () => agentApi.list(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const run = computed(() => currentRun(runs.data.value))
const overriding = ref(false)
const reason = ref('')

const outcomeText = computed(() => {
  if (run.value?.recommendedOutcomeId === 1) return t('caseDetail.agent.confirmed')
  if (run.value?.recommendedOutcomeId === 2) return t('caseDetail.agent.notConfirmed')

  return t('caseDetail.agent.inconclusive')
})

const decide = useMutation({
  mutationFn: (accept: boolean) =>
    agentApi.decide(run.value!.guid, accept, accept ? null : reason.value.trim()),
  onSuccess: (_data, accept) => {
    if (accept && run.value && run.value.recommendedOutcomeId !== 0)
      emit('accepted', acceptedNote(run.value.recommendedOutcomeId, run.value.rationale))

    overriding.value = false
    reason.value = ''
    toasts.success(t('caseDetail.agent.recorded'))
    void client.invalidateQueries({queryKey: ['service', 'CaseAgentRun']})
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <fieldset v-if="run" class="fieldset" data-testid="case-agent">
    <legend class="fieldset-legend">
      <Bot class="size-3.5" aria-hidden="true"/>
      {{ t('caseDetail.agent.title') }}
      <FieldHelp :label="t('caseDetail.agent.title')" :text="t('caseDetail.agent.hint')"/>
    </legend>

    <p class="text-sm font-semibold" data-testid="case-agent-outcome">{{ outcomeText }}</p>
    <p class="text-sm whitespace-pre-line" data-testid="case-agent-rationale">
      {{ run.rationale }}
    </p>
    <ul v-if="run.evidence.length > 0" class="list-disc pl-5 text-sm text-base-content/80">
      <li v-for="item in run.evidence" :key="item">{{ item }}</li>
    </ul>

    <p
      v-if="run.stateId === acceptedState"
      class="text-sm text-base-content/70"
      data-testid="case-agent-accepted"
    >
      {{ t('caseDetail.agent.acceptedBy', {user: run.decidedUser ?? ''}) }}
    </p>
    <p
      v-else-if="run.stateId === overriddenState"
      class="text-sm text-base-content/70"
      data-testid="case-agent-overridden"
    >
      {{
        t('caseDetail.agent.overriddenBy', {
          user: run.decidedUser ?? '',
          reason: run.overrideReason ?? '',
        })
      }}
    </p>

    <template v-else-if="run.stateId === pendingState">
      <div v-if="!overriding" class="flex gap-2 pt-1">
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="decide.isPending.value"
          data-testid="case-agent-accept"
          @click="decide.mutate(true)"
        >
          {{ t('caseDetail.agent.accept') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          data-testid="case-agent-override"
          @click="overriding = true"
        >
          {{ t('caseDetail.agent.override') }}
        </button>
      </div>
      <div v-else class="join w-full pt-1">
        <input
          v-model="reason"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumOverrideReason"
          :placeholder="t('caseDetail.agent.reason')"
          :aria-label="t('caseDetail.agent.reason')"
          data-testid="case-agent-reason"
        />
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="reason.trim().length === 0 || decide.isPending.value"
          data-testid="case-agent-confirm-override"
          @click="decide.mutate(false)"
        >
          {{ t('caseDetail.agent.confirmOverride') }}
        </button>
      </div>
    </template>

    <p v-if="run.modelName" class="text-xs text-base-content/60">{{ run.modelName }}</p>
  </fieldset>
</template>
