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
 */

 -->

<script setup vapor lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { datasourceApi } from '../datasource/api'
import { dashboardApi } from './api'
import { parameterType } from './types'

const props = defineProps<{ registryId: number }>()

const { t } = useI18n()

const tiles = useQuery({
  queryKey: computed(() => queryKeys.visualisationTiles(props.registryId)),
  queryFn: () => dashboardApi.tiles(props.registryId),
  retry: false,
})

const parameters = useQuery({
  queryKey: computed(() => queryKeys.visualisationParameters(props.registryId)),
  queryFn: () => dashboardApi.parameters(props.registryId),
  retry: false,
})

const kinds = useQuery({
  queryKey: queryKeys.datasourceChartKinds(),
  queryFn: () => datasourceApi.chartKinds(),
  retry: false,
  staleTime: 5 * 60_000,
})

function kindName(id: number | undefined): string {
  return (
    kinds.data.value?.find((kind) => kind.kindId === id)?.name ??
    t('visualisation.directory.detail.unknownKind')
  )
}

function typeName(id: number): string {
  switch (id) {
    case parameterType.integer:
      return t('visualisation.directory.detail.type.integer')
    case parameterType.decimal:
      return t('visualisation.directory.detail.type.decimal')
    case parameterType.dateTime:
      return t('visualisation.directory.detail.type.dateTime')
    case parameterType.flag:
      return t('visualisation.directory.detail.type.flag')
    default:
      return t('visualisation.directory.detail.type.text')
  }
}
</script>

<template>
  <div class="grid gap-4 p-3 md:grid-cols-2" :data-testid="`directory-detail-${registryId}`">
    <section class="flex flex-col gap-2" :aria-label="t('visualisation.directory.detail.tiles')">
      <h4 class="text-xs font-semibold text-secondary">
        {{ t('visualisation.directory.detail.tiles') }}
      </h4>
      <div v-if="tiles.isPending.value" key="loading" class="skeleton h-12 w-full"></div>
      <p v-else-if="tiles.isError.value" key="failed" class="text-sm">
        {{ t('visualisation.directory.detail.failed') }}
      </p>
      <p
        v-else-if="(tiles.data.value ?? []).length === 0"
        key="none"
        class="text-sm text-secondary"
      >
        {{ t('visualisation.directory.detail.noTiles') }}
      </p>
      <ul v-else key="list" class="flex flex-col gap-1.5">
        <li
          v-for="tile in tiles.data.value"
          :key="tile.id"
          class="flex flex-wrap items-center gap-2 text-sm"
        >
          <span class="font-medium">{{ tile.name }}</span>
          <span class="badge badge-sm badge-soft font-mono">{{
            kindName(tile.displayKindId)
          }}</span>
        </li>
      </ul>
    </section>

    <section
      class="flex flex-col gap-2"
      :aria-label="t('visualisation.directory.detail.parameters')"
    >
      <h4 class="text-xs font-semibold text-secondary">
        {{ t('visualisation.directory.detail.parameters') }}
      </h4>
      <div v-if="parameters.isPending.value" key="loading" class="skeleton h-12 w-full"></div>
      <p v-else-if="parameters.isError.value" key="failed" class="text-sm">
        {{ t('visualisation.directory.detail.failed') }}
      </p>
      <p
        v-else-if="(parameters.data.value ?? []).length === 0"
        key="none"
        class="text-sm text-secondary"
      >
        {{ t('visualisation.directory.detail.noParameters') }}
      </p>
      <ul v-else key="list" class="flex flex-col gap-1.5">
        <li
          v-for="parameter in parameters.data.value"
          :key="parameter.id"
          class="flex flex-wrap items-center gap-2 text-sm"
        >
          <span class="font-medium">{{ parameter.name }}</span>
          <span class="badge badge-sm badge-soft font-mono">{{
            typeName(parameter.dataTypeId)
          }}</span>
          <span
            v-if="parameter.required"
            key="required"
            class="badge badge-sm badge-neutral font-mono"
            >{{ t('visualisation.required') }}</span
          >
        </li>
      </ul>
    </section>
  </div>
</template>
