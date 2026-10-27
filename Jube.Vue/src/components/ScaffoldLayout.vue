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
import { RouterLink, RouterView } from 'vue-router'
import { useI18n } from 'vue-i18n'
import ThemeSwitcher from './ThemeSwitcher.vue'
import LocaleSwitcher from './LocaleSwitcher.vue'

const { t } = useI18n()
</script>

<template>
  <div class="min-h-screen bg-base-200 text-base-content">
    <a
      href="#main"
      class="btn btn-primary btn-sm absolute -translate-y-full focus:translate-y-0 focus:z-50"
    >
      {{ t('app.skipToContent') }}
    </a>
    <header class="navbar bg-base-100 border-b-2 border-accent gap-4 px-4">
      <RouterLink :to="{ name: 'home' }" class="px-1 py-2">
        <span
          role="img"
          :aria-label="t('app.name')"
          class="block aspect-[1640/616] h-6 bg-(--color-logo) mask-[url(/jube-wordmark.png)] mask-contain mask-center mask-no-repeat"
        ></span>
      </RouterLink>
      <nav :aria-label="t('nav.label')" class="flex-1">
        <ul class="menu menu-horizontal gap-1">
          <li>
            <RouterLink
              :to="{ name: 'home' }"
              class="aria-[current=page]:bg-primary! aria-[current=page]:text-primary-content!"
              >{{ t('nav.home') }}</RouterLink
            >
          </li>
          <li>
            <RouterLink
              :to="{ name: 'grid' }"
              class="aria-[current=page]:bg-primary! aria-[current=page]:text-primary-content!"
              >{{ t('nav.grid') }}</RouterLink
            >
          </li>
          <li>
            <RouterLink
              :to="{ name: 'api' }"
              class="aria-[current=page]:bg-primary! aria-[current=page]:text-primary-content!"
              >{{ t('nav.api') }}</RouterLink
            >
          </li>
          <li>
            <RouterLink
              :to="{ name: 'theme' }"
              class="aria-[current=page]:bg-primary! aria-[current=page]:text-primary-content!"
              >{{ t('nav.theme') }}</RouterLink
            >
          </li>
        </ul>
      </nav>
      <ThemeSwitcher />
      <LocaleSwitcher />
    </header>
    <main id="main" class="mx-auto max-w-5xl px-4 py-6">
      <RouterView />
    </main>
  </div>
</template>
