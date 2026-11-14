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
import {History} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {useToasts} from '../stores/toasts'
import {
  comparable,
  percent,
  replayApi,
  replayKey,
  replayState,
  windowValid,
  type Comparison,
} from './replayApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const replays = useQuery({
  queryKey: replayKey,
  queryFn: () => replayApi.list(),
  refetchInterval: 10_000,
  retry: false,
})

const from = ref('')
const to = ref('')
const maximum = ref(50)
const asOf = ref(60)
const selected = ref<string[]>([])
const comparison = ref<Comparison | null>(null)

const ready = computed(() => windowValid(from.value, to.value) && maximum.value > 0)

const start = useMutation({
  mutationFn: () =>
    replayApi.start(`${from.value}T00:00:00Z`, `${to.value}T23:59:59Z`, maximum.value, asOf.value),
  onSuccess: () => {
    toasts.success(t('replay.started'))
    void client.invalidateQueries({queryKey: replayKey})
  },
  onError: (error) => toasts.apiError(error),
})

const compare = useMutation({
  mutationFn: () => replayApi.compare(selected.value[0]!, selected.value[1]!),
  onSuccess: (result) => {
    comparison.value = result
  },
  onError: (error) => toasts.apiError(error),
})
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminReplay-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <History class="size-4" aria-hidden="true"/>
      {{ t('replay.title') }}
      <FieldHelp :label="t('replay.title')" :text="t('replay.hint')"/>
    </h1>

    <fieldset class="fieldset" data-testid="replay-form">
      <div class="flex flex-wrap items-end gap-2">
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('replay.from') }}</span>
          <input v-model="from" type="date" class="input input-sm" data-testid="replay-from"/>
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('replay.to') }}</span>
          <input v-model="to" type="date" class="input input-sm" data-testid="replay-to"/>
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('replay.maximum') }}</span>
          <input v-model.number="maximum" type="number" min="1" class="input input-sm w-24"/>
        </label>
        <label class="flex flex-col gap-1">
          <span class="label text-xs">{{ t('replay.asOf') }}</span>
          <input v-model.number="asOf" type="number" min="0" class="input input-sm w-24"/>
        </label>
        <button
          type="button"
          class="btn btn-sm btn-primary"
          :disabled="!ready || start.isPending.value"
          data-testid="replay-start"
          @click="start.mutate()"
        >
          {{ t('replay.start') }}
        </button>
      </div>
    </fieldset>

    <p v-if="replays.isError.value" role="alert" class="text-sm">{{ t('replay.failed') }}</p>
    <p
      v-else-if="(replays.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="replay-empty"
    >
      {{ t('replay.none') }}
    </p>
    <div v-else class="min-w-0 overflow-auto">
      <table class="table table-sm" data-testid="replay-table">
        <thead class="text-base-content">
        <tr>
          <th>
            <span class="sr-only">{{ t('replay.select') }}</span>
          </th>
          <th>{{ t('replay.status') }}</th>
          <th>{{ t('replay.agent') }}</th>
          <th>{{ t('replay.selected') }}</th>
          <th>{{ t('replay.scored') }}</th>
          <th>{{ t('replay.agreed') }}</th>
          <th>{{ t('replay.missed') }}</th>
          <th>{{ t('replay.over') }}</th>
          <th>{{ t('replay.rate') }}</th>
        </tr>
        </thead>
        <tbody>
        <tr v-for="row in replays.data.value" :key="row.guid">
          <td>
            <input
              v-model="selected"
              type="checkbox"
              class="checkbox checkbox-xs"
              :value="row.guid"
              :disabled="row.statusId !== replayState.completed"
              :aria-label="t('replay.select')"
              :data-testid="`replay-select-${row.guid}`"
            />
          </td>
          <td>
              <span class="badge badge-sm" :data-testid="`replay-state-${row.guid}`">{{
                  t(`replay.states.${row.statusId}`)
                }}</span>
          </td>
          <td class="font-mono text-xs">{{ row.agentDefinitionHash.slice(0, 10) }}</td>
          <td>{{ row.casesSelected }}</td>
          <td>{{ row.scored }}</td>
          <td>{{ row.agreed }}</td>
          <td>{{ row.missedRisk }}</td>
          <td>{{ row.overEscalation }}</td>
          <td>{{ percent(row.agreementRate) }}</td>
        </tr>
        </tbody>
      </table>
    </div>

    <div v-if="(replays.data.value ?? []).length > 0" class="flex gap-2">
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!comparable(selected) || compare.isPending.value"
        data-testid="replay-compare"
        @click="compare.mutate()"
      >
        {{ t('replay.compare') }}
      </button>
    </div>

    <p v-if="comparison" class="text-sm" data-testid="replay-comparison">
      {{
        t('replay.comparison', {
          common: comparison.commonCases,
          same: comparison.sameRecommendation,
          first: comparison.firstAgreed,
          second: comparison.secondAgreed,
        })
      }}
    </p>
  </section>
</template>
