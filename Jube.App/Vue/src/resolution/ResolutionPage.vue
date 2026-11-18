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
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {GitMerge} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {hasPermission, meQueryOptions} from '../auth/me'
import {useToasts} from '../stores/toasts'
import {
  canDecide,
  decidePermission,
  matchDecision,
  maximumNote,
  percent,
  resolutionApi,
  resolutionKey,
} from './resolutionApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)
const mayDecide = computed(() => hasPermission(me.data.value, decidePermission))

const filter = ref<number>(matchDecision.pending)
const notes = ref<Record<number, string>>({})

const matches = useQuery({
  queryKey: [...resolutionKey, filter],
  queryFn: () => resolutionApi.list(filter.value),
  retry: false,
})

const decide = useMutation({
  mutationFn: (input: { id: number; decisionId: number }) =>
    resolutionApi.decide(input.id, input.decisionId, notes.value[input.id]?.trim() || null),
  onSuccess: () => {
    toasts.success(t('resolution.decided'))
    void client.invalidateQueries({queryKey: resolutionKey})
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminResolution-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <GitMerge class="size-4" aria-hidden="true"/>
      {{ t('resolution.title') }}
      <FieldHelp :label="t('resolution.title')" :text="t('resolution.hint')"/>
    </h1>

    <select
      v-model.number="filter"
      class="select select-sm w-fit"
      :aria-label="t('resolution.filter')"
      data-testid="resolution-filter"
    >
      <option v-for="id in [1, 2, 3]" :key="id" :value="id">
        {{ t(`resolution.decisions.${id}`) }}
      </option>
    </select>

    <p v-if="matches.isError.value" role="alert" class="text-sm">{{ t('resolution.failed') }}</p>
    <p
      v-else-if="(matches.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="resolution-empty"
    >
      {{ t('resolution.none') }}
    </p>

    <div
      v-for="match in matches.data.value ?? []"
      :key="match.id"
      class="border-base-300 flex flex-col gap-1 border-b py-2 text-sm"
      :data-testid="`resolution-${match.id}`"
    >
      <div class="flex flex-wrap items-center gap-2">
        <span class="font-mono">{{ match.keyA }} {{ match.valueA }}</span>
        <span class="font-mono">{{ match.keyB }} {{ match.valueB }}</span>
        <span class="badge badge-sm">{{ t(`resolution.sources.${match.sourceId}`) }}</span>
        <span class="text-base-content/70">{{ percent(match.posterior) }}</span>
        <span class="badge badge-sm badge-ghost">{{
            t(`resolution.decisions.${match.decisionId}`)
          }}</span>
      </div>
      <p v-if="match.evidence" class="text-base-content/70">{{ match.evidence }}</p>
      <div v-if="mayDecide && canDecide(match)" class="join w-full">
        <input
          v-model="notes[match.id]"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumNote"
          :placeholder="t('resolution.note')"
          :aria-label="t('resolution.note')"
        />
        <button
          type="button"
          class="btn btn-sm btn-primary join-item"
          :data-testid="`resolution-confirm-${match.id}`"
          @click="decide.mutate({ id: match.id, decisionId: matchDecision.confirmed })"
        >
          {{ t('resolution.confirm') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :data-testid="`resolution-reject-${match.id}`"
          @click="decide.mutate({ id: match.id, decisionId: matchDecision.rejected })"
        >
          {{ t('resolution.reject') }}
        </button>
      </div>
    </div>
  </section>
</template>
