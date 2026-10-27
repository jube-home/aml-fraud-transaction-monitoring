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
import ValueGrid from '../../components/ValueGrid.vue'
import { useFormModels } from '../../formModel/useFormModels'
import { valueConfigOf } from '../../valueGrid/model'

const { t } = useI18n()

const { withValues } = useFormModels()

const area = ref('')
const parentId = ref(1)
const loaded = ref<{ area: string; parentId: number }>()

const chosen = computed(() => area.value || withValues.value[0]?.area || '')
const config = computed(() => {
  const values = withValues.value.find((candidate) => candidate.area === loaded.value?.area)?.values

  return values ? valueConfigOf(values) : undefined
})
const valid = computed(
  () => Number.isInteger(parentId.value) && parentId.value > 0 && chosen.value !== '',
)

function load(): void {
  loaded.value = { area: chosen.value, parentId: parentId.value }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('valueGrid.bench.kind') }}</span>
        <select v-model="area" class="select select-sm" data-testid="bench-kind">
          <option v-for="candidate in withValues" :key="candidate.area" :value="candidate.area">
            {{ candidate.area }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('valueGrid.bench.parentId') }}</span>
        <input
          v-model.number="parentId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-parent"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!valid"
        data-testid="bench-load"
        @click="load"
      >
        {{ t('valueGrid.bench.load') }}
      </button>
    </div>
    <ValueGrid
      v-if="loaded && config"
      :key="`${loaded.area}:${loaded.parentId}`"
      :config="config"
      :parent-id="loaded.parentId"
    />
    <p v-else class="text-sm" data-testid="bench-empty">{{ t('valueGrid.bench.choose') }}</p>
  </div>
</template>
