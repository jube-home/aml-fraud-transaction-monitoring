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
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/vue-query'
import { FileText } from 'lucide-vue-next'
import { ApiError } from '../../api/useApi'
import { useDebounced } from '../../composables/useDebounced'
import TemplateForm from '../../datasource/TemplateForm.vue'
import type { FieldValues } from '../../datasource/templateForm'
import { useToasts } from '../../stores/toasts'
import DecideDialog from './DecideDialog.vue'
import { operationsApi } from './operationsApi'
import type { CaseDetail } from './types'

const props = defineProps<{ open: boolean; detail: CaseDetail; formId: number; title: string }>()

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const values = ref<FieldValues>({})
const errors = ref<Record<string, string>>({})
const settled = useDebounced(values, 300)

watch(
  () => [props.open, props.formId],
  () => {
    values.value = {}
    errors.value = {}
  },
)

const rendered = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'form', props.detail.id, props.formId, settled.value] as const,
  ),
  queryFn: ({ signal }) =>
    operationsApi.renderForm(props.detail.id, props.formId, settled.value, signal),
  enabled: computed(() => props.open && props.formId !== 0),
  placeholderData: keepPreviousData,
  retry: false,
})

const submit = useMutation({
  mutationFn: () =>
    operationsApi.submitForm({
      caseWorkflowFormId: props.formId,
      caseId: props.detail.id,
      caseKey: props.detail.caseKey ?? '',
      caseKeyValue: props.detail.caseKeyValue ?? '',
      payload: values.value,
    }),
  onSuccess: () => {
    toasts.success(t('caseDetail.forms.submitted'))
    void client.invalidateQueries({ queryKey: ['service', 'Case'] })
    emit('close')
  },
  onError: (error) => {
    if (error instanceof ApiError && error.kind === 'validation') {
      const found: Record<string, string> = {}

      for (const [key, messages] of Object.entries(error.fieldErrors)) {
        found[key.startsWith('Payload.') ? key.slice('Payload.'.length) : key] = messages[0] ?? ''
      }

      errors.value = found
    } else toasts.apiError(error)
  },
})

function update(next: FieldValues): void {
  values.value = { ...values.value, ...next }
  errors.value = {}
}
</script>

<template>
  <DecideDialog
    :open="open"
    :title="title"
    :icon="FileText"
    testid="case-forms-dialog"
    @close="emit('close')"
  >
    <div v-if="rendered.isPending.value" key="drawing" class="skeleton h-40 w-full"></div>
    <div v-else-if="rendered.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ rendered.error.value?.message }}</span>
    </div>
    <form
      v-else
      key="form"
      class="flex flex-col gap-4"
      data-testid="case-form"
      @submit.prevent="submit.mutate()"
    >
      <TemplateForm
        :html="rendered.data.value?.html ?? ''"
        :visible-fields="rendered.data.value?.visibleFields"
        :values="values"
        :errors="errors"
        :label="title"
        @update:values="update"
      />
      <div class="flex items-center justify-end gap-2">
        <button type="button" class="btn btn-sm" @click="emit('close')">
          {{ t('caseDetail.decide.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="submit.isPending.value"
          data-testid="case-form-submit"
        >
          {{ t('caseDetail.forms.submit') }}
        </button>
      </div>
    </form>
  </DecideDialog>
</template>
