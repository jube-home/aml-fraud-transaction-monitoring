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
import ApprovalPanel from '../../components/ApprovalPanel.vue'
import { useFormModels } from '../../formModel/useFormModels'

const { t } = useI18n()

const { withApproval } = useFormModels()

const area = ref('')
const entityId = ref(1)
const loaded = ref<{ area: string; entityId: number }>()

const chosen = computed(() => area.value || withApproval.value[0]?.area || '')
const model = computed(() =>
  withApproval.value.find((candidate) => candidate.area === loaded.value?.area),
)
const valid = computed(
  () => Number.isInteger(entityId.value) && entityId.value > 0 && chosen.value !== '',
)

function load(): void {
  loaded.value = { area: chosen.value, entityId: entityId.value }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('approval.bench.endpoint') }}</span>
        <select v-model="area" class="select select-sm" data-testid="bench-endpoint">
          <option v-for="candidate in withApproval" :key="candidate.area" :value="candidate.area">
            {{ candidate.area }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('approval.bench.entityId') }}</span>
        <input
          v-model.number="entityId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-entity"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!valid"
        data-testid="bench-load"
        @click="load"
      >
        {{ t('approval.bench.load') }}
      </button>
    </div>
    <ApprovalPanel
      v-if="loaded && model?.approval"
      :key="`${loaded.area}:${loaded.entityId}`"
      :endpoint="model.endpoint"
      :approval="model.approval"
      :entity-id="loaded.entityId"
    />
    <p v-else class="text-sm" data-testid="bench-empty">{{ t('approval.bench.choose') }}</p>
  </div>
</template>
