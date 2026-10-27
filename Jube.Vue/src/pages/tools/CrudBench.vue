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
import { useRoute, useRouter } from 'vue-router'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../../api/queryKeys'
import { formModelApi } from '../../formModel/api'
import HierarchyStack from '../../components/HierarchyStack.vue'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const areas = useQuery({
  queryKey: queryKeys.formModelAreas(),
  queryFn: formModelApi.areas,
  retry: false,
})

const fromAddress = typeof route.query.area === 'string' ? route.query.area : undefined
const parentFromAddress = Number(route.query.parent)

const area = ref(fromAddress ?? 'EntityAnalysisModelTag')
const parentId = ref<number | undefined>(
  fromAddress
    ? Number.isInteger(parentFromAddress) && parentFromAddress > 0
      ? parentFromAddress
      : undefined
    : 1,
)
const loaded = ref<{ area: string; parentId: number | undefined } | undefined>(
  fromAddress ? { area: fromAddress, parentId: parentId.value } : undefined,
)
const path = typeof route.query.path === 'string' ? route.query.path : undefined

const choices = computed(() => {
  const found = areas.data.value ?? []

  return found.includes(area.value) ? found : [area.value, ...found]
})

function load(): void {
  loaded.value = { area: area.value, parentId: parentId.value || undefined }
  void router.replace({
    query: {
      ...route.query,
      area: area.value,
      parent: parentId.value || undefined,
      path: undefined,
    },
  })
}

function follow(text: string): void {
  void router.replace({ query: { ...route.query, path: text || undefined } })
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('crud.bench.area') }}</span>
        <select v-model="area" class="select select-sm min-w-64" data-testid="bench-area">
          <option v-for="name in choices" :key="name" :value="name">{{ name }}</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('crud.bench.parentId') }}</span>
        <input
          v-model.number="parentId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-parent"
        />
      </label>
      <button type="button" class="btn btn-sm" data-testid="bench-load" @click="load">
        {{ t('crud.bench.load') }}
      </button>
    </div>
    <HierarchyStack
      v-if="loaded"
      :key="`${loaded.area}:${loaded.parentId}`"
      :root-area="loaded.area"
      :root-parent="loaded.parentId"
      :path="path"
      @path="follow"
    />
    <p v-else class="text-sm" data-testid="bench-empty">{{ t('crud.bench.choose') }}</p>
  </div>
</template>
