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
import { useQuery } from '@tanstack/vue-query'
import { useRoute, useRouter } from 'vue-router'
import { hasPermission, meQueryOptions } from '../auth/me'
import SanctionSearch from '../components/SanctionSearch.vue'
import { locationFromParams, locationKey, paramsFromLocation } from '../sanction/location'
import type { SanctionParams } from '../sanction/types'

const sanctionsPermission = 29

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const allowed = computed(() => hasPermission(me.data.value, sanctionsPermission))
const params = computed(() => paramsFromLocation(route.query))

const generation = ref(0)
let written = locationKey(params.value)

function remember(next: SanctionParams): void {
  written = locationKey(next)
  void router.push({ query: locationFromParams(next) })
}

watch(
  () => locationKey(params.value),
  (now) => {
    if (now === written) return

    written = now
    generation.value += 1
  },
)
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="sanctions-page">
    <h1 class="sr-only">{{ t('nav.sanctions') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="sanctions-loading"
    >
      <span class="sr-only">{{ t('sanctions.loading') }}</span>
      <div class="skeleton h-40 w-full"></div>
      <div class="skeleton h-4 w-48"></div>
    </div>

    <div
      v-else-if="!allowed"
      key="refused"
      role="alert"
      class="alert alert-error"
      data-testid="sanctions-refused"
    >
      <span>{{ t('sanctions.refused') }}</span>
    </div>

    <SanctionSearch
      v-else
      :key="generation"
      :multi-part-string="params.multiPartString"
      :distance="params.distance"
      :max-distance-ratio="params.maxDistanceRatio"
      :max-coverage-ratio="params.maxCoverageRatio"
      auto-run
      @search="remember"
    />
  </section>
</template>
