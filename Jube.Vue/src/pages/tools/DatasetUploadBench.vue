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
import DatasetUpload from '../../components/DatasetUpload.vue'

const datasetPermission = 16

const { t } = useI18n()
const me = useQuery(meQueryOptions)
const allowed = computed(() => hasPermission(me.data.value, datasetPermission))

const value = ref('')
const locked = ref(false)
</script>

<template>
  <div
    class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto"
    data-testid="dataset-upload-bench"
  >
    <label class="flex items-center gap-2 text-sm">
      <input
        v-model="locked"
        type="checkbox"
        class="toggle toggle-sm"
        data-testid="dataset-bench-locked"
      />
      <span>{{ t('dataset.bench.locked') }}</span>
    </label>
    <DatasetUpload v-model="value" :locked="locked" :allowed="allowed" />
    <p class="text-sm text-secondary">
      {{ t('dataset.bench.reference') }}
      <span class="font-mono" data-testid="dataset-bench-value">{{ value || '–' }}</span>
    </p>
  </div>
</template>
