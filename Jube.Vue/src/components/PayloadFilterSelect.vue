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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { useApi } from '../api/useApi'
import type { GridModelFilter } from '../gridModel/types'

const props = defineProps<{ filter: GridModelFilter; modelValue: string }>()

const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const { t } = useI18n()

const lookup = computed(() => props.filter.lookup)

const records = useQuery({
  queryKey: computed(() => queryKeys.service(lookup.value ?? '')),
  queryFn: () => useApi<Record<string, unknown>[]>(`/api/${lookup.value}`),
  enabled: computed(() => lookup.value !== null),
  staleTime: 60_000,
})

const choices = computed(() => {
  if (lookup.value === null) {
    return props.filter.options.map((option) => ({
      value: String(option.value),
      label: option.label,
    }))
  }

  return (records.data.value ?? []).map((row) => ({
    value: String(row.guid ?? row.id),
    label: String(row.name ?? row.guid ?? row.id),
  }))
})
</script>

<template>
  <label class="flex flex-col gap-1">
    <span class="label text-xs font-medium text-secondary">{{ filter.label }}</span>
    <select
      class="select select-sm min-w-44"
      :value="modelValue"
      :data-testid="`payload-filter-${filter.param}`"
      @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
    >
      <option value="">{{ t('payloadGrid.all') }}</option>
      <option v-for="choice in choices" :key="choice.value" :value="choice.value">
        {{ choice.label }}
      </option>
    </select>
  </label>
</template>
