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
import { useI18n } from 'vue-i18n'
import { useQueryClient } from '@tanstack/vue-query'
import { Boxes, CheckCircle2, Database, Globe, Palette, XCircle } from 'lucide-vue-next'
import { useUiStore } from '../stores/ui'
import { useFormat } from '../composables/useFormat'

const { t } = useI18n()
const { currency, date } = useFormat()
const ui = useUiStore()
const queryClient = useQueryClient()

const checks = {
  pinia: typeof ui.setTheme === 'function' && ['system', 'light', 'dark'].includes(ui.theme),
  intl: currency(12500.5, 'GBP') !== '12500.5' && date('2026-10-20') !== '2026-10-20',
  query: typeof queryClient.getQueryCache === 'function',
}
</script>

<template>
  <section class="flex flex-col gap-6">
    <h1 class="text-2xl font-semibold text-primary">{{ t('home.title') }}</h1>
    <p>{{ t('home.intro') }}</p>
    <ul class="grid gap-4 sm:grid-cols-2">
      <li class="card bg-base-100 border border-base-300" data-testid="card-vapor">
        <div class="card-body">
          <h2 class="card-title"><Boxes aria-hidden="true" />{{ t('home.vapor') }}</h2>
          <p>{{ t('home.vaporBody') }}</p>
        </div>
      </li>
      <li class="card bg-base-100 border border-base-300" data-testid="card-pinia">
        <div class="card-body">
          <h2 class="card-title"><Palette aria-hidden="true" />{{ t('home.pinia') }}</h2>
          <p>{{ t('home.piniaBody', { theme: ui.theme }) }}</p>
          <p
            :class="[
              'badge badge-lg gap-1 font-semibold w-fit',
              checks.pinia ? 'badge-success' : 'badge-error',
            ]"
            data-testid="status-pinia"
          >
            <CheckCircle2 v-if="checks.pinia" aria-hidden="true" :size="18" />
            <XCircle v-else aria-hidden="true" :size="18" />
            {{ checks.pinia ? t('home.works') : t('home.fails') }}
          </p>
        </div>
      </li>
      <li class="card bg-base-100 border border-base-300" data-testid="card-intl">
        <div class="card-body">
          <h2 class="card-title"><Globe aria-hidden="true" />{{ t('home.intl') }}</h2>
          <p>
            {{
              t('home.intlBody', {
                amount: currency(12500.5, 'GBP'),
                date: date('2026-10-20'),
                cases: t('home.casesCount', 3),
              })
            }}
          </p>
          <p
            :class="[
              'badge badge-lg gap-1 font-semibold w-fit',
              checks.intl ? 'badge-success' : 'badge-error',
            ]"
            data-testid="status-intl"
          >
            <CheckCircle2 v-if="checks.intl" aria-hidden="true" :size="18" />
            <XCircle v-else aria-hidden="true" :size="18" />
            {{ checks.intl ? t('home.works') : t('home.fails') }}
          </p>
        </div>
      </li>
      <li class="card bg-base-100 border border-base-300" data-testid="card-query">
        <div class="card-body">
          <h2 class="card-title"><Database aria-hidden="true" />{{ t('home.query') }}</h2>
          <p>{{ t('home.queryBody') }}</p>
          <p
            :class="[
              'badge badge-lg gap-1 font-semibold w-fit',
              checks.query ? 'badge-success' : 'badge-error',
            ]"
            data-testid="status-query"
          >
            <CheckCircle2 v-if="checks.query" aria-hidden="true" :size="18" />
            <XCircle v-else aria-hidden="true" :size="18" />
            {{ checks.query ? t('home.works') : t('home.fails') }}
          </p>
        </div>
      </li>
    </ul>
    <p class="sr-only">{{ t('home.icons') }}: {{ t('home.iconsBody') }}</p>
  </section>
</template>
