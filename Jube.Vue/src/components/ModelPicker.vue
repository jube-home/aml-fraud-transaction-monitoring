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
import { computed, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { integrityApi } from '../integrity/api'
import type { IntegrityModel } from '../integrity/types'

const props = withDefaults(defineProps<{ modelValue: number | null; autoSelect?: boolean }>(), {
  autoSelect: true,
})

const emit = defineEmits<{
  'update:modelValue': [id: number | null]
  loaded: [models: IntegrityModel[]]
}>()

const { t } = useI18n()
const id = useId()

const models = useQuery({
  queryKey: queryKeys.integrityModels(),
  queryFn: integrityApi.models,
  retry: false,
})

const list = computed(() => models.data.value ?? [])

watch(
  list,
  (value) => {
    emit('loaded', value)

    if (props.autoSelect && props.modelValue === null && value.length > 0) {
      emit('update:modelValue', value[0]!.id)
    }
  },
  { immediate: true },
)

function pick(event: Event): void {
  const value = (event.target as HTMLSelectElement).value

  emit('update:modelValue', value === '' ? null : Number(value))
}
</script>

<template>
  <label class="flex flex-col gap-1" :for="id">
    <span class="label text-xs">{{ t('integrity.model') }}</span>
    <select
      :id="id"
      class="select select-sm min-w-64"
      :value="modelValue === null ? '' : String(modelValue)"
      :disabled="models.isPending.value"
      data-testid="model-picker"
      @change="pick"
    >
      <option v-if="modelValue === null" key="none" value="">
        {{ t('integrity.chooseModel') }}
      </option>
      <option v-for="model in list" :key="model.id" :value="String(model.id)">
        {{ model.name }}{{ model.active ? '' : ` (${t('integrity.inactive')})` }}
      </option>
    </select>
  </label>
</template>
