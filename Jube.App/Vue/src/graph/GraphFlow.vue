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
import {Banknote, Rows3, X} from 'lucide-vue-next'
import {useFormat} from '../composables/useFormat'
import type {GraphFlow} from './types'

const props = withDefaults(
  defineProps<{
    data?: GraphFlow
    pending?: boolean
    failed?: boolean
    selectedDepth: number | null
    journal?: boolean
  }>(),
  {pending: false, failed: false, journal: false},
)

const emit = defineEmits<{
  tranche: [depth: number | null]
  filterJournal: [archiveIds: number[]]
  leave: []
}>()

const {t} = useI18n()
const {fixed, dateTime, number} = useFormat()

const nodes = computed(
  () => new Map((props.data?.graph.nodes ?? []).map((node) => [node.id, node])),
)

const passThrough = computed(() => new Set(props.data?.passThroughNodeIds ?? []))

function valueOf(nodeId: number): string {
  return nodes.value.get(nodeId)?.value ?? String(nodeId)
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-3 text-sm"
    data-testid="graph-flow"
  >
    <div class="flex flex-wrap items-center gap-2">
      <Banknote class="size-4" aria-hidden="true"/>
      <h3 class="font-semibold">
        {{ data?.forward === false ? t('graph.flow.backward') : t('graph.flow.forward') }}
      </h3>
      <span
        v-if="data"
        key="count"
        class="badge badge-ghost badge-sm font-mono tabular-nums"
        data-testid="graph-flow-count"
      >
        {{ t('graph.flow.tranches', {count: data.tranches.length}, data.tranches.length) }}
      </span>
      <button
        type="button"
        class="btn btn-ghost btn-sm ms-auto"
        data-testid="graph-flow-leave"
        @click="emit('leave')"
      >
        <X class="size-4" aria-hidden="true"/>
        {{ t('graph.flow.leave') }}
      </button>
    </div>

    <div
      v-if="pending && !data"
      key="loading"
      class="skeleton h-16 w-full"
      role="status"
      :aria-label="t('graph.flow.loading')"
    ></div>
    <p v-else-if="failed && !data" key="failed" role="alert">{{ t('graph.flow.failed') }}</p>
    <p
      v-else-if="data && data.tranches.length === 0"
      key="empty"
      class="text-secondary"
      data-testid="graph-flow-empty"
    >
      {{ t('graph.flow.empty') }}
    </p>
    <template v-else-if="data">
      <ol class="flex flex-col gap-1" data-testid="graph-flow-tranches">
        <li
          v-for="(tranche, index) in data.tranches"
          :key="index"
          class="flex flex-wrap items-center gap-2 rounded-field border border-base-300 px-2 py-1"
          :class="{ 'bg-base-200': selectedDepth === tranche.depth }"
          :data-testid="`graph-tranche-${index}`"
        >
          <button
            type="button"
            class="btn btn-ghost btn-xs"
            :aria-pressed="selectedDepth === tranche.depth"
            @click="emit('tranche', selectedDepth === tranche.depth ? null : tranche.depth)"
          >
            <span class="badge badge-ghost badge-xs font-mono tabular-nums">
              {{ t('graph.flow.hop', {depth: tranche.depth}) }}
            </span>
          </button>
          <span class="font-mono" data-testid="graph-tranche-node">{{
              valueOf(tranche.nodeId)
            }}</span>
          <span
            v-if="passThrough.has(tranche.nodeId)"
            key="pass"
            class="badge badge-warning badge-xs"
            data-testid="graph-tranche-pass-through"
          >
            {{ t('graph.flow.passThrough') }}
          </span>
          <span class="font-mono tabular-nums">{{ fixed(tranche.amount) }}</span>
          <span class="font-mono text-xs text-secondary">{{ dateTime(tranche.at) }}</span>
          <span class="badge badge-ghost badge-xs font-mono tabular-nums">
            {{ t('graph.flow.transactions', {count: number(tranche.archiveIds.length)}) }}
          </span>
          <button
            v-if="journal"
            type="button"
            class="btn btn-xs ms-auto"
            :disabled="tranche.archiveIds.length === 0"
            :data-testid="`graph-tranche-filter-${index}`"
            @click="emit('filterJournal', tranche.archiveIds)"
          >
            <Rows3 class="size-3" aria-hidden="true"/>
            {{ t('graph.flow.filter') }}
          </button>
        </li>
      </ol>
      <ul
        v-if="data.sentences.length > 0"
        key="sentences"
        class="ps-4 text-xs [list-style:disc]"
        data-testid="graph-flow-sentences"
      >
        <li v-for="(sentence, index) in data.sentences" :key="index" dir="auto">{{ sentence }}</li>
      </ul>
    </template>
  </section>
</template>
