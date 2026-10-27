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
import { computed, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation } from '@tanstack/vue-query'
import { useToasts } from '../stores/toasts'
import { caseLabel } from './labels'
import { supervisionApi, type StuckLock } from './supervisionApi'

export type SupervisionAction = 'break' | 'reassign'

const props = defineProps<{
  action: SupervisionAction | null
  lock: StuckLock | null
  analysts: string[]
}>()

const emit = defineEmits<{ close: []; done: [] }>()

const { t } = useI18n()
const toasts = useToasts()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const reason = ref('')
const analyst = ref('')

const choices = computed(() => props.analysts.filter((name) => name !== props.lock?.lockedUser))

const open = computed(() => props.action !== null && props.lock !== null)

watch(
  open,
  (now) => {
    if (now) {
      reason.value = ''
      analyst.value = ''
      dialog.value?.showModal()
    } else dialog.value?.close?.()
  },
  { flush: 'post' },
)

const submit = useMutation({
  mutationFn: () =>
    props.action === 'break'
      ? supervisionApi.breakLock(props.lock!.caseId, reason.value)
      : supervisionApi.reassign(props.lock!.caseId, analyst.value),
  onSuccess: () => {
    toasts.success(
      t(props.action === 'break' ? 'cases.supervision.broken' : 'cases.supervision.reassigned'),
    )
    emit('done')
  },
  onError: (error) => toasts.apiError(error),
})

const canSubmit = computed(
  () => !submit.isPending.value && (props.action === 'break' || analyst.value !== ''),
)
</script>

<template>
  <dialog
    ref="dialog"
    class="modal"
    data-testid="cases-supervision-dialog"
    @cancel.prevent="emit('close')"
  >
    <form class="modal-box flex flex-col gap-3" @submit.prevent="submit.mutate()">
      <h3 class="text-lg font-bold">
        {{
          action === 'break'
            ? t('cases.supervision.dialog.breakTitle')
            : t('cases.supervision.dialog.reassignTitle')
        }}
      </h3>

      <p v-if="lock" key="body" class="text-sm">
        {{
          action === 'break'
            ? t('cases.supervision.dialog.breakBody', {
                case: caseLabel(lock.caseId),
                user: lock.lockedUser ?? '',
              })
            : t('cases.supervision.dialog.reassignBody', {
                case: caseLabel(lock.caseId),
                user: lock.lockedUser ?? '',
              })
        }}
      </p>

      <fieldset v-if="action === 'break'" key="break" class="fieldset">
        <label class="label" for="cases-supervision-reason">
          {{ t('cases.supervision.dialog.reason') }}
        </label>
        <textarea
          id="cases-supervision-reason"
          v-model="reason"
          class="textarea w-full"
          maxlength="500"
          rows="3"
          data-testid="cases-supervision-reason"
        ></textarea>
      </fieldset>

      <fieldset v-else key="reassign" class="fieldset">
        <label class="label" for="cases-supervision-analyst">
          {{ t('cases.supervision.dialog.analyst') }}
        </label>
        <select
          id="cases-supervision-analyst"
          v-model="analyst"
          class="select select-sm w-full"
          data-testid="cases-supervision-analyst"
        >
          <option value="" disabled>{{ t('cases.supervision.dialog.choose') }}</option>
          <option v-for="name in choices" :key="name" :value="name">{{ name }}</option>
        </select>
      </fieldset>

      <div class="modal-action mt-0">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="cases-supervision-cancel"
          @click="emit('close')"
        >
          {{ t('cases.supervision.dialog.cancel') }}
        </button>
        <button
          type="submit"
          class="btn btn-sm btn-primary"
          :disabled="!canSubmit"
          data-testid="cases-supervision-confirm"
        >
          {{
            action === 'break'
              ? t('cases.supervision.dialog.breakConfirm')
              : t('cases.supervision.dialog.reassignConfirm')
          }}
        </button>
      </div>
    </form>
    <div class="modal-backdrop">
      <button
        type="button"
        tabindex="-1"
        :aria-label="t('cases.supervision.dialog.cancel')"
        @click="emit('close')"
      ></button>
    </div>
  </dialog>
</template>
