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
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { safeReturnAddress } from '../auth/returnAddress'
import SignInWizard from '../components/auth/SignInWizard.vue'
import ThemeSwitcher from '../components/ThemeSwitcher.vue'
import LocaleSwitcher from '../components/LocaleSwitcher.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const client = useQueryClient()

const returnAddress = computed(() => safeReturnAddress(route.query.redirect))

function onSignedIn(): void {
  client.clear()
  void router.replace(returnAddress.value)
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-base-200 text-base-content">
    <a
      href="#main"
      class="btn btn-primary btn-sm absolute -translate-y-full focus:translate-y-0 focus:z-50"
    >
      {{ t('app.skipToContent') }}
    </a>
    <header class="flex flex-wrap items-center justify-end gap-4 px-4 py-3">
      <ThemeSwitcher />
      <LocaleSwitcher />
    </header>
    <main id="main" class="flex flex-1 flex-col items-center justify-center gap-8 px-4 pb-16">
      <span
        role="img"
        :aria-label="t('app.name')"
        class="block aspect-[1640/616] h-10 bg-(--color-logo) mask-[url(/jube-wordmark.png)] mask-contain mask-center mask-no-repeat"
      ></span>
      <div
        class="card w-full max-w-md border border-base-300 border-t-2 border-t-accent bg-base-100"
      >
        <div class="card-body">
          <SignInWizard :return-address="returnAddress" @signed-in="onSignedIn" />
        </div>
      </div>
    </main>
  </div>
</template>
