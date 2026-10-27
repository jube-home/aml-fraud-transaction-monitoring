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
import { queryKeys } from '../../api/queryKeys'
import { gridModelApi } from '../../gridModel/api'
import PayloadGrid from '../../components/PayloadGrid.vue'

const { t } = useI18n()

const areas = useQuery({
  queryKey: queryKeys.gridAreas(),
  queryFn: gridModelApi.areas,
  retry: false,
})

const chosen = ref('')
const loaded = ref<string>()

const choices = computed(() => areas.data.value ?? [])

function load(): void {
  loaded.value = chosen.value || choices.value[0]
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('crud.bench.area') }}</span>
        <select v-model="chosen" class="select select-sm min-w-64" data-testid="bench-area">
          <option v-for="name in choices" :key="name" :value="name">{{ name }}</option>
        </select>
      </label>
      <button type="button" class="btn btn-sm" data-testid="bench-load" @click="load">
        {{ t('crud.bench.load') }}
      </button>
    </div>
    <PayloadGrid v-if="loaded" :key="loaded" :area="loaded" />
    <p v-else class="text-sm" data-testid="bench-empty">{{ t('crud.bench.choose') }}</p>
  </div>
</template>
