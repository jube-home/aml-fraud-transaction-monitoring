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
import { useI18n } from 'vue-i18n'
import { Play } from 'lucide-vue-next'
import { parameterType, type ParameterDefinition, type ParameterValues } from './types'

defineProps<{
  definitions: ParameterDefinition[]
  values: ParameterValues
  missing: number[]
  running?: boolean
}>()

const emit = defineEmits<{ change: [id: number, value: string]; run: [] }>()

const { t } = useI18n()
</script>

<template>
  <form
    class="flex flex-wrap items-end gap-4 rounded-box border border-base-300 bg-base-100 p-4"
    data-testid="visualisation-parameters"
    novalidate
    @submit.prevent="emit('run')"
  >
    <template v-for="definition in definitions" :key="definition.id">
      <label
        v-if="definition.dataTypeId === parameterType.flag"
        class="flex items-center gap-2 text-sm"
      >
        <input
          type="checkbox"
          class="toggle toggle-sm toggle-primary"
          :checked="values[definition.id] === '1'"
          :data-testid="`parameter-${definition.id}`"
          @change="
            emit('change', definition.id, ($event.target as HTMLInputElement).checked ? '1' : '0')
          "
        />
        <span>{{ definition.name }}</span>
      </label>
      <label v-else class="flex flex-col gap-1 text-sm">
        <span class="label">
          {{ definition.name }}
          <span
            v-if="definition.required"
            key="required"
            class="badge badge-sm badge-soft font-mono"
            >{{ t('visualisation.required') }}</span
          >
        </span>
        <input
          class="input input-sm w-64 max-w-full"
          :class="definition.dataTypeId !== parameterType.text ? 'font-mono' : ''"
          :type="
            definition.dataTypeId === parameterType.dateTime
              ? 'datetime-local'
              : definition.dataTypeId === parameterType.text
                ? 'text'
                : 'number'
          "
          :step="definition.dataTypeId === parameterType.integer ? 1 : 'any'"
          :value="values[definition.id]"
          :aria-invalid="missing.includes(definition.id) ? 'true' : 'false'"
          :aria-required="definition.required ? 'true' : 'false'"
          :data-testid="`parameter-${definition.id}`"
          @input="emit('change', definition.id, ($event.target as HTMLInputElement).value)"
        />
        <span
          v-if="missing.includes(definition.id)"
          key="missing"
          class="text-sm text-error"
          :data-testid="`parameter-${definition.id}-missing`"
          >{{ t('visualisation.requiredMessage') }}</span
        >
      </label>
    </template>
    <button
      type="submit"
      class="btn btn-sm btn-primary"
      :disabled="running"
      data-testid="visualisation-run"
    >
      <Play aria-hidden="true" class="size-4" />
      {{ t('visualisation.run') }}
    </button>
  </form>
</template>
