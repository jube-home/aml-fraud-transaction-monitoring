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
import {Anchor} from 'lucide-vue-next'
import ServerChart from '../../components/ServerChart.vue'
import {useFormat} from '../../composables/useFormat'
import {useUiStore} from '../../stores/ui'
import {transactionChartBody, transactionChartUrl} from './api'
import {bandKey} from './activations'
import type {ActivationCard} from './types'

const props = defineProps<{ card: ActivationCard; workflow: string }>()

const emit = defineEmits<{
  anchor: [id: number]
  details: [id: number]
  facts: [id: number]
}>()

const {t} = useI18n()
const {dateTime, number, percent} = useFormat()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const isModel = computed(() => props.card.kind === 'model')
const source = computed(() => props.card.source ?? 'exhaustive')

function chartUrl(kind: 'Gauge' | 'Drivers'): string {
  return transactionChartUrl(kind, props.card.name, source.value, dark.value)
}

const band = computed(() => bandKey(props.card.bandId))

const bandText = computed(() => {
  if (band.value === 'veryLikely') return t('journal.activations.band.veryLikely')
  if (band.value === 'likely') return t('journal.activations.band.likely')
  if (band.value === 'possible') return t('journal.activations.band.possible')
  if (band.value === 'unlikely') return t('journal.activations.band.unlikely')

  return ''
})

const bandClass = computed(() => {
  if (band.value === 'veryLikely') return 'badge-error'
  if (band.value === 'likely') return 'badge-warning'
  if (band.value === 'possible') return ''

  return 'badge-soft'
})
</script>

<template>
  <article
    class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 bg-base-100 p-2"
    :data-testid="`activation-card-${card.name}`"
    :aria-label="card.humanLabel ?? card.name"
  >
    <template v-if="isModel">
      <ServerChart
        :url="chartUrl('Gauge')"
        :body="transactionChartBody(workflow, card.anchorId)"
        :label="t('journal.activations.gaugeLabel', { name: card.name })"
        :stale-seconds="300"
      />
      <ServerChart
        v-if="card.drivers.length > 0"
        key="drivers"
        :url="chartUrl('Drivers')"
        :body="transactionChartBody(workflow, card.anchorId)"
        :label="t('journal.activations.driversLabel', { name: card.name })"
        :stale-seconds="300"
        empty-on-refusal
      />
      <p
        v-if="card.observedRate !== null && card.bandSample !== null"
        key="observed"
        class="text-xs text-base-content/70"
        data-testid="activation-card-observed"
      >
        {{
          t('journal.activations.observed', {
            rate: percent(card.observedRate),
            n: number(card.bandSample),
          })
        }}
      </p>
    </template>

    <template v-else>
      <div class="flex flex-wrap items-center gap-2 px-1 pt-1">
        <h4 dir="auto" class="text-sm font-semibold">{{ card.name }}</h4>
        <span class="badge badge-soft badge-sm">{{ t('journal.activations.rule') }}</span>
      </div>
      <dl
        v-if="card.path.length > 0"
        key="path"
        class="flex flex-col gap-1 px-1 text-sm"
        data-testid="activation-card-path"
      >
        <div
          v-for="entry in card.path"
          :key="`${entry.group}-${entry.name}`"
          class="flex items-baseline justify-between gap-3"
        >
          <dt dir="auto" class="min-w-0 truncate text-base-content/70">{{ entry.name }}</dt>
          <dd dir="auto" class="font-mono tabular-nums">{{ String(entry.value ?? '') }}</dd>
        </div>
      </dl>
      <p v-else key="nopath" class="px-1 text-xs text-base-content/70">
        {{ t('journal.activations.noPath') }}
      </p>
      <p v-if="card.hiddenPath > 0" key="hidden" class="px-1 text-xs text-base-content/70">
        {{ t('journal.activations.hidden', {n: number(card.hiddenPath)}) }}
      </p>
    </template>

    <div class="flex flex-wrap items-center gap-2 px-1 text-xs">
      <span v-if="bandText" key="band" class="badge badge-sm" :class="bandClass">{{
          bandText
        }}</span>
      <span v-if="card.forced" key="forced" class="badge badge-warning badge-sm">{{
          t('journal.rule.forced')
        }}</span>
      <span v-if="card.shadow" key="shadow" class="badge badge-ghost badge-sm">{{
          t('journal.rule.shadow')
        }}</span>
      <span class="text-base-content/70">{{
          t('journal.activations.fired', {n: number(card.fired)})
        }}</span>
    </div>

    <div class="flex flex-wrap items-center gap-2 px-1 pb-1">
      <button
        type="button"
        class="btn btn-sm btn-ghost"
        data-testid="activation-card-anchor"
        @click="emit('anchor', card.anchorId)"
      >
        <Anchor class="size-4" aria-hidden="true"/>
        <span dir="auto" class="font-mono">{{
            t('journal.activations.anchor', {id: card.anchorId})
          }}</span>
        <span v-if="card.anchorDate" class="text-base-content/70">{{
            dateTime(card.anchorDate)
          }}</span>
      </button>
      <button
        v-if="isModel"
        key="facts"
        type="button"
        class="btn btn-sm"
        data-testid="activation-card-facts"
        @click="emit('facts', card.anchorId)"
      >
        {{ t('journal.activations.facts') }}
      </button>
      <button
        type="button"
        class="btn btn-sm"
        data-testid="activation-card-details"
        @click="emit('details', card.anchorId)"
      >
        {{ t('journal.activations.details') }}
      </button>
    </div>
  </article>
</template>
