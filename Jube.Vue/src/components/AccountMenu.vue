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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { KeyRound, LogOut } from 'lucide-vue-next'
import type { Me } from '../auth/me'
import { useSessionActions } from '../auth/session'
import { legacyUrl } from '../layout/legacyUrl'
import ThemeSwitcher from './ThemeSwitcher.vue'
import LocaleSwitcher from './LocaleSwitcher.vue'

const props = defineProps<{ me: Me }>()
const { t } = useI18n()
const { signOut } = useSessionActions()

const initials = computed(() => props.me.userName.slice(0, 2).toUpperCase())
</script>

<template>
  <button
    type="button"
    class="btn btn-ghost btn-sm gap-2"
    popovertarget="account-menu"
    style="anchor-name: --account-menu"
    data-testid="account-button"
  >
    <span class="avatar avatar-placeholder" aria-hidden="true">
      <span class="w-7 rounded-full bg-base-300 text-base-content">
        <span class="text-xs">{{ initials }}</span>
      </span>
    </span>
    <span class="sr-only">{{ t('shell.account') }}:</span>
    <span class="max-w-40 truncate max-sm:sr-only">{{ props.me.userName }}</span>
  </button>
  <div
    id="account-menu"
    popover
    class="dropdown dropdown-end card card-sm z-50 mt-2 w-72 bg-base-100 shadow-md"
    style="position-anchor: --account-menu"
  >
    <div class="card-body gap-3">
      <p class="text-sm">
        {{ t('shell.signedInAs') }}
        <strong class="block truncate font-mono">{{ props.me.userName }}</strong>
      </p>
      <div class="flex items-center justify-between gap-3 border-y border-base-300 py-3">
        <ThemeSwitcher />
        <LocaleSwitcher />
      </div>
      <a
        v-if="props.me.canChangePassword"
        class="btn btn-sm justify-start"
        :href="legacyUrl('/Account/ChangePassword')"
      >
        <KeyRound aria-hidden="true" class="size-4" />
        {{ t('shell.changePassword') }}
      </a>
      <button type="button" class="btn btn-sm justify-start" @click="signOut">
        <LogOut aria-hidden="true" class="size-4" />
        {{ t('shell.signOut') }}
      </button>
    </div>
  </div>
</template>
