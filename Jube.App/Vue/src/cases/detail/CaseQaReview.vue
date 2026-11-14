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
import {ClipboardCheck} from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import {useToasts} from '../../stores/toasts'
import {maximumComment, qaApi, qaKey, verdictKeys} from './qaApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const state = useQuery({
  queryKey: computed(() => qaKey(props.caseId)),
  queryFn: () => qaApi.state(props.caseId),
  staleTime: 30_000,
  retry: false,
})

const analyst = ref(1)
const agent = ref(0)
const comment = ref('')

const submit = useMutation({
  mutationFn: () =>
    qaApi.submit(
      props.caseId,
      analyst.value,
      agent.value === 0 ? null : agent.value,
      comment.value.trim() === '' ? null : comment.value.trim(),
    ),
  onSuccess: () => {
    comment.value = ''
    agent.value = 0
    toasts.success(t('caseDetail.qa.recorded'))
    void client.invalidateQueries({queryKey: ['service', 'CaseQaReview']})
  },
  onError: (error) => toasts.apiError(error),
})

const reviews = computed(() => state.data.value?.reviews ?? [])
const eligible = computed(() => state.data.value?.eligible === true)
const hasRecommendation = computed(() => state.data.value?.hasRecommendation === true)
const visible = computed(() => eligible.value || reviews.value.length > 0)

function verdict(id: number | null): string {
  return id === null ? '' : t(verdictKeys[id] ?? 'caseDetail.qa.notApplicable')
}
</script>

<template>
  <fieldset v-if="visible" class="fieldset" data-testid="case-qa">
    <legend class="fieldset-legend">
      <ClipboardCheck class="size-3.5" aria-hidden="true"/>
      {{ t('caseDetail.qa.title') }}
      <FieldHelp :label="t('caseDetail.qa.title')" :text="t('caseDetail.qa.hint')"/>
    </legend>

    <ul v-if="reviews.length > 0" class="flex flex-col gap-1 text-sm">
      <li v-for="(review, index) in reviews" :key="index">
        {{
          t('caseDetail.qa.reviewLine', {
            user: review.reviewerUser,
            analyst: verdict(review.analystVerdictId),
          })
        }}
        <span v-if="review.agentVerdictId !== null">
          -
          {{ t('caseDetail.qa.agentLine', {agent: verdict(review.agentVerdictId)}) }}
        </span>
        <span v-if="review.comment" class="text-base-content/70"> - {{ review.comment }}</span>
      </li>
    </ul>

    <div v-if="eligible" class="flex flex-col gap-2 pt-1">
      <label class="flex items-center gap-2 text-sm">
        <span class="w-40">{{ t('caseDetail.qa.analystVerdict') }}</span>
        <select v-model="analyst" class="select select-sm" data-testid="case-qa-analyst">
          <option v-for="id in [1, 2, 3]" :key="id" :value="id">
            {{ verdict(id) }}
          </option>
        </select>
      </label>
      <label v-if="hasRecommendation" class="flex items-center gap-2 text-sm">
        <span class="w-40">{{ t('caseDetail.qa.agentVerdict') }}</span>
        <select v-model="agent" class="select select-sm" data-testid="case-qa-agent">
          <option :value="0">{{ t('caseDetail.qa.noAgentVerdict') }}</option>
          <option v-for="id in [1, 2, 3]" :key="id" :value="id">
            {{ verdict(id) }}
          </option>
        </select>
      </label>
      <input
        v-model="comment"
        type="text"
        class="input input-sm w-full"
        :maxlength="maximumComment"
        :placeholder="t('caseDetail.qa.comment')"
        :aria-label="t('caseDetail.qa.comment')"
        data-testid="case-qa-comment"
      />
      <button
        type="button"
        class="btn btn-sm btn-primary self-start"
        :disabled="submit.isPending.value"
        data-testid="case-qa-submit"
        @click="submit.mutate()"
      >
        {{ t('caseDetail.qa.submit') }}
      </button>
    </div>
  </fieldset>
</template>
