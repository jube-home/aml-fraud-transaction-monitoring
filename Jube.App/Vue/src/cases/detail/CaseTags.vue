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
import {Plus, Tag, X} from 'lucide-vue-next'
import {ApiError} from '../../api/useApi'
import DecideCard from './DecideCard.vue'
import {useToasts} from '../../stores/toasts'
import {caseDetailApi} from './api'
import {remainingChoices, sameTags, tagNames, withTag, withoutTag} from './caseTags'
import type {CaseTag} from './types'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const tagsKey = computed(() => ['service', 'CaseTag', 'byCase', props.caseId] as const)

const tags = useQuery({
  queryKey: tagsKey,
  queryFn: () => caseDetailApi.tags(props.caseId),
  staleTime: 15_000,
  retry: false,
})

const choices = useQuery({
  queryKey: computed(() => ['service', 'CaseTag', 'choices', props.caseId] as const),
  queryFn: () => caseDetailApi.tagChoices(props.caseId),
  staleTime: 60_000,
  retry: false,
})

const saved = computed(() => tagNames(tags.data.value ?? []))
const draft = ref<string[]>([])
const picked = ref('')

watch(
  saved,
  (names) => {
    draft.value = [...names]
  },
  {immediate: true},
)

const remaining = computed(() => remainingChoices(choices.data.value ?? [], draft.value))
const dirty = computed(() => !sameTags(saved.value, draft.value))
const vocabulary = computed(() => choices.data.value ?? [])

const merge = useMutation({
  mutationFn: () => caseDetailApi.mergeTags(props.caseId, [...draft.value]),
  onSuccess: (result: CaseTag[]) => {
    client.setQueryData(tagsKey.value, result)
    toasts.success(t('caseDetail.tags.saved'))
    void client.invalidateQueries({queryKey: ['service', 'CaseTag']})
    void client.invalidateQueries({queryKey: ['graph']})
  },
  onError: (error) => {
    if (error instanceof ApiError && error.kind === 'validation' && typeof error.body === 'string')
      toasts.error(error.body)
    else toasts.apiError(error)
  },
})

function add(): void {
  draft.value = withTag(draft.value, picked.value)
  picked.value = ''
}

function remove(name: string): void {
  draft.value = withoutTag(draft.value, name)
}
</script>

<template>
  <DecideCard
    :title="t('caseDetail.tags.title')"
    :help="t('caseDetail.tags.hint')"
    testid="case-tags"
  >
    <template #icon>
      <Tag class="size-5"/>
    </template>

    <div
      v-if="tags.isPending.value"
      key="loading"
      class="skeleton h-6 w-2/3"
      role="status"
      :aria-label="t('caseDetail.tags.title')"
    ></div>
    <p v-else-if="tags.isError.value" key="failed" role="alert" class="text-sm">
      {{ t('caseDetail.tags.failed') }}
    </p>
    <template v-else>
      <div class="flex flex-wrap items-center gap-1" data-testid="case-tags-chips">
        <span
          v-if="draft.length === 0"
          key="empty"
          class="text-sm text-base-content/70"
          data-testid="case-tags-empty"
        >
          {{ t('caseDetail.tags.empty') }}
        </span>
        <span
          v-for="name in draft"
          :key="name"
          class="badge badge-soft gap-1 pe-0"
          :data-testid="`case-tag-${name}`"
        >
          {{ name }}
          <button
            type="button"
            class="btn btn-ghost btn-xs btn-circle"
            :aria-label="t('caseDetail.tags.remove', { name })"
            :data-testid="`case-tag-remove-${name}`"
            @click="remove(name)"
          >
            <X class="size-3" aria-hidden="true"/>
          </button>
        </span>
      </div>

      <p v-if="choices.isSuccess.value && vocabulary.length === 0" key="none" class="label">
        {{ t('caseDetail.tags.noChoices') }}
      </p>
      <p
        v-else-if="choices.isSuccess.value && remaining.length === 0"
        key="all"
        class="label"
        data-testid="case-tags-all-used"
      >
        {{ t('caseDetail.tags.allUsed') }}
      </p>
      <div v-else key="chooser" class="join">
        <label class="sr-only" for="case-tags-choice">{{ t('caseDetail.tags.choose') }}</label>
        <select
          id="case-tags-choice"
          v-model="picked"
          class="select select-sm join-item min-w-0"
          :disabled="choices.isPending.value || merge.isPending.value"
          data-testid="case-tags-choice"
        >
          <option value="" disabled>{{ t('caseDetail.tags.choose') }}</option>
          <option v-for="name in remaining" :key="name" :value="name">{{ name }}</option>
        </select>
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="picked === '' || merge.isPending.value"
          data-testid="case-tags-add"
          @click="add"
        >
          <Plus class="size-4" aria-hidden="true"/>
          {{ t('caseDetail.tags.add') }}
        </button>
      </div>

      <button
        type="button"
        class="btn btn-sm btn-primary w-fit"
        :disabled="!dirty || merge.isPending.value"
        data-testid="case-tags-save"
        @click="merge.mutate()"
      >
        <span
          v-if="merge.isPending.value"
          key="busy"
          class="loading loading-spinner loading-xs"
        ></span>
        {{ t('caseDetail.tags.save') }}
      </button>
    </template>
  </DecideCard>
</template>
