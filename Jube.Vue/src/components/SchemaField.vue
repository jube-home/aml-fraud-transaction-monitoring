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
import { computed, inject, ref } from 'vue'
import { CircleAlert } from 'lucide-vue-next'
import { useI18n } from 'vue-i18n'
import { changedFieldsKey } from '../formModel/changedKey'
import { fieldState } from '../formModel/conditions'
import type { FormField, RecordValues } from '../formModel/types'
import { fromInput, toInput } from '../formModel/values'
import { intlTag, useFormat } from '../composables/useFormat'
import FieldHelp from './FieldHelp.vue'
import LookupField from './LookupField.vue'

const props = defineProps<{
  field: FormField
  values: RecordValues
  errors?: string[]
  locked?: boolean
  lockField?: boolean
  disabled?: boolean
}>()

const emit = defineEmits<{ change: [name: string, value: unknown] }>()

const { t, locale } = useI18n()
const { dateTime } = useFormat()

const state = computed(() => {
  const conditional = fieldState(props.field, props.values)

  return { ...conditional, required: props.field.required || conditional.required }
})
const value = computed(() => props.values[props.field.name])
const inputId = computed(() => props.field.name)
const invalid = computed(() => (props.errors?.length ?? 0) > 0)
const changedNames = inject(changedFieldsKey, ref<string[]>([]))
const isChanged = computed(() => changedNames.value.includes(props.field.name))
const readOnly = computed(() => props.field.readOnly || props.lockField === true)
const inactive = computed(
  () => props.disabled === true || props.locked === true || !state.value.enabled,
)
const ranged = computed(
  () =>
    ['slider', 'percent'].includes(props.field.widget) &&
    props.field.min !== null &&
    props.field.max !== null,
)
const sliderValue = computed(() => {
  const current = Number(value.value)

  return Number.isFinite(current) && value.value !== null && value.value !== ''
    ? current
    : (props.field.min ?? 0)
})
const sliderStep = computed(() => {
  if (props.field.type === 'integer') return 1

  return (props.field.max! - props.field.min!) / 100
})
const readout = computed(() =>
  props.field.widget === 'percent'
    ? new Intl.NumberFormat(intlTag(locale.value), {
        style: 'percent',
        maximumFractionDigits: 1,
      }).format(sliderValue.value)
    : new Intl.NumberFormat(intlTag(locale.value)).format(sliderValue.value),
)
const pattern = computed(() =>
  props.field.patterns.length === 1 ? props.field.patterns[0] : undefined,
)
const chosen = computed(() => {
  const current = value.value

  return current === null || current === undefined ? '' : String(current)
})
const hexHint = '#RRGGBB'
const swatch = computed(() => {
  const text = String(value.value ?? '')

  return /^#[0-9a-fA-F]{6}$/.test(text) ? text : '#000000'
})
const asRadio = computed(() => props.field.widget === 'radio' && props.field.options.length > 0)
const asSelect = computed(() => props.field.options.length > 0 && !asRadio.value)
const multiline = computed(
  () => ['textarea', 'code'].includes(props.field.widget) || props.field.editor !== null,
)

const shown = computed(() => {
  const current = value.value

  if (current === null || current === undefined || current === '') return '–'
  if (props.field.type === 'datetime') return dateTime(String(current))

  return String(current)
})

function picked(text: string): void {
  const match = props.field.options.find((option) => String(option.value) === text)

  emit('change', props.field.name, match ? match.value : null)
}

function typed(event: Event): void {
  emit('change', props.field.name, fromInput(props.field, (event.target as HTMLInputElement).value))
}

function toggled(event: Event): void {
  emit('change', props.field.name, (event.target as HTMLInputElement).checked)
}
</script>

