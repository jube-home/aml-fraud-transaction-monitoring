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
import {Plus, Search, X} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import DecideDialog from './DecideDialog.vue'
import {operationsApi} from './operationsApi'
import {
  emptySource,
  isBlank,
  isValidSource,
  maximumSources,
  sourcesValid,
  toCitations,
  type SourceRow,
} from './researchSources'
import type {CaseDetail} from './types'

const props = defineProps<{
  open: boolean
  detail: CaseDetail
}>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const topic = ref('')
const findings = ref('')
const sources = ref<SourceRow[]>([])
const files = ref<File[]>([])
const inputKey = ref(0)

watch(
  () => props.open,
  (open) => {
    if (open) {
      topic.value = ''
      findings.value = ''
      sources.value = [emptySource()]
      files.value = []
      inputKey.value++
    }
  },
)

const save = useMutation({
  mutationFn: async () => {
    const caseKey = props.detail.caseKey ?? ''
    const caseKeyValue = props.detail.caseKeyValue ?? ''

    const saved = await operationsApi.addResearch({
      caseId: props.detail.id,
      topic: topic.value.trim(),
      findings: findings.value.trim(),
      sources: toCitations(sources.value),
    })

    let unattached = 0

    for (const file of files.value) {
      try {
        await operationsApi.attachFile(
          {caseKey, caseKeyValue, caseId: props.detail.id, caseResearchId: saved.id},
          file,
        )
      } catch {
        unattached++
      }
    }

    return unattached
  },
  onSuccess: (unattached) => {
    if (unattached > 0) {
      toasts.error(t('caseDetail.decide.notAttached', {n: unattached}))
    } else {
      toasts.success(t('caseDetail.decide.researchAdded'))
    }

    void client.invalidateQueries({queryKey: ['service', 'Case']})
    emit('close')
  },
  onError: (error) => toasts.apiError(error),
})

function pick(event: Event) {
  files.value = [...((event.target as HTMLInputElement).files ?? [])]
}

function addSource() {
  if (sources.value.length < maximumSources) sources.value = [...sources.value, emptySource()]
}

function removeSource(key: number) {
  sources.value = sources.value.filter((row) => row.key !== key)
}

function flagged(row: SourceRow): boolean {
  return !isBlank(row) && !isValidSource(row)
}

const canSave = computed(
  () =>
    topic.value.trim() !== '' &&
    findings.value.trim() !== '' &&
    sourcesValid(sources.value) &&
    !save.isPending.value,
)
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.decide.researchTitle')"
    :icon="Search"
    testid="case-research-dialog"
    @close="emit('close')"
  >
    <form
      class="flex flex-col gap-4"
      data-testid="case-decide-research"
      @submit.prevent="save.mutate()"
    >
      <fieldset class="fieldset">
        <label class="label" for="case-research-topic">{{
            t('caseDetail.decide.researchTopic')
          }}</label>
        <input
          id="case-research-topic"
          v-model="topic"
          type="text"
          class="input input-sm w-full"
          maxlength="500"
          data-testid="case-research-topic"
        />
      </fieldset>
      <fieldset class="fieldset">
        <label class="label" for="case-research-findings">{{
            t('caseDetail.decide.researchFindings')
          }}</label>
        <textarea
          id="case-research-findings"
          v-model="findings"
          class="textarea min-h-40 w-full"
          maxlength="4000"
          data-testid="case-research-findings"
        ></textarea>
        <span class="label ms-auto font-mono text-xs">{{
            t('caseDetail.decide.noteCount', {n: findings.length, max: 4000})
          }}</span>
      </fieldset>
      <fieldset class="fieldset" data-testid="case-research-sources-editor">
        <legend class="fieldset-legend">{{ t('caseDetail.decide.researchSources') }}</legend>
        <div
          v-for="(row, index) in sources"
          :key="row.key"
          class="flex flex-col gap-2 border-t border-base-300 pt-2"
          data-testid="case-research-source"
        >
          <label class="label" :for="`case-research-source-title-${row.key}`">{{
              t('caseDetail.decide.sourceTitle', {n: index + 1})
            }}</label>
          <input
            :id="`case-research-source-title-${row.key}`"
            v-model="row.title"
            type="text"
            class="input input-sm w-full"
            maxlength="300"
            data-testid="case-research-source-title"
          />
          <label class="label" :for="`case-research-source-url-${row.key}`">{{
              t('caseDetail.decide.sourceUrl', {n: index + 1})
            }}</label>
          <input
            :id="`case-research-source-url-${row.key}`"
            v-model="row.url"
            type="url"
            class="input input-sm w-full"
            :aria-invalid="flagged(row)"
            :aria-describedby="flagged(row) ? `case-research-source-error-${row.key}` : undefined"
            data-testid="case-research-source-url"
          />
          <span
            v-if="flagged(row)"
            :id="`case-research-source-error-${row.key}`"
            class="label text-xs"
            role="alert"
            data-testid="case-research-source-error"
          >{{ t('caseDetail.decide.sourceInvalid') }}</span
          >
          <label class="label" :for="`case-research-source-date-${row.key}`">{{
              t('caseDetail.decide.sourceRetrieved', {n: index + 1})
            }}</label>
          <input
            :id="`case-research-source-date-${row.key}`"
            v-model="row.retrieved"
            type="date"
            class="input input-sm w-full"
            data-testid="case-research-source-date"
          />
          <button
            type="button"
            class="btn btn-ghost btn-xs self-start"
            data-testid="case-research-source-remove"
            @click="removeSource(row.key)"
          >
            <X class="size-4" aria-hidden="true"/>
            {{ t('caseDetail.decide.removeSource', {n: index + 1}) }}
          </button>
        </div>
        <div class="flex flex-wrap items-center gap-2 pt-2">
          <button
            type="button"
            class="btn btn-sm"
            :disabled="sources.length >= maximumSources"
            data-testid="case-research-source-add"
            @click="addSource"
          >
            <Plus class="size-4" aria-hidden="true"/>
            {{ t('caseDetail.decide.addSource') }}
          </button>
          <span class="label text-xs">{{
              t('caseDetail.decide.sourcesLimit', {max: maximumSources})
            }}</span>
        </div>
      </fieldset>
      <fieldset class="fieldset">
        <label class="label" for="case-research-files">{{ t('caseDetail.decide.attach') }}</label>
        <input
          id="case-research-files"
          :key="inputKey"
          type="file"
          multiple
          class="file-input file-input-sm w-full"
          data-testid="case-research-files-input"
          @change="pick"
        />
        <span class="label">{{ t('caseDetail.decide.researchAttachHint') }}</span>
      </fieldset>
      <div class="flex justify-end gap-2">
        <button type="button" class="btn btn-sm" @click="emit('close')">
          {{ t('caseDetail.decide.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="!canSave"
          data-testid="case-research-add"
        >
          {{ t('caseDetail.decide.addResearch') }}
        </button>
      </div>
    </form>
  </DecideDialog>
</template>
