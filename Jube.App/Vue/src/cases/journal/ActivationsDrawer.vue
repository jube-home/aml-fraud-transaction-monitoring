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
import {computed, onBeforeUnmount, onMounted} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {X} from 'lucide-vue-next'
import ServerChart from '../../components/ServerChart.vue'
import {useFormat} from '../../composables/useFormat'
import {useUiStore} from '../../stores/ui'
import {bandKey} from './activations'
import AdaptationPanel from './AdaptationPanel.vue'
import {journalApi, transactionChartBody, transactionChartUrl} from './api'
import type {ActivationCard, NotableRow} from './types'

const props = defineProps<{ workflow: string; id: number; notable: NotableRow | null }>()

const emit = defineEmits<{ close: []; facts: [id: number] }>()

const {t} = useI18n()
const {fixed, number, percent} = useFormat()
const ui = useUiStore()
const dark = computed(() => ui.resolvedTheme === 'jube-dark')

const fired = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'transactionactivations',
        props.workflow,
        props.id,
      ] as const,
  ),
  queryFn: () => journalApi.transactionActivations(props.workflow, props.id),
  staleTime: 30_000,
  retry: false,
})

const transaction = useQuery({
  queryKey: computed(
    () => ['service', 'Case', 'keyjournal', 'transaction', props.workflow, props.id] as const,
  ),
  queryFn: () => journalApi.transaction(props.workflow, props.id),
  staleTime: 30_000,
  retry: false,
})

const cards = computed(() => fired.data.value?.cards ?? [])
const rules = computed(() => cards.value.filter((card) => card.kind === 'rule'))
const models = computed(() => cards.value.filter((card) => card.kind === 'model'))

function adaptationOf(card: ActivationCard) {
  return (transaction.data.value?.adaptations ?? []).find(
    (item) => item.name === card.name && item.source === card.source,
  )
}

function gaugeUrl(card: ActivationCard): string {
  return transactionChartUrl('Gauge', card.name, card.source ?? 'exhaustive', dark.value)
}

function bandText(card: ActivationCard): string {
  const band = bandKey(card.bandId)

  if (band === 'veryLikely') return t('journal.activations.band.veryLikely')
  if (band === 'likely') return t('journal.activations.band.likely')
  if (band === 'possible') return t('journal.activations.band.possible')
  if (band === 'unlikely') return t('journal.activations.band.unlikely')

  return ''
}

function bandClass(card: ActivationCard): string {
  const band = bandKey(card.bandId)

  if (band === 'veryLikely') return 'badge-error'
  if (band === 'likely') return 'badge-warning'
  if (band === 'possible') return ''

  return 'badge-soft'
}

function groupText(group: string): string {
  if (group === 'payload') return t('journal.group.payload')
  if (group === 'counter') return t('journal.group.counter')
  if (group === 'abstraction') return t('journal.group.abstraction')
  if (group === 'calculation') return t('journal.group.calculation')
  if (group === 'dictionary') return t('journal.group.dictionary')

  return t('journal.group.sanction')
}

function keys(event: KeyboardEvent): void {
  if (event.key === 'Escape') emit('close')
}

onMounted(() => window.addEventListener('keydown', keys))
onBeforeUnmount(() => window.removeEventListener('keydown', keys))
</script>

