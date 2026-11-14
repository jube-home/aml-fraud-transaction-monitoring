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
import {RotateCw} from 'lucide-vue-next'
import {keepPreviousData, useQuery} from '@tanstack/vue-query'
import {ApiError} from '../api/useApi'
import {queryKeys} from '../api/queryKeys'
import {refusalReasons} from '../approval/model'
import DatasourceCardView from '../datasource/DatasourceCardView.vue'
import type {CardParameter} from '../datasource/types'
import {dashboardApi} from './api'
import {tileSize, tileStyle} from './state'
import type {TileDefinition, VisualisationRegistry} from './types'

const props = defineProps<{
  registry: VisualisationRegistry
  tile: TileDefinition
  parameters: CardParameter[]
  generation: number
  contextId?: number
}>()

const {t} = useI18n()

const query = useQuery({
  queryKey: computed(() =>
    queryKeys.visualisationCard(props.tile.id, props.parameters, props.generation, props.contextId),
  ),
  queryFn: ({signal}) =>
    dashboardApi.card(
      props.tile.id,
      props.parameters,
      tileSize(props.registry, props.tile),
      signal,
      props.contextId,
    ),
  placeholderData: keepPreviousData,
  retry: false,
  staleTime: Infinity,
})

const failure = computed(() => {
  const error = query.error.value

  if (!error) return []
  if (error instanceof ApiError && error.kind === 'forbidden') return [t('visualisation.forbidden')]
  if (error instanceof ApiError && error.kind === 'notFound')
    return [t('visualisation.tileMissing')]
  if (error instanceof ApiError && error.kind === 'validation')
    return refusalReasons(error.body, t('visualisation.tileFailed'))

  return [t('visualisation.tileFailed')]
})
</script>

<template>
  <section
    class="flex min-h-(--tile-height) min-w-0 flex-col md:col-[span_var(--tile-columns)] md:row-[span_var(--tile-rows)] md:min-h-0"
    :style="tileStyle(registry, tile)"
    :aria-label="tile.name ?? ''"
    :data-testid="`tile-${tile.id}`"
  >
    <div
      v-if="failure.length > 0"
      key="failure"
      role="alert"
      class="alert alert-error items-start text-sm"
      :data-testid="`tile-${tile.id}-failure`"
    >
      <ul class="flex flex-col gap-1">
        <li dir="auto" class="font-semibold">{{ tile.name }}</li>
        <li v-for="reason in failure" :key="reason">{{ reason }}</li>
      </ul>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="query.isFetching.value"
        :data-testid="`tile-${tile.id}-retry`"
        @click="query.refetch()"
      >
        <RotateCw aria-hidden="true" class="size-4"/>
        {{ t('visualisation.retry') }}
      </button>
    </div>
    <DatasourceCardView v-else tile :card="query.data.value" :loading="query.isFetching.value"/>
  </section>
</template>
