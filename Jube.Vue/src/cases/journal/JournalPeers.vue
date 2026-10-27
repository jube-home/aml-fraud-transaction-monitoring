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
import { useFormat } from '../../composables/useFormat'
import FieldHelp from '../../components/FieldHelp.vue'
import { ordinal } from './peers'
import type { JournalField, JournalPeers, PeerComparison } from './types'

const props = defineProps<{ peers: JournalPeers; fields: JournalField[] }>()

const { t } = useI18n()
const { number, fixed, date } = useFormat()

const titles = computed(() => new Map(props.fields.map((field) => [field.name, field.title])))

function show(value: number): string {
  return fixed(value, Number.isInteger(value) ? 0 : 2)
}

function directionText(comparison: PeerComparison): string {
  if (comparison.direction === 'above') return t('journal.peers.above')
  if (comparison.direction === 'below') return t('journal.peers.below')

  return t('journal.peers.typical')
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-3"
    :aria-label="t('journal.peers.title')"
    data-testid="journal-peers"
  >
    <h4 class="flex items-center gap-1 text-sm font-semibold">
      {{ t('journal.peers.title') }}
      <FieldHelp :label="t('journal.peers.title')" :text="t('journal.help.peers')" />
    </h4>

    <p
      v-if="!peers.available"
      key="empty"
      class="text-sm text-base-content/70"
      data-testid="journal-peers-empty"
    >
      {{
        peers.reason === 'NoMeasures'
          ? t('journal.peers.noMeasures')
          : t('journal.peers.notComputed')
      }}
    </p>

    <template v-else>
      <p class="text-xs text-base-content/70" data-testid="journal-peers-basis">
        <template v-if="peers.segmentValue && !peers.segmentFallback">
          {{ t('journal.peers.segment', { name: peers.segment, value: peers.segmentValue }) }}
        </template>
        <template v-else-if="peers.segmentFallback">
          {{ t('journal.peers.fallback', { name: peers.segment, value: peers.segmentValue }) }}
        </template>
        <template v-else>{{ t('journal.peers.everyone') }}</template>
        <template v-if="peers.computedDate">
          {{
            t('journal.peers.built', {
              date: date(peers.computedDate),
              days: number(peers.windowDays ?? 0),
            })
          }}
        </template>
      </p>

      <ul class="flex flex-col gap-2">
        <li
          v-for="comparison in peers.comparisons"
          :key="comparison.field"
          class="flex flex-col gap-1 border-b border-base-300 pb-2"
          :data-testid="`journal-peer-${comparison.field}`"
        >
          <div class="flex flex-wrap items-center justify-between gap-2">
            <span class="text-sm font-semibold">{{
              titles.get(comparison.field) ?? comparison.field
            }}</span>
            <span
              class="badge badge-sm"
              :class="comparison.direction === 'typical' ? 'badge-soft' : 'badge-warning'"
              data-testid="journal-peer-direction"
            >
              {{ directionText(comparison) }}
            </span>
          </div>
          <p class="font-mono text-xs tabular-nums">
            {{
              t('journal.peers.medians', {
                key: show(comparison.keyMedian),
                peers: show(comparison.peerMedian),
              })
            }}
          </p>
          <progress
            class="progress w-full"
            :value="ordinal(comparison.percentile)"
            max="100"
            :aria-label="t('journal.peers.percentile', { n: ordinal(comparison.percentile) })"
          ></progress>
          <p class="text-xs text-base-content/70">
            {{ t('journal.peers.percentile', { n: ordinal(comparison.percentile) }) }}
            {{ t('journal.peers.of', { n: number(comparison.peerKeys) }) }}
            <span v-if="comparison.lowConfidence" key="few">{{ t('journal.peers.few') }}</span>
          </p>
        </li>
      </ul>
    </template>
  </section>
</template>
