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
import {ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {MoveRight, Route, Search, X} from 'lucide-vue-next'
import {useFormat} from '../composables/useFormat'
import type {GraphPaths} from './types'

withDefaults(
  defineProps<{
    picking: boolean
    data?: GraphPaths
    pending?: boolean
    failed?: boolean
    notFound?: boolean
    selectedIndex: number | null
  }>(),
  {pending: false, failed: false, notFound: false},
)

const emit = defineEmits<{
  cancel: []
  clear: []
  pick: [index: number | null]
  target: [target: { key: string; value: string }]
}>()

const {t} = useI18n()
const {fixed} = useFormat()

const key = ref('')
const value = ref('')

function find(): void {
  const k = key.value.trim()
  const v = value.value.trim()

  if (k !== '' && v !== '') emit('target', {key: k, value: v})
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-3 text-sm"
    data-testid="graph-paths"
  >
    <div class="flex flex-wrap items-center gap-2">
      <Route class="size-4" aria-hidden="true"/>
      <h3 class="font-semibold">{{ t('graph.paths.title') }}</h3>
      <span
        v-if="data?.from && data?.to"
        key="ends"
        class="badge badge-ghost badge-sm gap-1 font-mono"
        data-testid="graph-paths-ends"
      >
        <span dir="ltr">{{ data.from.value }}</span>
        <MoveRight class="size-3 rtl:rotate-180" aria-hidden="true"/>
        <span dir="ltr">{{ data.to.value }}</span>
      </span>
      <button
        type="button"
        class="btn btn-ghost btn-sm ms-auto"
        data-testid="graph-paths-close"
        @click="picking ? emit('cancel') : emit('clear')"
      >
        <X class="size-4" aria-hidden="true"/>
        {{ picking ? t('graph.paths.cancel') : t('graph.paths.clear') }}
      </button>
    </div>

    <template v-if="picking">
      <p class="text-secondary" role="status" data-testid="graph-paths-picking">
        {{ t('graph.paths.picking') }}
      </p>
      <form class="flex flex-wrap items-end gap-2" @submit.prevent="find">
        <label class="input input-sm w-44">
          <span class="label">{{ t('graph.paths.key') }}</span>
          <input v-model="key" type="text" class="font-mono" data-testid="graph-paths-key"/>
        </label>
        <label class="input input-sm w-56">
          <span class="label">{{ t('graph.paths.value') }}</span>
          <input v-model="value" type="text" class="font-mono" data-testid="graph-paths-value"/>
        </label>
        <button type="submit" class="btn btn-sm" data-testid="graph-paths-find">
          <Search class="size-4" aria-hidden="true"/>
          {{ t('graph.paths.find') }}
        </button>
      </form>
      <p v-if="notFound" key="notFound" role="alert" class="text-xs">
        {{ t('graph.paths.notFound') }}
      </p>
    </template>

    <div
      v-else-if="pending && !data"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('graph.paths.loading')"
    ></div>
    <p v-else-if="failed && !data" key="failed" role="alert">{{ t('graph.paths.failed') }}</p>
    <p
      v-else-if="data && data.paths.length === 0"
      key="empty"
      class="text-secondary"
      data-testid="graph-paths-empty"
    >
      {{ t('graph.paths.empty') }}
    </p>
    <template v-else-if="data">
      <ul class="menu w-full p-0" data-testid="graph-paths-list">
        <li>
          <button
            type="button"
            :class="{ 'menu-active': selectedIndex === null }"
            data-testid="graph-paths-all"
            @click="emit('pick', null)"
          >
            {{ t('graph.paths.all', {count: data.paths.length}) }}
          </button>
        </li>
        <li v-for="(path, index) in data.paths" :key="index">
          <button
            type="button"
            class="flex flex-wrap items-center gap-2"
            :class="{ 'menu-active': selectedIndex === index }"
            :data-testid="`graph-path-${index}`"
            @click="emit('pick', index)"
          >
            <span class="badge badge-ghost badge-xs font-mono tabular-nums">
              {{ t('graph.paths.path', {hops: path.hops, weight: fixed(path.weight)}) }}
            </span>
            <span dir="auto" class="text-xs">{{ path.sentence }}</span>
          </button>
        </li>
      </ul>
      <ul
        v-if="data.sentences.length > 0"
        key="sentences"
        class="ps-4 text-xs [list-style:disc]"
        data-testid="graph-paths-sentences"
      >
        <li v-for="(sentence, index) in data.sentences" :key="index" dir="auto">{{ sentence }}</li>
      </ul>
    </template>
  </section>
</template>
