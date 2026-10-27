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
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryKeys } from '../api/queryKeys'
import DashboardTile from './DashboardTile.vue'
import VisualisationParameters from './VisualisationParameters.vue'
import { dashboardApi } from './api'
import { cardParameters, gridStyle, initialValues, missingRequired, orderedTiles } from './state'
import { namedValues, sameValues } from './url'
import type { CardParameter } from '../datasource/types'
import type { ParameterValues } from './types'

const props = withDefaults(
  defineProps<{
    guid: string
    showParameters?: boolean
    showTitle?: boolean
    parameters?: Record<string, string>
    contextId?: number
    autoRun?: boolean
  }>(),
  { showParameters: true, showTitle: true, parameters: () => ({}), autoRun: false },
)

const emit = defineEmits<{ run: [values: Record<string, string>] }>()

const { t } = useI18n()

const registryQuery = useQuery({
  queryKey: computed(() => queryKeys.visualisationRegistry(props.guid)),
  queryFn: () => dashboardApi.registry(props.guid),
  retry: false,
})

const registry = computed(() => registryQuery.data.value)
const registryId = computed(() => registry.value?.id ?? 0)

const definitionsQuery = useQuery({
  queryKey: computed(() => queryKeys.visualisationParameters(registryId.value)),
  queryFn: () => dashboardApi.parameters(registryId.value),
  enabled: computed(() => registryId.value > 0),
})

const tilesQuery = useQuery({
  queryKey: computed(() => queryKeys.visualisationTiles(registryId.value)),
  queryFn: () => dashboardApi.tiles(registryId.value),
  enabled: computed(() => registryId.value > 0),
})

const definitions = computed(() => definitionsQuery.data.value ?? [])
const tiles = computed(() => orderedTiles(tilesQuery.data.value ?? []))

const values = ref<ParameterValues>({})
const applied = ref<CardParameter[] | undefined>()
const missing = ref<number[]>([])
const generation = ref(0)

const ready = computed(() => registry.value !== undefined && definitionsQuery.isSuccess.value)

const missingRegistry = computed(
  () =>
    registryQuery.error.value instanceof ApiError && registryQuery.error.value.kind === 'notFound',
)
const forbidden = computed(
  () =>
    registryQuery.error.value instanceof ApiError && registryQuery.error.value.kind === 'forbidden',
)

function run(announce = true): void {
  const gaps = missingRequired(definitions.value, values.value)

  missing.value = gaps

  if (gaps.length > 0) return

  const next = cardParameters(definitions.value, values.value)

  if (applied.value && JSON.stringify(applied.value) === JSON.stringify(next)) {
    generation.value += 1
  } else {
    applied.value = next
  }

  if (announce) emit('run', namedValues(definitions.value, values.value))
}

function submit(): void {
  run(true)
}

watch(
  () => definitionsQuery.data.value,
  (loaded) => {
    if (!loaded) return

    values.value = initialValues(loaded, new Date(), props.parameters)

    if (
      loaded.length === 0 ||
      props.autoRun ||
      !props.showParameters ||
      Object.keys(props.parameters).length > 0
    ) {
      run(false)
    }
  },
  { immediate: true },
)

watch(
  () => JSON.stringify(props.parameters),
  () => {
    const loaded = definitionsQuery.data.value

    if (!loaded) return

    const next = initialValues(loaded, new Date(), props.parameters)

    if (Object.keys(props.parameters).length === 0) {
      if (loaded.length === 0 || !props.showParameters) return

      values.value = next
      applied.value = undefined
      missing.value = []

      return
    }

    if (applied.value && sameValues(next, values.value)) return

    values.value = next
    run(false)
  },
)

function change(id: number, value: string): void {
  values.value = { ...values.value, [id]: value }
}

defineExpose({ run: submit })
</script>

<template>
  <div class="flex flex-col gap-4" data-testid="visualisation-dashboard">
    <div
      v-if="registryQuery.isPending.value"
      key="loading"
      class="skeleton h-40 w-full"
      data-testid="dashboard-loading"
    ></div>

    <p
      v-else-if="missingRegistry || forbidden"
      key="unavailable"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="dashboard-unavailable"
    >
      {{ forbidden ? t('visualisation.forbidden') : t('visualisation.notFound') }}
    </p>

    <p
      v-else-if="registryQuery.isError.value"
      key="failed"
      role="alert"
      class="alert alert-error text-sm"
      data-testid="dashboard-failed"
    >
      {{ t('visualisation.loadFailed') }}
    </p>

    <template v-else-if="registry && ready">
      <h2 v-if="showTitle" key="title" class="text-lg font-semibold" data-testid="dashboard-title">
        {{ registry.name }}
      </h2>

      <VisualisationParameters
        v-if="showParameters && definitions.length > 0"
        :definitions="definitions"
        :values="values"
        :missing="missing"
        @change="change"
        @run="submit"
      />

      <p
        v-if="tilesQuery.isSuccess.value && tiles.length === 0"
        key="empty"
        class="text-sm text-secondary"
        data-testid="dashboard-empty"
      >
        {{ t('visualisation.empty') }}
      </p>

      <div v-else-if="applied" key="tiles" class="flex flex-col gap-3">
        <div
          class="grid grid-cols-1 gap-3 md:grid-cols-(--dash-columns) md:auto-rows-(--dash-rows)"
          :style="gridStyle(registry)"
          data-testid="dashboard-tiles"
        >
          <DashboardTile
            v-for="tile in tiles"
            :key="tile.id"
            :registry="registry"
            :tile="tile"
            :parameters="applied"
            :generation="generation"
            :context-id="contextId"
          />
        </div>
      </div>

      <p v-else key="waiting" class="text-sm text-secondary" data-testid="dashboard-waiting">
        {{ t('visualisation.waiting') }}
      </p>
    </template>
  </div>
</template>
