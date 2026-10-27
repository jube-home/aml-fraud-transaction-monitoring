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
import { nextTick, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useMutation } from '@tanstack/vue-query'
import { Check, Copy, KeySquare } from 'lucide-vue-next'
import { useFormat } from '../composables/useFormat'
import { useToasts } from '../stores/toasts'
import { userAccessApi } from '../userAccess/api'
import { passwordRequest, type TemporaryPassword } from '../userAccess/model'

const props = defineProps<{
  open: boolean
  userRegistryId: number
  userName: string
  wirePasswordHash: boolean
}>()

const emit = defineEmits<{ close: []; issued: [] }>()

const { t } = useI18n()
const { dateTime } = useFormat()
const toasts = useToasts()

const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const passwordInput = useTemplateRef<HTMLInputElement>('passwordInput')
const issued = ref<TemporaryPassword>()
const copied = ref(false)

const issue = useMutation({
  mutationFn: () =>
    userAccessApi.temporaryPassword(
      passwordRequest(props.userRegistryId, {
        wirePasswordHash: props.wirePasswordHash,
        passwordLocked: false,
      }),
    ),
  onSuccess: async (result) => {
    issued.value = result
    copied.value = false
    toasts.success(t('security.access.issued'))
    emit('issued')
    await nextTick()
    passwordInput.value?.focus()
    passwordInput.value?.select()
  },
  onError: (error) => toasts.apiError(error),
})

watch(
  () => props.open,
  (open) => {
    if (open) {
      issued.value = undefined
      copied.value = false
      dialog.value?.showModal()
    } else {
      dialog.value?.close?.()
    }
  },
  { flush: 'post' },
)

async function copyPassword(): Promise<void> {
  const password = issued.value?.password ?? ''

  try {
    await navigator.clipboard.writeText(password)
    copied.value = true
    toasts.success(t('security.access.copied'))
  } catch {
    passwordInput.value?.focus()
    passwordInput.value?.select()
    toasts.error(t('security.access.copyFailed'))
  }
}

function selectPassword(event: FocusEvent): void {
  ;(event.target as HTMLInputElement).select()
}
</script>

<template>
  <dialog
    ref="dialog"
    class="modal"
    data-testid="temporary-password-dialog"
    @cancel.prevent="emit('close')"
  >
    <div class="modal-box flex flex-col gap-4">
      <div v-if="issued" key="reveal" class="flex flex-col gap-4">
        <div class="flex items-start gap-3">
          <KeySquare class="mt-1 size-5 shrink-0 text-secondary" aria-hidden="true" />
          <div>
            <h3 class="text-lg font-bold">
              {{ t('security.access.revealTitle', { name: userName }) }}
            </h3>
            <p class="text-sm">{{ t('security.access.revealBody', { name: userName }) }}</p>
          </div>
        </div>
        <div class="flex flex-col gap-1">
          <label class="label text-sm" for="temporary-password-value">
            {{ t('security.access.passwordLabel') }}
          </label>
          <input
            id="temporary-password-value"
            ref="passwordInput"
            class="input input-sm w-full font-mono"
            type="text"
            readonly
            :value="issued.password ?? ''"
            data-testid="temporary-password-value"
            @focus="selectPassword"
          />
          <p v-if="issued.passwordExpiryDate" key="expires" class="text-xs text-secondary">
            {{ t('security.access.expires', { when: dateTime(issued.passwordExpiryDate) }) }}
          </p>
        </div>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            data-testid="temporary-password-copy"
            @click="copyPassword"
          >
            <Check v-if="copied" key="copied" aria-hidden="true" class="size-4" />
            <Copy v-else key="copy" aria-hidden="true" class="size-4" />
            {{ copied ? t('security.access.copied') : t('security.access.copy') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            data-testid="temporary-password-done"
            @click="emit('close')"
          >
            {{ t('security.access.done') }}
          </button>
        </div>
      </div>
      <div v-else key="ask" class="flex flex-col gap-4">
        <h3 class="text-lg font-bold">
          {{ t('security.access.passwordTitle', { name: userName }) }}
        </h3>
        <p class="text-sm">{{ t('security.access.passwordBody') }}</p>
        <div class="modal-action">
          <button
            type="button"
            class="btn btn-sm"
            :disabled="issue.isPending.value"
            data-testid="temporary-password-cancel"
            @click="emit('close')"
          >
            {{ t('security.access.cancel') }}
          </button>
          <button
            type="button"
            class="btn btn-sm btn-primary"
            :disabled="issue.isPending.value"
            data-testid="temporary-password-issue"
            @click="issue.mutate()"
          >
            <span
              v-if="issue.isPending.value"
              key="busy"
              class="loading loading-spinner loading-xs"
              aria-hidden="true"
            ></span>
            {{ t('security.access.issue') }}
          </button>
        </div>
      </div>
    </div>
    <div class="modal-backdrop">
      <button
        type="button"
        tabindex="-1"
        :aria-label="t('security.access.cancel')"
        @click="emit('close')"
      ></button>
    </div>
  </dialog>
</template>
