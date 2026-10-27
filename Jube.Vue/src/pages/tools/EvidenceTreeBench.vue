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
import EvidenceTree from '../../components/EvidenceTree.vue'

const { t } = useI18n()

const sample = {
  model: { name: 'Fraud score', version: 4, active: true },
  metrics: { auc: 0.9123456, precision: 0.8, recall: 0.75 },
  variables: [
    { name: 'Amount', weight: 1.25 },
    { name: 'Country', weight: 0 },
  ],
  notes: '',
  reviewer: null,
  flags: [],
  extra: {},
}

const text = ref(JSON.stringify(sample, null, 2))

const parsed = computed<{ value?: unknown; failed: boolean }>(() => {
  try {
    return { value: JSON.parse(text.value) as unknown, failed: false }
  } catch {
    return { failed: true }
  }
})
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <label class="flex flex-col gap-1 text-sm">
      <span class="label">{{ t('evidence.bench.json') }}</span>
      <textarea
        v-model="text"
        rows="8"
        class="textarea w-full font-mono text-xs"
        data-testid="bench-json"
      ></textarea>
    </label>
    <p v-if="parsed.failed" role="alert" class="alert alert-error" data-testid="bench-json-error">
      {{ t('evidence.bench.invalid') }}
    </p>
    <EvidenceTree v-else :evidence="parsed.value" />
  </div>
</template>
