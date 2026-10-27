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
import { computed, ref, useTemplateRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useQuery } from '@tanstack/vue-query'
import { ExternalLink } from 'lucide-vue-next'
import ActivationWatcher from '../../components/ActivationWatcher.vue'
import { toLocalInput } from '../../composables/localDateTime'
import { useFormat } from '../../composables/useFormat'
import type { WatcherSource } from '../../watcher/source'
import type { Activation } from '../../watcher/types'
import { journalApi } from './api'
import { serialiseFilter } from './filter'
import { onImpossibleLegs, toRawPoints } from './map/points'
import { windowFrom, type JournalState, type Lane } from './state'

const props = defineProps<{
  workflow: string
  caseId: number
  primary: Lane
  state: JournalState
  location: { latitude: string; longitude: string } | null
  focus?: string | null
}>()

const emit = defineEmits<{
  select: [guid: string | null]
  openTransaction: [id: number]
}>()

const { t } = useI18n()
const { dateTime } = useFormat()

const now = ref(new Date())
const watcher = useTemplateRef<{ fit: () => void }>('watcher')

const lanes = computed<Lane[]>(() => [props.primary, ...props.state.merge])
const from = computed(() => windowFrom(props.state.days, now.value))
const filterText = computed(() => serialiseFilter(props.state.filter))
const showAll = computed(() => props.state.all && props.state.filter !== null)

const track = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'maptrack',
        props.workflow,
        props.primary.key,
        props.primary.value,
        props.state.days,
        JSON.stringify(props.state.merge),
        filterText.value,
        props.state.speed,
        props.state.distance,
        showAll.value,
        props.caseId,
      ] as const,
  ),
  queryFn: () =>
    journalApi.track({
      caseWorkflowGuid: props.workflow,
      key: props.primary.key,
      value: props.primary.value,
      from: from.value,
      caseId: props.caseId,
      merge: props.state.merge,
      filter: filterText.value,
      limitKmh: props.state.speed,
      minimumKilometres: props.state.distance,
      includeUnmatched: showAll.value,
    }),
  enabled: computed(() => props.location !== null),
  placeholderData: keepPreviousData,
  staleTime: 30_000,
  retry: false,
})

const generation = computed(() =>
  JSON.stringify([
    props.state.days,
    props.state.merge,
    props.state.filter,
    props.state.speed,
    props.state.distance,
    props.state.improbable,
    props.state.all,
    track.dataUpdatedAt.value,
  ]),
)

const mapped = computed(() =>
  toRawPoints(
    track.data.value?.points ?? [],
    { latitude: 'Latitude', longitude: 'Longitude' },
    lanes.value,
    (name, values) => t(name, values ?? {}),
  ),
)

const drawn = computed(() =>
  props.state.improbable
    ? onImpossibleLegs(mapped.value, props.state.speed, props.state.distance)
    : mapped.value,
)

const selected = ref<Activation | null>(null)

const selectedTransaction = computed(() => {
  const activation = selected.value

  if (!activation) return null

  return (
    track.data.value?.points.find((point) =>
      activation.id.endsWith(`|${String(point.EntityAnalysisModelInstanceEntryGuid)}`),
    ) ?? null
  )
})

function choose(activation: Activation | null): void {
  selected.value = activation
  emit(
    'select',
    (selectedTransaction.value?.EntityAnalysisModelInstanceEntryGuid as string | null) ?? null,
  )
}

const source = computed<WatcherSource>(() => ({
  replay: () => Promise.resolve(drawn.value),
}))

defineExpose({ fit: () => watcher.value?.fit() })
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="journal-map">
    <div v-if="track.isError.value" key="trackfailed" role="alert" class="alert alert-error">
      <span>{{ t('journal.map.failed') }}</span>
      <button type="button" class="btn btn-sm" @click="track.refetch()">
        {{ t('chart.retry') }}
      </button>
    </div>
    <div v-else-if="!track.data.value" key="tracking" class="skeleton h-40 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <template v-else>
      <div
        v-if="track.data.value.meta.truncated"
        key="truncated"
        role="status"
        class="alert alert-soft"
        data-testid="journal-map-truncated"
      >
        <span>{{ t('journal.map.truncated') }}</span>
      </div>
      <div
        v-if="selectedTransaction"
        key="selected"
        class="flex flex-wrap items-center gap-3 rounded-box border border-base-300 px-3 py-2 text-sm"
        data-testid="journal-map-selected"
      >
        <span>
          {{
            t('journal.map.selected', {
              when: dateTime(String(selectedTransaction.ReferenceDate ?? '')),
              lane: String(selectedTransaction.Lane ?? 'A'),
            })
          }}
        </span>
        <button
          type="button"
          class="btn btn-sm"
          data-testid="journal-map-open-transaction"
          @click="emit('openTransaction', selectedTransaction.Id)"
        >
          <ExternalLink class="size-4" aria-hidden="true" />
          {{ t('journal.map.openTransaction') }}
        </button>
      </div>
      <ActivationWatcher
        :key="generation"
        ref="watcher"
        tall
        auto-fit
        compact
        mode="all"
        :realtime="false"
        :fade-minutes="0"
        :source="source"
        :trails="state.trails"
        :heat="state.heat"
        :focus-suffix="focus ?? null"
        :travel-limit-kmh="state.speed"
        :minimum-kilometres="state.distance"
        :show-chart="false"
        :show-feed="false"
        :replay-from="toLocalInput(from)"
        :replay-to="toLocalInput(now)"
        @select="choose"
      />
    </template>
  </section>
</template>
