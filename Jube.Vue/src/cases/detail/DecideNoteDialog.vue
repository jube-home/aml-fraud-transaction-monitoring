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
import { useMutation, useQueryClient } from '@tanstack/vue-query'
import { NotebookPen } from 'lucide-vue-next'
import { useToasts } from '../../stores/toasts'
import DecideDialog from './DecideDialog.vue'
import { notePriorityOf } from './decide'
import { operationsApi, type Choice } from './operationsApi'
import type { CaseDetail, CaseStatusChoice } from './types'

const props = defineProps<{
  open: boolean
  detail: CaseDetail
  actions: Choice[]
  statuses: CaseStatusChoice[]
}>()

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const actionId = ref('')
const note = ref('')

watch(
  () => props.open,
  (open) => {
    if (open) {
      actionId.value = ''
      note.value = ''
    }
  },
)

const save = useMutation({
  mutationFn: () =>
    operationsApi.addNote({
      note: note.value.trim(),
      actionId: Number(actionId.value),
      priorityId: notePriorityOf(props.statuses, props.detail.caseWorkflowStatusGuid),
      caseKey: props.detail.caseKey ?? '',
      caseKeyValue: props.detail.caseKeyValue ?? '',
      caseId: props.detail.id,
    }),
  onSuccess: () => {
    toasts.success(t('caseDetail.decide.noteAdded'))
    void client.invalidateQueries({ queryKey: ['service', 'Case'] })
    emit('close')
  },
  onError: (error) => toasts.apiError(error),
})

const canSave = computed(
  () => note.value.trim() !== '' && actionId.value !== '' && !save.isPending.value,
)
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.decide.noteTitle')"
    :icon="NotebookPen"
    testid="case-note-dialog"
    @close="emit('close')"
  >
    <form
      class="flex flex-col gap-4"
      data-testid="case-decide-note"
      @submit.prevent="save.mutate()"
    >
      <fieldset class="fieldset">
        <label class="label" for="case-note-action">{{ t('caseDetail.decide.action') }}</label>
        <select
          id="case-note-action"
          v-model="actionId"
          class="select select-sm w-full"
          data-testid="case-note-action"
        >
          <option value="" disabled>{{ t('caseDetail.decide.choose') }}</option>
          <option v-for="choice in actions" :key="choice.id" :value="String(choice.id)">
            {{ choice.name }}
          </option>
        </select>
      </fieldset>
      <fieldset class="fieldset">
        <label class="label" for="case-note-text-input">{{ t('caseDetail.decide.note') }}</label>
        <textarea
          id="case-note-text-input"
          v-model="note"
          class="textarea min-h-40 w-full"
          maxlength="4000"
          data-testid="case-note-text-input"
        ></textarea>
        <span class="label ms-auto font-mono text-xs">{{
          t('caseDetail.decide.noteCount', { n: note.length, max: 4000 })
        }}</span>
      </fieldset>
      <div class="flex justify-end gap-2">
        <button type="button" class="btn btn-sm" @click="emit('close')">
          {{ t('caseDetail.decide.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="!canSave"
          data-testid="case-note-add"
        >
          {{ t('caseDetail.decide.addNote') }}
        </button>
      </div>
    </form>
  </DecideDialog>
</template>
