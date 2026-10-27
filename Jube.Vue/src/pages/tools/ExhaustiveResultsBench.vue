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
import ExhaustiveResults from '../../components/ExhaustiveResults.vue'
import ModelPicker from '../../components/ModelPicker.vue'

interface Adaptation {
  id: number
  name: string | null
}

const { t } = useI18n()

const modelId = ref<number | null>(null)
const picked = ref<number | null>(null)

const adaptations = useQuery({
  queryKey: computed(() => ['exhaustive', 'bench', modelId.value] as const),
  queryFn: () =>
    useApi<Adaptation[]>(`/api/ExhaustiveSearchInstance/ByEntityAnalysisModelId/${modelId.value}`),
  enabled: computed(() => modelId.value !== null),
  retry: false,
})

const list = computed(() => adaptations.data.value ?? [])
const current = computed(() => picked.value ?? list.value[0]?.id ?? null)

function choose(event: Event): void {
  picked.value = Number((event.target as HTMLSelectElement).value)
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto" data-testid="exhaustive-bench">
    <div class="flex flex-wrap items-end gap-3">
      <ModelPicker v-model="modelId" />
      <label class="flex flex-col gap-1">
        <span class="label text-xs">{{ t('exhaustive.bench.adaptation') }}</span>
        <select
          class="select select-sm min-w-64"
          :value="current ?? ''"
          data-testid="exhaustive-bench-adaptation"
          @change="choose"
        >
          <option v-for="adaptation in list" :key="adaptation.id" :value="adaptation.id">
            {{ adaptation.name }}
          </option>
        </select>
      </label>
    </div>
    <ExhaustiveResults v-if="current !== null" :key="current" :instance-id="current" />
    <p v-else key="none" class="text-sm text-secondary">{{ t('exhaustive.bench.none') }}</p>
  </div>
</template>
