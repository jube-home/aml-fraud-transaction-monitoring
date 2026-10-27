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
import ExhaustiveFilterBuilder, {
  type ExhaustiveFilterValue,
} from '../../components/ExhaustiveFilterBuilder.vue'
import type { ExhaustiveSource } from '../../filterBuilder/api'
import BenchValueCard from './BenchValueCard.vue'

const { t } = useI18n()

const kind = ref<'model' | 'Csv' | 'Json'>('model')
const modelId = ref(1)
const datasetId = ref(1)
const generation = ref(0)
const value = ref<ExhaustiveFilterValue>()

const source = computed<ExhaustiveSource>(() =>
  kind.value === 'model'
    ? { kind: 'model', modelId: modelId.value }
    : { kind: 'dataset', datasetKind: kind.value, id: datasetId.value },
)

function reload(): void {
  value.value = undefined
  generation.value += 1
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('filterBuilder.bench.source') }}</span>
        <select v-model="kind" class="select select-sm" data-testid="bench-source" @change="reload">
          <option value="model">{{ t('filterBuilder.bench.sourceModel') }}</option>
          <option value="Csv">{{ t('filterBuilder.bench.sourceCsv') }}</option>
          <option value="Json">{{ t('filterBuilder.bench.sourceJson') }}</option>
        </select>
      </label>
      <label v-if="kind === 'model'" class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('filterBuilder.bench.modelId') }}</span>
        <input
          v-model.number="modelId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-model-id"
        />
      </label>
      <label v-else class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('filterBuilder.bench.datasetId') }}</span>
        <input
          v-model.number="datasetId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-dataset-id"
        />
      </label>
      <button type="button" class="btn btn-sm" data-testid="bench-reload" @click="reload">
        {{ t('ruleBuilder.bench.remount') }}
      </button>
    </div>

    <ExhaustiveFilterBuilder
      :key="`${kind}-${modelId}-${datasetId}-${generation}`"
      :source="source"
      @change="value = $event"
    />

    <BenchValueCard :title="t('filterBuilder.bench.value')" :value="value" />
  </div>
</template>
