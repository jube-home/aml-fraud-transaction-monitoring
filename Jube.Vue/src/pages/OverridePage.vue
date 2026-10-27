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
import { useRoute, useRouter } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import { Info, ShieldAlert } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { hasPermission, meQueryOptions } from '../auth/me'
import { useJoobyContextStore } from '../askjooby/pageContext'
import OverrideManager from '../components/OverrideManager.vue'
import StorySpine from '../components/StorySpine.vue'
import {
  forceOverridePermission,
  levelOf,
  levelsOf,
  normalise,
  overridePermission,
  readAddress,
  reopen,
  sameAddress,
  writeAddress,
  type OverrideAddress,
} from '../overrides/address'
import { overrideApi } from '../overrides/api'
import type { Beat } from '../story/model'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const joobyContext = useJoobyContextStore()
const me = useQuery(meQueryOptions)

const address = ref<OverrideAddress>(readAddress(route.query))
const generation = ref(0)
let written = address.value

const permitted = computed(() => hasPermission(me.data.value, overridePermission))
const canForce = computed(() => hasPermission(me.data.value, forceOverridePermission))
const level = computed(() => levelOf(address.value))

const keyRows = useQuery({
  queryKey: queryKeys.overrideKeys(),
  queryFn: overrideApi.keys,
  enabled: computed(() => permitted.value && address.value.key !== ''),
  retry: false,
})

const keyRow = computed(() =>
  (keyRows.data.value ?? []).find((row) => row.overrideKey === address.value.key),
)

const beats = computed<Beat[]>(() =>
  levelsOf(address.value).map((step): Beat => {
    if (step === 'keys') return { id: 'keys', title: t('nav.override'), facts: [] }

    if (step === 'values') {
      const row = keyRow.value

      return {
        id: `key:${address.value.key}`,
        title: t('override.key'),
        name: address.value.key,
        facts: row
          ? [
              { text: t('override.fact.values', { count: row.values }, row.values) },
              { text: t('override.fact.overrides', { count: row.overrides }, row.overrides) },
              ...(row.forced > 0
                ? [
                    {
                      text: t('override.fact.forced', { count: row.forced }, row.forced),
                      tone: 'warning' as const,
                    },
                  ]
                : []),
            ]
          : [],
      }
    }

    return {
      id: `value:${address.value.value}`,
      title: t('override.keyValue'),
      name: address.value.value,
      facts: [],
    }
  }),
)

function queryOf(next: OverrideAddress): Record<string, string | undefined> {
  return { ...route.query, ...writeAddress(next) } as Record<string, string | undefined>
}

function drill(next: OverrideAddress): void {
  const clean = normalise(next)

  if (sameAddress(clean, written)) return

  written = clean
  address.value = clean
  void router.push({ query: queryOf(clean) })
}

function onChange(scope: { overrideKey: string; overrideKeyValue: string }): void {
  drill({ key: scope.overrideKey, value: scope.overrideKeyValue, model: '' })
}

function onExpand(model: string): void {
  const clean = normalise({ ...written, model })

  if (sameAddress(clean, written)) return

  written = clean
  address.value = clean
  void router.replace({ query: queryOf(clean) })
}

function onReopen(index: number): void {
  drill(reopen(written, index))
}

watch(
  () => route.query,
  (query) => {
    const now = readAddress(query)

    if (sameAddress(now, written)) return

    written = now
    address.value = now
    generation.value += 1
  },
)

watch(
  address,
  (now) => {
    const current = levelOf(now)

    joobyContext.set({
      pageName: 'Override',
      state: current === 'keys' ? 'list' : current === 'values' ? 'search' : 'edit',
      record:
        current === 'keys' ? undefined : { overrideKey: now.key, overrideKeyValue: now.value },
    })
  },
  { immediate: true },
)
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="override-page">
    <h1 class="sr-only">{{ t('nav.override') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="override-loading"
    >
      <span class="sr-only">{{ t('override.page.loading') }}</span>
      <div class="skeleton h-10 w-full"></div>
      <div class="skeleton h-48 w-full"></div>
    </div>

    <div
      v-else-if="me.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error"
      data-testid="override-me-failed"
    >
      <ShieldAlert class="size-5" aria-hidden="true" />
      <span>{{ t('override.page.failed') }}</span>
    </div>

    <div
      v-else-if="!permitted"
      key="denied"
      role="alert"
      class="alert alert-error"
      data-testid="override-denied"
    >
      <ShieldAlert class="size-5" aria-hidden="true" />
      <span>{{ t('override.page.denied') }}</span>
    </div>

    <StorySpine v-else key="story" :beats="beats" @reopen="onReopen">
      <template #default>
        <p
          v-if="level === 'models' && !canForce"
          key="noForce"
          class="flex items-center gap-2 text-sm text-secondary"
          data-testid="override-no-force"
        >
          <Info class="size-4 shrink-0" aria-hidden="true" />
          <span>{{ t('override.page.noForce') }}</span>
        </p>
        <OverrideManager
          :key="generation"
          :override-key="address.key"
          :override-key-value="address.value"
          :expanded-model="address.model"
          :show-trail="false"
          :can-force="canForce"
          @change="onChange"
          @expand="onExpand"
        />
      </template>
    </StorySpine>
  </section>
</template>
