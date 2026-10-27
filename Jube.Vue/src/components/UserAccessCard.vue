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
import { KeySquare, Lock, LogIn, LogOut } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { useToasts } from '../stores/toasts'
import { userAccessApi } from '../userAccess/api'
import ConfirmDialog from './ConfirmDialog.vue'
import TemporaryPasswordDialog from './TemporaryPasswordDialog.vue'

const props = defineProps<{
  userRegistryId: number
  userName: string
  wirePasswordHash: boolean
  passwordLocked: boolean
}>()

const { t } = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()

const askingPassword = ref(false)
const askingRevoke = ref(false)
const unlocked = ref(false)

const locked = computed(() => props.passwordLocked && !unlocked.value)

watch(
  () => props.userRegistryId,
  () => {
    askingPassword.value = false
    askingRevoke.value = false
    unlocked.value = false
  },
)

const revoke = useMutation({
  mutationFn: () => userAccessApi.revokeSessions(props.userRegistryId),
  onSuccess: () => toasts.success(t('security.access.revoked')),
  onError: (error) => toasts.apiError(error),
})

async function issued(): Promise<void> {
  unlocked.value = true
  await queryClient.invalidateQueries({
    queryKey: queryKeys.formRecords('UserRegistry', undefined),
  })
}

function confirmRevoke(): void {
  askingRevoke.value = false
  revoke.mutate()
}
</script>

<template>
  <div class="card card-border border-base-300 bg-base-100" data-testid="hub-access">
    <div class="card-body gap-3 p-4">
      <div class="flex items-start justify-between gap-2">
        <h4 class="card-title flex items-center gap-2 text-base">
          <LogIn class="size-4 text-secondary" aria-hidden="true" />
          {{ t('security.access.title') }}
        </h4>
        <span
          v-if="locked"
          key="locked"
          class="badge badge-sm gap-1 font-mono"
          data-testid="hub-access-locked"
        >
          <Lock class="size-3" aria-hidden="true" />
          {{ t('security.access.locked') }}
        </span>
      </div>
      <p class="min-h-5 text-sm text-secondary">{{ t('security.access.cardHint') }}</p>
      <div class="card-actions justify-end">
        <button
          type="button"
          class="btn btn-sm"
          data-testid="hub-access-password"
          @click="askingPassword = true"
        >
          <KeySquare aria-hidden="true" class="size-4" />
          {{ t('security.access.password') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          :disabled="revoke.isPending.value"
          data-testid="hub-access-revoke"
          @click="askingRevoke = true"
        >
          <span
            v-if="revoke.isPending.value"
            key="busy"
            class="loading loading-spinner loading-xs"
            aria-hidden="true"
          ></span>
          <LogOut v-else key="icon" aria-hidden="true" class="size-4" />
          {{ t('security.access.revoke') }}
        </button>
      </div>
    </div>

    <TemporaryPasswordDialog
      :open="askingPassword"
      :user-registry-id="userRegistryId"
      :user-name="userName"
      :wire-password-hash="wirePasswordHash"
      @issued="issued"
      @close="askingPassword = false"
    />

    <ConfirmDialog
      id="revoke-sessions"
      :open="askingRevoke"
      :title="t('security.access.revokeTitle', { name: userName })"
      :body="t('security.access.revokeBody')"
      :confirm-label="t('security.access.revoke')"
      :cancel-label="t('security.access.cancel')"
      @confirm="confirmRevoke"
      @cancel="askingRevoke = false"
    />
  </div>
</template>
