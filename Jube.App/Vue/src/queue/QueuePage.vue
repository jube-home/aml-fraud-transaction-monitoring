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
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {Inbox} from 'lucide-vue-next'
import FieldHelp from '../components/FieldHelp.vue'
import {useToasts} from '../stores/toasts'
import {queueApi, queueKey} from './queueApi'

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()

const depth = useQuery({
  queryKey: [...queueKey, 'depth'],
  queryFn: () => queueApi.depth(),
  refetchInterval: 10_000,
  retry: false,
})

const dead = useQuery({
  queryKey: [...queueKey, 'dead'],
  queryFn: () => queueApi.dead(),
  refetchInterval: 10_000,
  retry: false,
})

const act = useMutation({
  mutationFn: (work: () => Promise<unknown>) => work(),
  onSuccess: () => {
    toasts.success(t('queue.done'))
    void client.invalidateQueries({queryKey: queueKey})
  },
  onError: (error) => toasts.apiError(error),
})

function requeue(id: number): void {
  act.mutate(() => queueApi.requeue(id))
}

function remove(id: number): void {
  act.mutate(() => queueApi.remove(id))
}
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminQueue-page">
    <h1 class="flex items-center gap-2 text-lg font-semibold">
      <Inbox class="size-4" aria-hidden="true"/>
      {{ t('queue.title') }}
      <FieldHelp :label="t('queue.title')" :text="t('queue.hint')"/>
    </h1>

    <p v-if="depth.isError.value" role="alert" class="text-sm">{{ t('queue.failed') }}</p>
    <div v-else-if="depth.data.value" class="stats stats-horizontal" data-testid="queue-depth">
      <div class="stat">
        <div class="stat-title">{{ t('queue.pending') }}</div>
        <div class="stat-value text-2xl" data-testid="queue-pending">
          {{ depth.data.value.pending }}
        </div>
      </div>
      <div class="stat">
        <div class="stat-title">{{ t('queue.processing') }}</div>
        <div class="stat-value text-2xl">{{ depth.data.value.processing }}</div>
      </div>
      <div class="stat">
        <div class="stat-title">{{ t('queue.dead') }}</div>
        <div class="stat-value text-2xl" data-testid="queue-dead-count">
          {{ depth.data.value.dead }}
        </div>
      </div>
    </div>

    <h2 class="text-sm font-semibold">{{ t('queue.dead') }}</h2>
    <p
      v-if="(dead.data.value ?? []).length === 0"
      class="text-base-content/70 text-sm"
      data-testid="queue-empty"
    >
      {{ t('queue.none') }}
    </p>
    <div v-else class="min-w-0 overflow-auto">
      <table class="table table-sm" data-testid="queue-table">
        <thead class="text-base-content">
        <tr>
          <th>{{ t('queue.model') }}</th>
          <th>{{ t('queue.attempts') }}</th>
          <th>{{ t('queue.error') }}</th>
          <th>{{ t('queue.by') }}</th>
          <th>{{ t('queue.created') }}</th>
          <th>
            <span class="sr-only">{{ t('queue.requeue') }}</span>
          </th>
        </tr>
        </thead>
        <tbody>
        <tr v-for="row in dead.data.value" :key="row.id">
          <td class="font-mono text-xs">{{ row.entityAnalysisModelGuid }}</td>
          <td>{{ row.attempts }}</td>
          <td>{{ row.lastError ?? '' }}</td>
          <td>{{ row.enqueuedBy }}</td>
          <td>{{ row.createdDate.slice(0, 19).replace('T', ' ') }}</td>
          <td class="flex gap-1">
            <button
              type="button"
              class="btn btn-xs"
              :data-testid="`queue-requeue-${row.id}`"
              @click="requeue(row.id)"
            >
              {{ t('queue.requeue') }}
            </button>
            <button
              type="button"
              class="btn btn-xs"
              :data-testid="`queue-delete-${row.id}`"
              @click="remove(row.id)"
            >
              {{ t('queue.delete') }}
            </button>
          </td>
        </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
