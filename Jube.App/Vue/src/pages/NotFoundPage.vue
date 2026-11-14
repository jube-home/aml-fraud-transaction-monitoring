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
import {RouterLink, useRouter} from 'vue-router'
import {useI18n} from 'vue-i18n'

const {t} = useI18n()
const router = useRouter()

const canGoBack =
  Boolean(router.options.history.state.back) ||
  Boolean((window as {navigation?: {canGoBack?: boolean}}).navigation?.canGoBack)
</script>

<template>
  <main
    id="main"
    class="flex min-h-screen flex-col items-center justify-center gap-4 bg-base-200 px-4 text-base-content"
  >
    <h1 class="text-2xl font-semibold text-primary">{{ t('notFound.title') }}</h1>
    <button
      v-if="canGoBack"
      type="button"
      class="btn btn-sm btn-primary"
      data-testid="not-found-previous"
      @click="router.back()"
    >
      {{ t('notFound.previous') }}
    </button>
    <RouterLink :to="{ name: 'askJooby' }" class="link" data-testid="not-found-start">
      {{ t('notFound.back') }}
    </RouterLink>
  </main>
</template>