<template>
  <aside
    class="fixed inset-y-0 end-0 z-30 flex w-full max-w-xl flex-col gap-4 overflow-y-auto border-s border-base-300 bg-base-100 p-4 shadow-lg"
    :aria-label="t('journal.fired.label')"
    data-testid="journal-fired-drawer"
  >
    <div class="flex items-center justify-between gap-2">
      <h3 dir="auto" class="font-mono text-base font-semibold" data-testid="journal-fired-title">
        {{ t('journal.fired.title', {id}) }}
      </h3>
      <button
        type="button"
        class="btn btn-ghost btn-sm btn-square"
        :aria-label="t('journal.fired.close')"
        data-testid="journal-fired-close"
        @click="emit('close')"
      >
        <X class="size-4" aria-hidden="true"/>
      </button>
    </div>

    <div v-if="fired.isPending.value" key="loading" class="flex flex-col gap-2" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-6 w-40"></div>
      <div class="skeleton h-32 w-full"></div>
    </div>

    <div v-else-if="fired.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('journal.fired.failed') }}</span>
    </div>

    <p v-else-if="cards.length === 0" key="none" class="text-sm text-base-content/70" role="status">
      {{ t('journal.fired.none') }}
    </p>

    <template v-else>
      <section
        v-if="rules.length > 0"
        key="rules"
        class="flex flex-col gap-3"
        data-testid="journal-fired-rules"
      >
        <h4 class="text-sm font-semibold">{{ t('journal.fired.rules') }}</h4>
        <article
          v-for="rule in rules"
          :key="rule.name"
          class="flex min-w-0 flex-col gap-2 rounded-box border border-base-300 p-3"
          :data-testid="`journal-fired-rule-${rule.name}`"
        >
          <div class="flex flex-wrap items-center gap-2">
            <h5 dir="auto" class="text-sm font-semibold">{{ rule.name }}</h5>
            <span v-if="rule.forced" class="badge badge-warning badge-sm">{{
                t('journal.rule.forced')
              }}</span>
            <span v-if="rule.shadow" class="badge badge-ghost badge-sm">{{
                t('journal.rule.shadow')
              }}</span>
          </div>
          <dl v-if="rule.path.length > 0" key="path" class="flex flex-col gap-1 text-sm">
            <div
              v-for="entry in rule.path"
              :key="`${entry.group}-${entry.name}`"
              class="flex items-baseline justify-between gap-3"
            >
              <dt dir="auto" class="min-w-0 truncate text-base-content/70">
                {{ entry.name }}
                <span class="text-xs">{{ groupText(entry.group) }}</span>
              </dt>
              <dd dir="auto" class="font-mono tabular-nums">{{ String(entry.value ?? '') }}</dd>
            </div>
          </dl>
          <p v-else key="nopath" class="text-xs text-base-content/70">
            {{ t('journal.activations.noPath') }}
          </p>
          <p v-if="rule.hiddenPath > 0" key="hidden" class="text-xs text-base-content/70">
            {{ t('journal.activations.hidden', {n: number(rule.hiddenPath)}) }}
          </p>
        </article>
      </section>

      <section
        v-if="models.length > 0"
        key="models"
        class="flex flex-col gap-3"
        data-testid="journal-fired-models"
      >
        <h4 class="text-sm font-semibold">{{ t('journal.fired.models') }}</h4>
        <div
          v-for="model in models"
          :key="`${model.source}-${model.name}`"
          class="flex flex-col gap-2"
        >
          <ServerChart
            :url="gaugeUrl(model)"
            :body="transactionChartBody(workflow, id)"
            :label="t('journal.activations.gaugeLabel', { name: model.name })"
            :stale-seconds="300"
          />
          <p class="flex flex-wrap items-center gap-2 text-xs text-base-content/70">
            <span v-if="bandText(model)" class="badge badge-sm" :class="bandClass(model)">{{
                bandText(model)
              }}</span>
            <span v-if="model.observedRate !== null && model.bandSample !== null">
              {{
                t('journal.activations.observed', {
                  rate: percent(model.observedRate),
                  n: number(model.bandSample),
                })
              }}
            </span>
          </p>
          <AdaptationPanel
            v-if="adaptationOf(model)"
            :adaptation="adaptationOf(model)!"
            :workflow="workflow"
            :id="id"
          />
          <button
            type="button"
            class="btn btn-sm self-start"
            :data-testid="`journal-fired-facts-${model.name}`"
            @click="emit('facts', id)"
          >
            {{ t('journal.activations.facts') }}
          </button>
        </div>
      </section>

      <section class="flex flex-col gap-2" data-testid="journal-fired-normal">
        <h4 class="text-sm font-semibold">{{ t('journal.fired.normal') }}</h4>
        <ul v-if="notable && notable.reasons.length > 0" key="reasons" class="flex flex-col gap-1">
          <li v-for="reason in notable.reasons" :key="reason.field" class="text-sm">
            {{
              t('journal.fired.normalRow', {
                field: reason.field,
                value: fixed(reason.value, 2),
                median: fixed(reason.median, 2),
              })
            }}
          </li>
        </ul>
        <p v-else key="plain" class="text-sm text-base-content/70">
          {{ t('journal.fired.normalNone') }}
        </p>
      </section>
    </template>
  </aside>
</template>
