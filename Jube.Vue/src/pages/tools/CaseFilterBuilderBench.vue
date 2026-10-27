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
import CaseFilterBuilder, { type CaseFilterValue } from '../../components/CaseFilterBuilder.vue'
import type { CaseWorkflowRef } from '../../filterBuilder/api'
import BenchValueCard from './BenchValueCard.vue'

const { t } = useI18n()

const workflow = ref('1')
const generation = ref(0)
const value = ref<CaseFilterValue>()

const reference = computed<CaseWorkflowRef>(() =>
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(workflow.value.trim())
    ? { kind: 'guid', value: workflow.value.trim() }
    : { kind: 'id', value: Number(workflow.value) || 0 },
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
        <span class="label">{{ t('filterBuilder.bench.workflowId') }}</span>
        <input
          v-model="workflow"
          type="text"
          class="input input-sm w-72 font-mono"
          data-testid="bench-workflow"
        />
      </label>
      <button type="button" class="btn btn-sm" data-testid="bench-reload" @click="reload">
        {{ t('ruleBuilder.bench.remount') }}
      </button>
    </div>

    <CaseFilterBuilder
      :key="`${workflow}-${generation}`"
      :case-workflow="reference"
      @change="value = $event"
    />

    <BenchValueCard :title="t('filterBuilder.bench.value')" :value="value" />
  </div>
</template>
