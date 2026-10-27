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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useFormat } from '../composables/useFormat'
import { sameSource, type CaseSource } from './pageState'
import { viewKeys } from './labels'
import type { CaseCount } from './types'

const props = defineProps<{ counts: CaseCount[]; source: CaseSource }>()

const emit = defineEmits<{
  source: [source: CaseSource]
  newFilter: []
}>()

const { t } = useI18n()
const { number } = useFormat()

const built = computed(() => props.counts.filter((count) => count.kind === 'View'))
const shared = computed(() => props.counts.filter((count) => count.kind === 'Workflow'))
const own = computed(() => props.counts.filter((count) => count.kind === 'Own'))

function sourceOf(count: CaseCount): CaseSource {
  if (count.kind === 'View') return { kind: 'view', view: Number(count.key) }
  if (count.kind === 'Workflow') return { kind: 'workflow', guid: count.key }

  return { kind: 'own', guid: count.key }
}

function active(count: CaseCount): boolean {
  return sameSource(sourceOf(count), props.source)
}

function label(count: CaseCount): string {
  return count.kind === 'View' ? t(viewKeys[Number(count.key)] ?? 'cases.view.all') : count.name
}

function pickOne(event: Event): void {
  const wanted = (event.target as HTMLSelectElement).value
  const found = props.counts.find((count) => `${count.kind}:${count.key}` === wanted)

  if (found) emit('source', sourceOf(found))
}
</script>

<template>
  <nav
    class="flex w-full shrink-0 flex-col gap-3 lg:w-56"
    :aria-label="t('cases.rail.label')"
    data-testid="cases-rail"
  >
    <div class="hidden lg:flex lg:flex-col lg:gap-3">
      <ul
        class="menu menu-sm w-full min-w-0 flex-nowrap rounded-box border border-base-300 bg-base-100 p-1"
        data-testid="cases-filters"
      >
        <li class="menu-title text-base-content opacity-100">{{ t('cases.rail.filters') }}</li>
        <li v-for="count in built" :key="count.key">
          <button
            type="button"
            class="min-w-0"
            :class="{ 'menu-active': active(count) }"
            :aria-current="active(count) ? 'true' : undefined"
            @click="emit('source', sourceOf(count))"
          >
            <span class="min-w-0 grow truncate text-start" :title="label(count)">{{
              label(count)
            }}</span>
            <span class="badge badge-soft badge-sm shrink-0 font-mono">{{
              number(count.count)
            }}</span>
          </button>
        </li>
        <li v-for="count in shared" :key="count.key">
          <button
            type="button"
            class="min-w-0"
            :class="{ 'menu-active': active(count) }"
            :aria-current="active(count) ? 'true' : undefined"
            @click="emit('source', sourceOf(count))"
          >
            <span class="min-w-0 grow truncate text-start" :title="label(count)">{{
              label(count)
            }}</span>
            <span
              v-if="count.failed"
              key="failed"
              class="badge badge-soft badge-sm shrink-0 font-mono"
            >
              {{ t('cases.rail.failed') }}
            </span>
            <span v-else key="count" class="badge badge-soft badge-sm shrink-0 font-mono">
              {{ number(count.count) }}
            </span>
          </button>
        </li>
      </ul>

      <ul
        class="menu menu-sm w-full min-w-0 flex-nowrap rounded-box border border-base-300 bg-base-100 p-1"
        data-testid="cases-own"
      >
        <li class="menu-title text-base-content opacity-100">{{ t('cases.rail.own') }}</li>
        <li v-for="count in own" :key="count.key">
          <button
            type="button"
            class="min-w-0"
            :class="{ 'menu-active': active(count) }"
            :aria-current="active(count) ? 'true' : undefined"
            @click="emit('source', sourceOf(count))"
          >
            <span class="min-w-0 grow truncate text-start" :title="label(count)">{{
              label(count)
            }}</span>
            <span
              v-if="count.failed"
              key="failed"
              class="badge badge-soft badge-sm shrink-0 font-mono"
            >
              {{ t('cases.rail.failed') }}
            </span>
            <span v-else key="count" class="badge badge-soft badge-sm shrink-0 font-mono">
              {{ number(count.count) }}
            </span>
          </button>
        </li>
        <li>
          <button type="button" data-testid="cases-new-filter" @click="emit('newFilter')">
            {{ t('cases.rail.newFilter') }}
          </button>
        </li>
      </ul>
    </div>

    <div class="flex items-end gap-2 lg:hidden">
      <label class="flex min-w-0 grow flex-col gap-1">
        <span class="label text-xs">{{ t('cases.rail.source') }}</span>
        <select class="select select-sm w-full" data-testid="cases-source-select" @change="pickOne">
          <option
            v-for="count in counts"
            :key="`${count.kind}:${count.key}`"
            :value="`${count.kind}:${count.key}`"
            :selected="active(count)"
          >
            {{ label(count) }} ({{ number(count.count) }})
          </option>
        </select>
      </label>
      <button type="button" class="btn btn-sm" @click="emit('newFilter')">
        {{ t('cases.rail.newFilter') }}
      </button>
    </div>
  </nav>
</template>
