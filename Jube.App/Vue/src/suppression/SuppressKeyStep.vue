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
import {useI18n} from 'vue-i18n'
import {isolate} from './state'
import type {CandidateKey} from './types'

defineProps<{ keys: CandidateKey[]; loading: boolean; failed: boolean; modelValue: string }>()

const emit = defineEmits<{ 'update:modelValue': [name: string] }>()

const {t} = useI18n()
</script>

<template>
  <div class="flex flex-col gap-3" data-testid="suppress-keys">
    <div v-if="loading" key="loading" class="skeleton h-24 w-full" role="status">
      <span class="sr-only">{{ t('cases.loading') }}</span>
    </div>
    <p v-else-if="failed" key="failed" role="alert" class="text-sm text-error">
      {{ t('suppression.key.failed') }}
    </p>
    <fieldset v-else key="keys" class="fieldset">
      <legend class="fieldset-legend">{{ t('suppression.key.legend') }}</legend>
      <label
        v-for="entry in keys"
        :key="entry.name"
        class="label items-start gap-3 rounded-box border border-base-300 p-3"
        :class="entry.enabled ? '' : 'opacity-70'"
      >
        <input
          type="radio"
          name="suppress-key"
          class="radio radio-sm mt-1"
          :value="entry.name"
          :checked="modelValue === entry.name"
          :disabled="!entry.enabled"
          :data-testid="`suppress-key-${entry.name}`"
          @change="emit('update:modelValue', entry.name)"
        />
        <span class="flex min-w-0 flex-col gap-1 text-sm text-base-content">
          <span class="font-medium">
            {{ isolate(entry.name) }}
            <span v-if="entry.isCaseKey" class="badge badge-ghost badge-sm">
              {{ t('suppression.key.caseKey') }}
            </span>
          </span>
          <span v-if="entry.enabled && entry.isCaseKey" key="caseHint" class="text-xs">
            {{ t('suppression.key.caseHint') }}
          </span>
          <span v-else-if="entry.enabled" key="otherHint" class="text-xs">
            {{ t('suppression.key.otherHint') }}
          </span>
          <span
            v-else
            key="disabled"
            class="text-xs"
            :data-testid="`suppress-key-disabled-${entry.name}`"
          >
            {{ t('suppression.key.disabled', {name: isolate(entry.name)}) }}
          </span>
        </span>
      </label>
      <p v-if="keys.length === 0" key="none" class="text-sm">{{ t('suppression.key.none') }}</p>
    </fieldset>
  </div>
</template>
