<!--  Copyright (C) 2022-present Jube Holdings Limited.

  This file is part of Jube™ software.

  Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
  as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
  Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
  of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
  see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {useRoute, useRouter} from 'vue-router'
import {hasPermission, meQueryOptions} from '../auth/me'
import {queryKeys} from '../api/queryKeys'
import SvirfdSearch from '../components/SvirfdSearch.vue'
import {svirfdApi} from '../svirfd/api'
import {remoteBlocked, tenantRemoteOff} from '../svirfd/status'
import {locationFromParams, locationKey, paramsFromLocation} from '../svirfd/location'
import {useJoobyPageContext} from '../composables/useJoobyPageContext'

const svirfdPermission = 72

const {t} = useI18n()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const allowed = computed(() => hasPermission(me.data.value, svirfdPermission))
const enabled = computed(() => me.data.value?.svirfd === true)
const params = computed(() => paramsFromLocation(route.query))

const status = useQuery({
  queryKey: queryKeys.svirfdStatus(),
  queryFn: () => svirfdApi.status(),
  enabled: computed(() => allowed.value && enabled.value),
  refetchInterval: 30_000,
})

const tenantOff = computed(() => tenantRemoteOff(status.data.value))
const blocked = computed(() => remoteBlocked(status.data.value))

const stateNotice = computed(() => {
  const value = status.data.value

  if (!value || !['Starting', 'FellBackToRemote', 'Stale', 'Failed'].includes(value.state))
    return null

  const minutes = value.nextRetryAt
    ? Math.max(1, Math.ceil((new Date(value.nextRetryAt).getTime() - Date.now()) / 60_000))
    : null
  const hours = Math.max(1, Math.round((value.stalenessSeconds ?? 0) / 3600))

  const when = minutes === null ? t('svirfd.state.soon') : t('svirfd.state.in', {n: minutes})
  const age = t('svirfd.state.hours', {n: hours})

  switch (value.state) {
    case 'Starting':
      return t('svirfd.state.Starting')
    case 'FellBackToRemote':
      return t('svirfd.state.FellBackToRemote', {when})
    case 'Stale':
      return t('svirfd.state.Stale', {age})
    default:
      return t('svirfd.state.Failed', {error: value.lastError})
  }
})

const generation = ref(0)
let written = locationKey(params.value)

function remember(ip: string): void {
  written = locationKey({ip})
  void router.push({query: locationFromParams({ip})})
}

watch(
  () => locationKey(params.value),
  (now) => {
    if (now === written) return

    written = now
    generation.value += 1
  },
)

useJoobyPageContext(() => ({pageName: 'Svirfd', state: 'search', record: undefined}))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="svirfd-page">
    <h1 class="sr-only">{{ t('nav.svirfd') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="svirfd-loading"
    >
      <span class="sr-only">{{ t('svirfd.loading') }}</span>
      <div class="skeleton h-40 w-full"></div>
      <div class="skeleton h-4 w-48"></div>
    </div>

    <div
      v-else-if="!allowed"
      key="refused"
      role="alert"
      class="alert alert-error"
      data-testid="svirfd-refused"
    >
      <span>{{ t('svirfd.refused') }}</span>
    </div>

    <div
      v-else-if="!enabled"
      key="disabled"
      role="status"
      class="alert alert-soft"
      data-testid="svirfd-disabled"
    >
      <span>{{ t('svirfd.disabled') }}</span>
    </div>

    <template v-else>
      <p
        v-if="stateNotice"
        key="state"
        class="alert alert-soft text-sm"
        role="status"
        data-testid="svirfd-state"
      >
        {{ stateNotice }}
      </p>
      <p
        v-if="tenantOff"
        key="tenant-remote-off"
        class="alert alert-info text-sm"
        role="status"
        data-testid="svirfd-tenant-remote-off"
      >
        {{ t('svirfd.tenantRemoteOff') }}
      </p>
      <dl v-if="status.data.value" class="flex flex-wrap gap-2 text-sm" data-testid="svirfd-status">
        <dt class="text-secondary">{{ t('svirfd.status.tenantRemoteLookups') }}</dt>
        <dd class="font-semibold" data-testid="svirfd-status-tenant-remote">
          {{ tenantOff ? t('svirfd.no') : t('svirfd.yes') }}
        </dd>
      </dl>
      <SvirfdSearch
        :key="generation"
        :ip="params.ip"
        :remote-blocked="blocked"
        auto-run
        @search="remember"
      />
    </template>
  </section>
</template>
