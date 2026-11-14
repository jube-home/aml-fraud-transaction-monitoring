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
import {computed, useId} from 'vue'
import {useI18n} from 'vue-i18n'
import {
  effectiveValue,
  limitsOf,
  parameterLabel,
  parameterValue,
  parseTyped,
  withParameter,
  type ParameterMap,
} from '../formModel/aggregationParameters'
import type {FormParameter, FormParameterSet} from '../formModel/types'
import FieldHelp from './FieldHelp.vue'

const props = defineProps<{
  set: FormParameterSet
  modelValue: ParameterMap
  disabled?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: ParameterMap | null] }>()

const {t} = useI18n()
const baseId = useId()

const rows = computed(() =>
  props.set.parameters.map((parameter) => ({
    parameter,
    id: `${baseId}-${parameter.name}`,
    label: parameterLabel(parameter.name),
    typed: parameterValue(props.modelValue, parameter),
    on: effectiveValue(props.modelValue, parameter) === 1,
    limits: limitsOf(parameter),
  })),
)

function limitText(bound: string, value: number): string {
  if (bound === 'atLeast') return t('aggregationParameters.atLeast', {value})
  if (bound === 'moreThan') return t('aggregationParameters.moreThan', {value})
  if (bound === 'atMost') return t('aggregationParameters.atMost', {value})

  return t('aggregationParameters.lessThan', {value})
}

function typed(parameter: FormParameter, event: Event): void {
  const value = parseTyped((event.target as HTMLInputElement).value, parameter)

  if (value === undefined) return

  emit('update:modelValue', withParameter(props.modelValue, parameter, value))
}

function toggled(parameter: FormParameter, event: Event): void {
  const value = (event.target as HTMLInputElement).checked ? 1 : 0

  emit('update:modelValue', withParameter(props.modelValue, parameter, value))
}
</script>

<template>
  <div
    class="@container flex flex-col gap-3 rounded-box border border-base-300 bg-base-200 p-4"
    data-testid="aggregation-parameters"
  >
    <div class="flex flex-wrap items-center gap-2">
      <span class="text-xs font-medium text-secondary">{{ t('aggregationParameters.title') }}</span>
      <span
        v-if="set.orderSensitive"
        class="badge badge-ghost badge-sm font-mono"
        data-testid="aggregation-order-sensitive"
      >{{ t('aggregationParameters.orderSensitive') }}</span
      >
    </div>

    <p class="text-sm text-base-content" data-testid="aggregation-description">
      {{ set.description }}
    </p>

    <p
      v-if="rows.length === 0"
      class="text-xs text-secondary"
      data-testid="aggregation-no-parameters"
    >
      {{ t('aggregationParameters.none') }}
    </p>

    <div v-else class="grid gap-x-6 gap-y-4 @lg:grid-cols-2">
      <div
        v-for="row in rows"
        :key="row.parameter.name"
        class="flex flex-col gap-1"
        :data-testid="`aggregation-parameter-${row.parameter.name}`"
      >
        <div v-if="row.parameter.kind === 'boolean'" class="flex items-center gap-1">
          <label class="flex min-h-8 cursor-pointer items-center gap-3 text-base-content">
            <input
              :id="row.id"
              type="checkbox"
              class="toggle toggle-sm toggle-primary shrink-0"
              :checked="row.on"
              :disabled="disabled"
              :data-testid="`aggregation-input-${row.parameter.name}`"
              @change="toggled(row.parameter, $event)"
            />
            <span class="text-sm font-medium">{{ row.label }}</span>
          </label>
          <FieldHelp :label="row.label" :text="row.parameter.description"/>
        </div>

        <template v-else>
          <div class="flex items-center gap-1">
            <label :for="row.id" class="text-xs font-medium text-secondary">{{ row.label }}</label>
            <FieldHelp :label="row.label" :text="row.parameter.description"/>
          </div>
          <input
            :id="row.id"
            type="number"
            class="input input-sm w-full font-mono"
            :step="row.parameter.kind === 'integer' ? 1 : 'any'"
            :min="row.parameter.minimum ?? undefined"
            :max="row.parameter.maximum ?? undefined"
            :value="row.typed ?? ''"
            :placeholder="String(row.parameter.default)"
            :disabled="disabled"
            :data-testid="`aggregation-input-${row.parameter.name}`"
            @input="typed(row.parameter, $event)"
          />
        </template>

        <div class="flex flex-wrap gap-1">
          <span class="badge badge-ghost badge-sm font-mono">{{
              row.parameter.kind === 'boolean'
                ? row.parameter.default === 1
                  ? t('aggregationParameters.defaultOn')
                  : t('aggregationParameters.defaultOff')
                : t('aggregationParameters.default', {value: row.parameter.default})
            }}</span>
          <span
            v-for="limit in row.limits"
            :key="limit.bound"
            class="badge badge-ghost badge-sm font-mono"
          >{{ limitText(limit.bound, limit.value) }}</span
          >
        </div>
      </div>
    </div>
  </div>
</template>
