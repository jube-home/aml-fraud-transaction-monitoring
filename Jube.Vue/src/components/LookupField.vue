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
import { lookupChoices, lookupSelected, lookupUrl } from '../formModel/lookups'
import type { FormLookup } from '../formModel/types'

const props = defineProps<{
  id: string
  lookup: FormLookup
  modelValue: unknown
  parentValue?: unknown
  disabled?: boolean
  invalid?: boolean
  changed?: boolean
  label: string
}>()

const emit = defineEmits<{ 'update:modelValue': [value: unknown] }>()

const { t } = useI18n()

const url = computed(() => lookupUrl(props.lookup, props.parentValue))

const options = useQuery({
  queryKey: computed(() => queryKeys.formLookup(url.value ?? '')),
  queryFn: () => useApi<unknown>(url.value!),
  enabled: computed(() => url.value !== undefined),
  retry: false,
})

const choices = computed(() => lookupChoices(options.data.value, props.lookup))

const waiting = computed(() => url.value === undefined)
const current = computed(() => lookupSelected(choices.value, props.modelValue))

function change(event: Event): void {
  const text = (event.target as HTMLSelectElement).value
  const match = choices.value.find((choice) => String(choice.value) === text)

  emit('update:modelValue', match ? match.value : null)
}
</script>

<template>
  <select
    :id="id"
    class="select select-sm w-full"
    :class="invalid ? 'select-error' : changed ? 'select-info' : ''"
    :disabled="disabled || waiting || options.isError.value"
    :aria-label="label"
    :value="current"
    :data-testid="`field-${id}`"
    @change="change"
  >
    <option value="" :disabled="!lookup.allowEmpty">
      {{ options.isFetching.value ? t('crud.loadingChoices') : t('crud.choose') }}
    </option>
    <option
      v-for="choice in choices"
      :key="String(choice.value)"
      :value="String(choice.value)"
      :selected="String(choice.value) === current"
    >
      {{ choice.text }}
    </option>
  </select>
</template>
