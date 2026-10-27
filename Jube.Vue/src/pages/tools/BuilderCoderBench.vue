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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BuilderCoder from '../../components/BuilderCoder.vue'
import BenchValueCard from './BenchValueCard.vue'
import type { BuilderCoderValue } from '../../ruleBuilder/types'

const { t } = useI18n()

const parseType = ref(5)
const modelId = ref(1)
const generation = ref(0)
const value = ref<BuilderCoderValue>()

function reload(): void {
  value.value = undefined
  generation.value += 1
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('ruleBuilder.bench.parseType') }}</span>
        <select
          v-model.number="parseType"
          class="select select-sm"
          data-testid="bench-parse-type"
          @change="reload"
        >
          <option :value="1">{{ t('ruleBuilder.bench.type1') }}</option>
          <option :value="2">{{ t('ruleBuilder.bench.type2') }}</option>
          <option :value="3">{{ t('ruleBuilder.bench.type3') }}</option>
          <option :value="4">{{ t('ruleBuilder.bench.type4') }}</option>
          <option :value="5">{{ t('ruleBuilder.bench.type5') }}</option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('ruleBuilder.bench.modelId') }}</span>
        <input
          v-model.number="modelId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-model-id"
        />
      </label>
      <button type="button" class="btn btn-sm" data-testid="bench-reload" @click="reload">
        {{ t('ruleBuilder.bench.remount') }}
      </button>
    </div>

    <div class="flex flex-1 flex-col">
      <BuilderCoder
        :key="`${parseType}-${modelId}-${generation}`"
        :parse-type="parseType"
        :entity-analysis-model-id="modelId"
        @change="value = $event"
      />
    </div>

    <BenchValueCard :title="t('ruleBuilder.bench.value')" :value="value" />
  </div>
</template>
