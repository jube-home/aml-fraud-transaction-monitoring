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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useRoute, useRouter} from 'vue-router'
import {useQuery} from '@tanstack/vue-query'
import {ShieldAlert} from 'lucide-vue-next'
import {meQueryOptions} from '../auth/me'
import CrudPanel from '../components/CrudPanel.vue'
import TabStrip from '../components/TabStrip.vue'

const areas = ['OpenTelemetryLogCounter', 'OpenTelemetryExclude'] as const

const {t} = useI18n()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const allowed = computed(() => me.data.value?.landlord === true)

const tabs = computed(() => [
  {key: areas[0], label: t('adminTelemetry.logCounters')},
  {key: areas[1], label: t('adminTelemetry.excludes')},
])

const current = computed(() => areas.find((area) => area === route.query.area) ?? areas[0])

function choose(area: string): void {
  void router.replace({query: {area}})
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="adminTelemetry-page">
    <h1 class="sr-only">{{ t('nav.adminTelemetry') }}</h1>
    <h2 class="text-lg font-semibold">{{ t('nav.adminTelemetry') }}</h2>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="adminTelemetry-loading"
    >
      <span class="sr-only">{{ t('adminTelemetry.loading') }}</span>
      <div class="skeleton h-10 w-full"></div>
      <div class="skeleton h-48 w-full"></div>
    </div>

    <div
      v-else-if="me.isError.value || !allowed"
      key="denied"
      role="alert"
      class="alert alert-error"
      data-testid="adminTelemetry-denied"
    >
      <ShieldAlert class="size-5" aria-hidden="true"/>
      <span>{{ t('adminTelemetry.denied') }}</span>
    </div>

    <template v-else>
      <TabStrip :label="t('nav.adminTelemetry')" :current="current" :tabs="tabs" @choose="choose"/>
      <CrudPanel :key="current" :area="current"/>
    </template>
  </section>
</template>
