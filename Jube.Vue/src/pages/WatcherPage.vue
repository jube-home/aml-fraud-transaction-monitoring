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
import { useQuery } from '@tanstack/vue-query'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { hasPermission, meQueryOptions } from '../auth/me'
import ActivationWatcher from '../components/ActivationWatcher.vue'
import {
  parsePageState,
  sameState,
  serialisePageState,
  type WatcherPageState,
  type WatcherView,
} from '../watcher/pageState'

const watcherPermission = 30

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const me = useQuery(meQueryOptions)

const allowed = computed(() => hasPermission(me.data.value, watcherPermission))

const state = ref<WatcherPageState>(parsePageState(route.query))
const generation = ref(0)
const draftKey = ref(state.value.key)
const draftValue = ref(state.value.keyValue)

const filtered = computed(() => state.value.key !== '' || state.value.keyValue !== '')
const filterLabel = computed(() =>
  [state.value.key, state.value.keyValue].filter((part) => part !== '').join(':'),
)

function write(next: WatcherPageState, push: boolean): void {
  state.value = next

  const location = { query: serialisePageState(next) }

  if (push) void router.push(location)
  else void router.replace(location)
}

function view(next: WatcherView): void {
  const merged = { ...state.value, ...next }

  if (!sameState(merged, state.value)) write(merged, false)
}

function apply(): void {
  const next = {
    ...state.value,
    key: draftKey.value.trim(),
    keyValue: draftValue.value.trim(),
    from: '',
    to: '',
  }

  if (sameState(next, state.value)) return

  generation.value += 1
  write(next, true)
}

function clear(): void {
  draftKey.value = ''
  draftValue.value = ''
  apply()
}

watch(
  () => route.query,
  (query) => {
    const incoming = parsePageState(query)

    if (sameState(incoming, state.value)) return

    state.value = incoming
    draftKey.value = incoming.key
    draftValue.value = incoming.keyValue
    generation.value += 1
  },
)
</script>

<template>
  <section class="flex flex-col gap-3" data-testid="watcher-page">
    <h1 class="sr-only">{{ t('nav.watcher') }}</h1>

    <p
      v-if="me.isPending.value"
      key="loading"
      role="status"
      class="flex items-center gap-2 text-sm"
      data-testid="watcher-page-loading"
    >
      <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
      {{ t('watcher.page.loading') }}
    </p>

    <p
      v-else-if="me.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error"
      data-testid="watcher-page-failed"
    >
      {{ t('watcher.page.loadFailed') }}
    </p>

    <p
      v-else-if="!allowed"
      key="refused"
      role="alert"
      class="alert alert-warning"
      data-testid="watcher-page-refused"
    >
      {{ t('watcher.page.refused') }}
    </p>

    <template v-else>
      <form
        key="filter"
        class="flex flex-wrap items-end gap-x-4 gap-y-2"
        :aria-label="t('watcher.page.filter')"
        data-testid="watcher-page-filter"
        @submit.prevent="apply"
      >
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.page.key') }}</span>
          <input
            v-model="draftKey"
            type="text"
            class="input input-sm"
            autocomplete="off"
            data-testid="watcher-page-key"
          />
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('watcher.page.keyValue') }}</span>
          <input
            v-model="draftValue"
            type="text"
            class="input input-sm"
            autocomplete="off"
            data-testid="watcher-page-value"
          />
        </label>
        <div class="flex items-center gap-2">
          <button type="submit" class="btn btn-sm" data-testid="watcher-page-apply">
            {{ t('watcher.page.apply') }}
          </button>
          <button
            v-if="filtered"
            key="clear"
            type="button"
            class="btn btn-sm btn-ghost"
            data-testid="watcher-page-clear"
            @click="clear"
          >
            {{ t('watcher.page.clearFilter') }}
          </button>
          <span
            v-if="filtered"
            key="badge"
            class="badge badge-soft font-mono"
            data-testid="watcher-page-filtered"
            >{{ filterLabel }}</span
          >
        </div>
      </form>

      <h2 class="sr-only">{{ t('watcher.mapLabel') }}</h2>

      <ActivationWatcher
        :key="generation"
        tall
        :realtime="state.live"
        :mode="state.mode"
        :window-minutes="state.window"
        :fade-minutes="state.fade"
        :trails="state.trails"
        :heat="state.heat"
        :replay-from="state.from"
        :replay-to="state.to"
        :key-filter="state.key || undefined"
        :key-value-filter="state.keyValue || undefined"
        @view="view"
      />
    </template>
  </section>
</template>
