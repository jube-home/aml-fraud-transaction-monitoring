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
import { hasPermission, meQueryOptions } from '../../auth/me'
import ModelPicker from '../../components/ModelPicker.vue'
import RuleTools from '../../components/RuleTools.vue'

const { t } = useI18n()
const me = useQuery(meQueryOptions)

const modelId = ref<number | null>(null)
const ruleId = ref<number | null>(null)
const ruleText = ref('Amount > 1000')

const canBacktest = computed(() => hasPermission(me.data.value, 61))
const canCheckIntegrity = computed(() => hasPermission(me.data.value, 60))
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto" data-testid="rule-tools-bench">
    <div class="flex flex-wrap items-end gap-3">
      <ModelPicker v-model="modelId" />
      <label class="flex flex-col gap-1 text-sm">
        <span class="label text-xs">{{ t('ruleTools.bench.ruleId') }}</span>
        <input
          v-model.number="ruleId"
          type="number"
          min="1"
          class="input input-sm w-28 font-mono"
          data-testid="bench-rule-id"
        />
      </label>
      <label class="flex min-w-64 flex-1 flex-col gap-1 text-sm">
        <span class="label text-xs">{{ t('ruleTools.bench.ruleText') }}</span>
        <input
          v-model="ruleText"
          type="text"
          class="input input-sm w-full font-mono"
          data-testid="bench-rule-text"
        />
      </label>
    </div>

    <RuleTools
      v-if="modelId !== null"
      :key="`${modelId}|${ruleId}`"
      :model-id="modelId"
      :rule-id="ruleId"
      :rule-text="ruleText"
      :can-backtest="canBacktest"
      :can-check-integrity="canCheckIntegrity"
    />
  </div>
</template>
