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
import {KeySquare, LogIn, LogOut, ShieldOff} from 'lucide-vue-next'
import HubTile from './HubTile.vue'
import {queryKeys} from '../api/queryKeys'
import {useToasts} from '../stores/toasts'
import {userAccessApi} from '../userAccess/api'
import ConfirmDialog from './ConfirmDialog.vue'
import TemporaryPasswordDialog from './TemporaryPasswordDialog.vue'

const props = defineProps<{
  userRegistryId: number
  userName: string
  wirePasswordHash: boolean
  passwordLocked: boolean
}>()

const {t} = useI18n()
const toasts = useToasts()
const queryClient = useQueryClient()

const askingPassword = ref(false)
const askingRevoke = ref(false)
const askingReset = ref(false)
const unlocked = ref(false)

const locked = computed(() => props.passwordLocked && !unlocked.value)

watch(
  () => props.userRegistryId,
  () => {
    askingPassword.value = false
    askingRevoke.value = false
    askingReset.value = false
    unlocked.value = false
  },
)

const revoke = useMutation({
  mutationFn: () => userAccessApi.revokeSessions(props.userRegistryId),
  onSuccess: () => toasts.success(t('security.access.revoked')),
  onError: (error) => toasts.apiError(error),
})

const reset = useMutation({
  mutationFn: () => userAccessApi.resetAuthenticator(props.userRegistryId),
  onSuccess: () => toasts.success(t('security.access.resetDone')),
  onError: (error) => toasts.apiError(error),
})

function confirmReset(): void {
  askingReset.value = false
  reset.mutate()
}

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
  <div class="contents">
    <HubTile
      id="access"
      :label="t('security.access.title')"
      :icon="LogIn"
      :meta="t('security.access.cardHint')"
      :pill="locked ? t('security.access.locked') : undefined"
      :openable="false"
    >
      <button
        type="button"
        class="btn btn-xs btn-square btn-ghost relative z-10 shrink-0"
        :aria-label="t('security.access.password')"
        :title="t('security.access.password')"
        data-testid="hub-access-password"
        @click="askingPassword = true"
      >
        <KeySquare aria-hidden="true" class="size-4"/>
      </button>
      <button
        type="button"
        class="btn btn-xs btn-square btn-ghost relative z-10 shrink-0"
        :disabled="revoke.isPending.value"
        :aria-label="t('security.access.revoke')"
        :title="t('security.access.revoke')"
        data-testid="hub-access-revoke"
        @click="askingRevoke = true"
      >
        <span
          v-if="revoke.isPending.value"
          key="busy"
          class="loading loading-spinner loading-xs"
          aria-hidden="true"
        ></span>
        <LogOut v-else key="icon" aria-hidden="true" class="size-4"/>
      </button>
      <button
        type="button"
        class="btn btn-xs btn-square btn-ghost relative z-10 shrink-0"
        :disabled="reset.isPending.value"
        :aria-label="t('security.access.reset')"
        :title="t('security.access.reset')"
        data-testid="hub-access-reset-authenticator"
        @click="askingReset = true"
      >
        <ShieldOff aria-hidden="true" class="size-4"/>
      </button>
    </HubTile>
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
    <ConfirmDialog
      id="reset-authenticator"
      :open="askingReset"
      :title="t('security.access.resetTitle', { name: userName })"
      :body="t('security.access.resetBody')"
      :confirm-label="t('security.access.reset')"
      :cancel-label="t('security.access.cancel')"
      @confirm="confirmReset"
      @cancel="askingReset = false"
    />
  </div>
</template>