<template>
  <div v-if="state.visible" class="flex flex-col gap-1" :data-testid="`schema-field-${field.name}`">
    <div v-if="field.type === 'boolean'" class="flex items-center gap-1">
      <label class="flex min-h-8 cursor-pointer items-center gap-3 text-base-content">
        <input
          :id="inputId"
          type="checkbox"
          class="toggle toggle-sm toggle-primary shrink-0"
          :checked="value === true"
          :disabled="readOnly || inactive"
          :data-testid="`field-${field.name}`"
          @change="toggled"
        />
        <span class="flex min-w-0 flex-col">
          <span class="text-sm font-medium">{{ field.label }}</span>
          <span v-if="isChanged" class="badge badge-info badge-soft badge-xs">{{
            t('crud.changed')
          }}</span>
          <span v-if="field.help" class="text-xs text-secondary">{{ field.help }}</span>
        </span>
      </label>
      <FieldHelp :label="field.label" :text="field.description" />
    </div>

    <template v-else>
      <div class="flex items-center gap-1">
        <label
          class="label flex items-center gap-2 text-xs font-medium text-secondary"
          :for="inputId"
        >
          <span>{{ field.label }}</span>
          <span v-if="state.required" class="badge badge-primary badge-soft badge-xs font-mono">
            {{ t('crud.required') }}
          </span>
          <span v-if="isChanged" class="badge badge-info badge-soft badge-xs">{{
            t('crud.changed')
          }}</span>
        </label>
        <FieldHelp :label="field.label" :text="field.description" />
      </div>

      <output
        v-if="readOnly"
        :id="inputId"
        class="min-h-8 rounded-field bg-base-200 px-3 py-1.5 text-sm font-medium text-base-content"
        :class="field.type === 'string' && field.widget !== 'date' ? '' : 'font-mono tabular-nums'"
        :data-testid="`field-${field.name}`"
        >{{ shown }}</output
      >

      <fieldset
        v-else-if="asRadio"
        class="flex flex-wrap gap-x-5 gap-y-2"
        :aria-label="field.label"
        :data-testid="`field-${field.name}`"
      >
        <label
          v-for="option in field.options"
          :key="String(option.value)"
          class="label cursor-pointer gap-2 text-base-content"
        >
          <input
            type="radio"
            class="radio radio-sm radio-primary"
            :name="field.name"
            :value="String(option.value)"
            :checked="chosen === String(option.value)"
            :disabled="inactive"
            :data-testid="`option-${field.name}-${option.value}`"
            @change="picked(String(option.value))"
          />
          <span>{{ option.label }}</span>
        </label>
      </fieldset>

      <select
        v-else-if="asSelect"
        :id="inputId"
        class="select select-sm w-full"
        :class="invalid ? 'select-error' : isChanged ? 'select-info' : ''"
        :value="chosen"
        :aria-required="state.required || undefined"
        :disabled="inactive"
        :data-testid="`field-${field.name}`"
        @change="picked(($event.target as HTMLSelectElement).value)"
      >
        <option v-if="field.nullable || chosen === ''" value="">{{ t('crud.choose') }}</option>
        <option
          v-for="option in field.options"
          :key="String(option.value)"
          :value="String(option.value)"
        >
          {{ option.label }}
        </option>
      </select>

      <LookupField
        v-else-if="field.lookup"
        :id="inputId"
        :lookup="field.lookup"
        :model-value="value"
        :parent-value="field.lookup.parentField ? values[field.lookup.parentField] : undefined"
        :label="field.label"
        :invalid="invalid"
        :changed="isChanged"
        :disabled="inactive"
        @update:model-value="emit('change', field.name, $event)"
      />

      <textarea
        v-else-if="multiline"
        :id="inputId"
        rows="5"
        class="textarea textarea-sm w-full font-mono"
        :class="invalid ? 'textarea-error' : isChanged ? 'textarea-info' : ''"
        :minlength="field.minLength ?? undefined"
        :maxlength="field.maxLength ?? undefined"
        :aria-required="state.required || undefined"
        :value="toInput(field, value)"
        :placeholder="field.placeholder ?? undefined"
        :disabled="inactive"
        :data-testid="`field-${field.name}`"
        @input="typed"
      ></textarea>

      <div v-else-if="field.widget === 'colour'" class="flex items-center gap-2">
        <input
          type="color"
          class="input input-sm w-12 shrink-0 cursor-pointer p-1"
          :value="swatch"
          :disabled="inactive"
          :aria-label="t('crud.pick', { field: field.label })"
          :data-testid="`swatch-${field.name}`"
          @input="typed"
        />
        <input
          :id="inputId"
          type="text"
          class="input input-sm w-full font-mono"
          :class="invalid ? 'input-error' : isChanged ? 'input-info' : ''"
          :placeholder="field.placeholder ?? hexHint"
          maxlength="9"
          autocomplete="off"
          :value="toInput(field, value)"
          :disabled="inactive"
          :data-testid="`field-${field.name}`"
          @input="typed"
        />
      </div>

      <input
        v-else-if="field.type === 'datetime'"
        :id="inputId"
        type="datetime-local"
        class="input input-sm w-full font-mono"
        :class="invalid ? 'input-error' : isChanged ? 'input-info' : ''"
        :value="toInput(field, value)"
        :disabled="inactive"
        :data-testid="`field-${field.name}`"
        @input="typed"
      />

      <div v-else-if="ranged" class="flex items-center gap-3">
        <input
          :id="inputId"
          type="range"
          class="range range-sm range-primary flex-1"
          :min="field.min ?? undefined"
          :max="field.max ?? undefined"
          :step="sliderStep"
          :value="sliderValue"
          :disabled="inactive"
          :data-testid="`field-${field.name}`"
          @input="typed"
        />
        <output
          class="w-16 text-end font-mono text-sm tabular-nums"
          :for="inputId"
          :data-testid="`readout-${field.name}`"
          >{{ readout }}</output
        >
      </div>

      <input
        v-else-if="field.type === 'integer' || field.type === 'number'"
        :id="inputId"
        type="number"
        class="input input-sm w-full font-mono"
        :class="invalid ? 'input-error' : isChanged ? 'input-info' : ''"
        :step="field.type === 'integer' ? 1 : 'any'"
        :min="field.min ?? undefined"
        :max="field.max ?? undefined"
        :aria-required="state.required || undefined"
        :value="toInput(field, value)"
        :placeholder="field.placeholder ?? undefined"
        :disabled="inactive"
        :data-testid="`field-${field.name}`"
        @input="typed"
      />

      <input
        v-else
        :id="inputId"
        type="text"
        class="input input-sm w-full"
        :class="invalid ? 'input-error' : isChanged ? 'input-info' : ''"
        :minlength="field.minLength ?? undefined"
        :maxlength="field.maxLength ?? undefined"
        :pattern="pattern"
        :aria-required="state.required || undefined"
        :value="toInput(field, value)"
        :placeholder="field.placeholder ?? undefined"
        autocomplete="off"
        :disabled="inactive"
        :data-testid="`field-${field.name}`"
        @input="typed"
      />
    </template>

    <p v-if="field.editor && !readOnly" class="text-sm" data-testid="editor-pending">
      {{ t('crud.editorPending', { editor: field.editor }) }}
    </p>
    <p v-if="field.help && field.type !== 'boolean'" class="text-xs text-secondary">
      {{ field.help }}
    </p>
    <p
      v-for="message in errors ?? []"
      :key="message"
      class="flex items-center gap-1.5 text-sm text-error"
      role="alert"
      :data-testid="`error-${field.name}`"
    >
      <CircleAlert aria-hidden="true" class="size-4 shrink-0" />
      {{ message }}
    </p>
  </div>
</template>
