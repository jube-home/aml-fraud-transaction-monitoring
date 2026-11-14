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
import {ShieldAlert} from 'lucide-vue-next'
import {useToasts} from '../../stores/toasts'
import DecideDialog from '../detail/DecideDialog.vue'
import {checklistApi, checklistKeys} from './api'
import {serverMessage} from './refusal'
import {overrideReasonMaximum} from './types'

const props = defineProps<{ open: boolean; caseId: number }>()

const emit = defineEmits<{ close: [] }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const reason = ref('')
const failure = ref<string | null>(null)

watch(
  () => props.open,
  (open) => {
    if (open) {
      reason.value = ''
      failure.value = null
    }
  },
)

const trimmed = computed(() => reason.value.trim())
const valid = computed(
  () => trimmed.value.length > 0 && trimmed.value.length <= overrideReasonMaximum,
)

const grant = useMutation({
  mutationFn: () => checklistApi.override(props.caseId, trimmed.value),
  onSuccess: () => {
    toasts.success(t('caseDetail.checklist.override.granted'))
    void client.invalidateQueries({queryKey: checklistKeys.all})
    void client.invalidateQueries({queryKey: ['service', 'Case']})
    emit('close')
  },
  onError: (error) => {
    const message = serverMessage(error)

    if (message === null) toasts.apiError(error)
    else failure.value = message
  },
})
</script>

<template>
  <DecideDialog
    :open="open"
    :title="t('caseDetail.checklist.override.title')"
    :icon="ShieldAlert"
    testid="checklist-override-dialog"
    @close="emit('close')"
  >
    <p class="text-sm text-base-content/70">{{ t('caseDetail.checklist.override.intro') }}</p>

    <fieldset class="fieldset">
      <label class="label" for="checklist-override-reason">
        {{ t('caseDetail.checklist.override.reason') }}
      </label>
      <textarea
        id="checklist-override-reason"
        v-model="reason"
        class="textarea h-28 w-full"
        :maxlength="overrideReasonMaximum"
        data-testid="checklist-override-reason"
      ></textarea>
      <p class="label justify-between">
        <span>{{ t('caseDetail.checklist.override.help') }}</span>
        <span>
          {{
            t('caseDetail.checklist.override.counter', {
              n: reason.length,
              max: overrideReasonMaximum,
            })
          }}
        </span>
      </p>
    </fieldset>

    <p
      v-if="failure"
      key="failure"
      role="alert"
      class="text-sm text-error"
      data-testid="checklist-override-error"
    >
      {{ failure }}
    </p>

    <div class="flex justify-end gap-2">
      <button type="button" class="btn btn-sm" @click="emit('close')">
        {{ t('caseDetail.decide.cancel') }}
      </button>
      <button
        type="button"
        class="btn btn-sm btn-primary"
        :disabled="!valid || grant.isPending.value"
        data-testid="checklist-override-submit"
        @click="grant.mutate()"
      >
        {{ t('caseDetail.checklist.override.submit') }}
      </button>
    </div>
  </DecideDialog>
</template>
