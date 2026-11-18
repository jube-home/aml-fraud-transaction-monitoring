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
import {Undo2, UserCheck} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import {
  analystLikelihoodKeys,
  assessmentApi,
  assessmentKey,
  canRecord,
  likelihoodChoices,
  maximumReason,
} from './assessmentApi'
import DecideCard from './DecideCard.vue'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const state = useQuery({
  queryKey: computed(() => assessmentKey(props.caseId)),
  queryFn: () => assessmentApi.state(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const likelihood = ref(0)
const reason = ref('')

const own = computed(() => {
  const current = state.data.value?.current ?? null

  return current?.mine ? current : null
})

const ready = computed(() => canRecord(likelihood.value, reason.value))

function refresh(): void {
  void client.invalidateQueries({queryKey: ['service', 'CaseAnalystAssessment']})
}

const record = useMutation({
  mutationFn: () => assessmentApi.record(props.caseId, likelihood.value, reason.value.trim()),
  onSuccess: () => {
    likelihood.value = 0
    reason.value = ''
    toasts.success(t('caseDetail.assessment.saved'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const withdraw = useMutation({
  mutationFn: (guid: string) => assessmentApi.withdraw(guid),
  onSuccess: () => {
    toasts.success(t('caseDetail.assessment.withdrawn'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <DecideCard
    v-if="state.data.value"
    :title="t('caseDetail.assessment.title')"
    :help="t('caseDetail.assessment.hint')"
    testid="case-decide-assessment"
  >
    <template #icon>
      <UserCheck class="size-5"/>
    </template>
    <template #status>
      <span v-if="own" key="own" class="badge badge-soft badge-sm font-semibold">
        {{ t(analystLikelihoodKeys[own.likelihoodId] ?? 'caseDetail.assessment.medium') }}
      </span>
    </template>

    <fieldset class="flex min-w-0 flex-col gap-2">
      <legend class="mb-2 text-xs text-base-content/70">
        {{ t('caseDetail.assessment.choose') }}
      </legend>
      <div class="join w-full">
        <input
          v-for="id in likelihoodChoices"
          :key="id"
          v-model="likelihood"
          type="radio"
          name="case-assessment-likelihood"
          class="join-item btn btn-sm flex-1 bg-base-100"
          :value="id"
          :aria-label="t(analystLikelihoodKeys[id] ?? 'caseDetail.assessment.medium')"
          :data-testid="`case-assessment-choice-${id}`"
        />
      </div>
      <textarea
        v-model="reason"
        class="textarea textarea-sm w-full"
        rows="2"
        :maxlength="maximumReason"
        :placeholder="t('caseDetail.assessment.reason')"
        :aria-label="t('caseDetail.assessment.reason')"
        data-testid="case-assessment-reason"
      ></textarea>
      <div class="flex flex-wrap items-center gap-2">
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="!ready || record.isPending.value"
          data-testid="case-assessment-save"
          @click="record.mutate()"
        >
          {{ t('caseDetail.assessment.save') }}
        </button>
        <button
          v-if="own"
          key="withdraw"
          type="button"
          class="btn btn-ghost btn-sm ms-auto"
          :disabled="withdraw.isPending.value"
          data-testid="case-assessment-withdraw"
          @click="withdraw.mutate(own.guid)"
        >
          <Undo2 class="size-4" aria-hidden="true"/>
          {{ t('caseDetail.assessment.withdraw') }}
        </button>
      </div>
    </fieldset>
  </DecideCard>
</template>
