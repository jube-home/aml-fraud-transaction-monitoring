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
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../../api/queryKeys'
import { hasPermission, meQueryOptions } from '../../auth/me'
import ModelPicker from '../../components/ModelPicker.vue'
import ModelSample from '../../components/ModelSample.vue'
import { sampleApi } from '../../sample/api'

const samplePermission = 40

const me = useQuery(meQueryOptions)
const allowed = computed(() => hasPermission(me.data.value, samplePermission))

const modelId = ref<number | null>(null)
const models = useQuery({
  queryKey: queryKeys.sampleModels(),
  queryFn: sampleApi.models,
  retry: false,
})
const modelGuid = computed(
  () => models.data.value?.find((model) => model.id === modelId.value)?.guid ?? null,
)
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto" data-testid="sample-bench">
    <div class="flex flex-wrap items-end gap-3">
      <ModelPicker v-model="modelId" />
    </div>
    <ModelSample :model-guid="modelGuid" :allowed="allowed" />
  </div>
</template>
