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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { useApi } from '../../api/useApi'
import { queryKeys } from '../../api/queryKeys'
import DatasourceBuilder from '../../datasource/DatasourceBuilder.vue'
import type { DatasourceValue, MappingEntry } from '../../datasource/types'
import BenchValueCard from './BenchValueCard.vue'

interface Named {
  id: number
  name: string | null
}

interface Stored extends Named {
  command: string | null
  displayKindId: number
  includeGrid: boolean
  mapping: MappingEntry[]
  templateText: string | null
}

const { t } = useI18n()

const registryId = ref(0)
const datasourceId = ref(0)
const generation = ref(0)
const value = ref<DatasourceValue>()

const registries = useQuery({
  queryKey: queryKeys.datasourceRegistries(),
  queryFn: () => useApi<Named[]>('/api/VisualisationRegistry'),
})

const datasources = useQuery({
  queryKey: computed(() => queryKeys.datasourceList(registryId.value)),
  queryFn: () =>
    useApi<Named[]>(
      `/api/VisualisationRegistryDatasource/ByVisualisationRegistryId/${registryId.value}`,
    ),
  enabled: computed(() => registryId.value > 0),
})

const record = useQuery({
  queryKey: computed(() => queryKeys.datasourceRecord(datasourceId.value)),
  queryFn: () => useApi<Stored>(`/api/VisualisationRegistryDatasource/${datasourceId.value}`),
  enabled: computed(() => datasourceId.value > 0),
})

const initial = computed<DatasourceValue | undefined>(() =>
  datasourceId.value > 0 && record.data.value
    ? {
        command: record.data.value.command ?? '',
        displayKindId: record.data.value.displayKindId,
        includeGrid: record.data.value.includeGrid,
        mapping: record.data.value.mapping ?? [],
        templateText: record.data.value.templateText ?? '',
        valid: false,
      }
    : undefined,
)

const ready = computed(
  () => registryId.value > 0 && (datasourceId.value === 0 || !!record.data.value),
)

function pickRegistry(): void {
  datasourceId.value = 0
  value.value = undefined
  generation.value += 1
}

function pickDatasource(): void {
  value.value = undefined
  generation.value += 1
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('datasource.bench.registry') }}</span>
        <select
          v-model.number="registryId"
          class="select select-sm"
          data-testid="bench-registry"
          @change="pickRegistry"
        >
          <option :value="0">{{ t('datasource.bench.pickRegistry') }}</option>
          <option v-for="item in registries.data.value ?? []" :key="item.id" :value="item.id">
            {{ item.name }}
          </option>
        </select>
      </label>
      <label v-if="registryId > 0" key="datasource" class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('datasource.bench.datasource') }}</span>
        <select
          v-model.number="datasourceId"
          class="select select-sm"
          data-testid="bench-datasource"
          @change="pickDatasource"
        >
          <option :value="0">{{ t('datasource.bench.new') }}</option>
          <option v-for="item in datasources.data.value ?? []" :key="item.id" :value="item.id">
            {{ item.name }}
          </option>
        </select>
      </label>
    </div>

    <DatasourceBuilder
      v-if="ready"
      :key="`${registryId}-${datasourceId}-${generation}`"
      :registry-id="registryId"
      :initial-value="initial"
      @change="value = $event"
    />
    <p v-else key="pick" class="text-sm text-secondary" data-testid="bench-pick">
      {{ t('datasource.bench.pick') }}
    </p>

    <BenchValueCard :title="t('datasource.bench.value')" :value="value" />
  </div>
</template>
