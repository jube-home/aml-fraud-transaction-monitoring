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
import {computed, ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {keepPreviousData, useQuery} from '@tanstack/vue-query'
import FieldHelp from '../../components/FieldHelp.vue'
import {journalApi, type JournalQuery} from './api'
import {activationSorts, ownCards, parseSort, sortCards} from './activations'
import ActivationCard from './ActivationCard.vue'

const props = defineProps<{
  workflow: string
  request: JournalQuery
  ownNames: string[]
  ownOnly: boolean
}>()

const emit = defineEmits<{
  anchor: [id: number]
  details: [id: number]
  facts: [id: number]
  own: [on: boolean]
}>()

const {t} = useI18n()

const sort = ref(parseSort(null))

const activations = useQuery({
  queryKey: computed(
    () =>
      [
        'service',
        'Case',
        'keyjournal',
        'activations',
        props.workflow,
        props.request.key,
        props.request.value,
        props.request.from,
        props.request.activationsOnly ?? false,
        JSON.stringify(props.request.merge ?? []),
        props.request.pinnedOnly ?? false,
        props.request.filter ?? '',
        props.request.caseId ?? 0,
      ] as const,
  ),
  queryFn: () => journalApi.activations(props.request),
  placeholderData: keepPreviousData,
  staleTime: 30_000,
  retry: false,
})

const all = computed(() => activations.data.value?.cards ?? [])

const cards = computed(() =>
  sortCards(ownCards(all.value, props.ownNames, props.ownOnly), sort.value),
)

function choose(event: Event): void {
  sort.value = parseSort((event.target as HTMLSelectElement).value)
}

function sortText(name: string): string {
  if (name === 'fired') return t('journal.activations.sort.fired')
  if (name === 'recent') return t('journal.activations.sort.recent')
  if (name === 'name') return t('journal.activations.sort.name')

  return t('journal.activations.sort.score')
}
</script>

<template>
  <section
    class="flex min-w-0 flex-col gap-3"
    aria-labelledby="journal-activations"
    data-testid="journal-activations"
  >
    <div class="flex flex-wrap items-center gap-2">
      <h3 id="journal-activations" class="flex items-center gap-1 text-sm font-semibold">
        {{ t('journal.activations.title') }}
        <FieldHelp :label="t('journal.activations.title')" :text="t('journal.activations.help')"/>
      </h3>
      <span class="badge badge-soft badge-sm font-mono" data-testid="journal-activations-count">{{
          cards.length
        }}</span>

      <div class="ms-auto flex flex-wrap items-center gap-2">
        <button
          type="button"
          class="btn btn-sm"
          :class="{ 'btn-active': ownOnly }"
          :aria-pressed="ownOnly ? 'true' : 'false'"
          :disabled="ownNames.length === 0"
          data-testid="journal-activations-own"
          @click="emit('own', !ownOnly)"
        >
          {{ t('journal.activations.own') }}
        </button>
        <label class="flex items-center gap-2 text-xs">
          <span>{{ t('journal.activations.sort.label') }}</span>
          <select
            class="select select-sm"
            :value="sort"
            data-testid="journal-activations-sort"
            @change="choose"
          >
            <option v-for="name in activationSorts" :key="name" :value="name">
              {{ sortText(name) }}
            </option>
          </select>
        </label>
      </div>
    </div>

    <div v-if="activations.isPending.value" key="loading" class="flex gap-3" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
      <div class="skeleton h-48 w-80 shrink-0"></div>
      <div class="skeleton h-48 w-80 shrink-0"></div>
    </div>
    <div v-else-if="activations.isError.value" key="failed" role="alert" class="alert alert-error">
      <span>{{ t('journal.activations.failed') }}</span>
      <button type="button" class="btn btn-sm" @click="activations.refetch()">
        {{ t('chart.retry') }}
      </button>
    </div>
    <p
      v-else-if="cards.length === 0"
      key="empty"
      class="text-sm text-base-content/70"
      role="status"
      data-testid="journal-activations-empty"
    >
      {{ ownOnly ? t('journal.activations.ownEmpty') : t('journal.activations.empty') }}
    </p>
    <ul
      v-else
      key="band"
      class="flex min-w-0 snap-x gap-3 overflow-x-auto pb-2"
      :aria-label="t('journal.activations.title')"
      data-testid="journal-activations-band"
    >
      <li
        v-for="card in cards"
        :key="`${card.kind}-${card.source ?? ''}-${card.name}`"
        class="w-80 shrink-0 snap-start"
      >
        <ActivationCard
          :card="card"
          :workflow="workflow"
          @anchor="emit('anchor', $event)"
          @details="emit('details', $event)"
          @facts="emit('facts', $event)"
        />
      </li>
    </ul>

    <p
      v-if="activations.data.value?.meta.truncated"
      key="truncated"
      class="text-xs text-base-content/70"
    >
      {{ t('journal.activations.truncated') }}
    </p>
  </section>
</template>
