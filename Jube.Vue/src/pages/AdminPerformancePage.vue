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
import { ShieldAlert } from 'lucide-vue-next'
import { meQueryOptions } from '../auth/me'
import PayloadGrid from '../components/PayloadGrid.vue'
import PerformanceCard from '../components/PerformanceCard.vue'
import StorySpine from '../components/StorySpine.vue'
import {
  encodeAddress,
  levelOf,
  levelsOf,
  readAddress,
  reopen,
  sameAddress,
  type PerformanceAddress,
} from '../performance/address'
import { areaById, familyById, performanceFamilies } from '../performance/families'
import type { Beat } from '../story/model'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const address = ref<PerformanceAddress>(readAddress(route.query))
let written = address.value

const level = computed(() => levelOf(address.value))
const family = computed(() => familyById(address.value.family))
const area = computed(() => areaById(address.value.area))

const beats = computed<Beat[]>(() =>
  levelsOf(address.value).map((step): Beat => {
    if (step === 'families') {
      return {
        id: 'families',
        title: t('perfHub.title'),
        facts: [
          {
            text: t(
              'perfHub.families',
              { count: performanceFamilies.length },
              performanceFamilies.length,
            ),
          },
        ],
      }
    }

    if (step === 'areas') {
      const count = family.value?.areas.length ?? 0

      return {
        id: `family:${address.value.family}`,
        title: t('perfHub.familyBeat'),
        name: family.value ? t(family.value.titleKey) : '',
        facts: [{ text: t('perfHub.counters', { count }, count) }],
      }
    }

    return {
      id: `area:${address.value.area}`,
      title: t('perfHub.counterBeat'),
      name: area.value ? t(area.value.titleKey) : '',
      facts: [],
    }
  }),
)

function go(next: PerformanceAddress): void {
  if (sameAddress(next, written)) return

  written = next
  address.value = next
  const path = encodeAddress(next)

  void router.push({ query: { ...route.query, path: path || undefined } })
}

function openFamily(id: string): void {
  go({ family: id, area: '' })
}

function openArea(name: string): void {
  go({ family: address.value.family, area: name })
}

function onReopen(index: number): void {
  go(reopen(written, index))
}

watch(
  () => route.query,
  (query) => {
    const now = readAddress(query)

    if (sameAddress(now, written)) return

    written = now
    address.value = now
  },
)
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminPerformance-page">
    <h1 class="sr-only">{{ t('nav.adminPerformance') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="perf-loading"
    >
      <span class="sr-only">{{ t('perfHub.loading') }}</span>
      <div class="skeleton h-10 w-full"></div>
      <div class="skeleton h-48 w-full"></div>
    </div>

    <div
      v-else-if="me.isError.value || !me.data.value?.landlord"
      key="denied"
      role="alert"
      class="alert alert-error"
      data-testid="perf-denied"
    >
      <ShieldAlert class="size-5" aria-hidden="true" />
      <span>{{ t('perfHub.denied') }}</span>
    </div>

    <StorySpine v-else key="story" :beats="beats" @reopen="onReopen">
      <template #default>
        <div
          v-if="level === 'families'"
          key="families"
          class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3"
          data-testid="perf-families"
        >
          <PerformanceCard
            v-for="item in performanceFamilies"
            :key="item.id"
            :id="item.id"
            :title="t(item.titleKey)"
            :hint="t(item.hintKey)"
            :pill="t('perfHub.counters', { count: item.areas.length }, item.areas.length)"
            :icon="item.icon"
            :open-label="t('perfHub.open')"
            @open="openFamily(item.id)"
          />
        </div>

        <div
          v-else-if="level === 'areas' && family"
          key="areas"
          class="grid gap-3 sm:grid-cols-2 xl:grid-cols-3"
          data-testid="perf-areas"
        >
          <PerformanceCard
            v-for="item in family.areas"
            :key="item.area"
            :id="item.area"
            :title="t(item.titleKey)"
            :icon="family.icon"
            :open-label="t('perfHub.open')"
            @open="openArea(item.area)"
          />
        </div>

        <PayloadGrid v-else-if="area" :key="address.area" :area="address.area" />
      </template>
    </StorySpine>
  </section>
</template>
