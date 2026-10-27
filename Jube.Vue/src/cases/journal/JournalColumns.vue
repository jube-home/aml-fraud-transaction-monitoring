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
import { Columns3 } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'

export interface ColumnChoice {
  field: string
  title: string
  on: boolean
}

defineProps<{ choices: ColumnChoice[]; customised: boolean }>()

const emit = defineEmits<{
  toggle: [field: string, on: boolean]
  reset: []
}>()

const { t } = useI18n()
</script>

<template>
  <div class="flex items-center gap-1">
    <button
      type="button"
      class="btn btn-sm"
      popovertarget="journal-columns"
      style="anchor-name: --journal-columns"
      data-testid="journal-columns-open"
    >
      <Columns3 class="size-4" aria-hidden="true" />
      {{ t('journal.columns.label') }}
    </button>
    <FieldHelp :label="t('journal.columns.label')" :text="t('journal.help.columns')" />
    <div
      id="journal-columns"
      class="dropdown dropdown-end card card-sm z-1 max-h-96 w-72 overflow-y-auto bg-base-100 shadow-md"
      popover
      style="position-anchor: --journal-columns"
      data-testid="journal-columns"
    >
      <div class="card-body gap-2">
        <fieldset class="fieldset gap-1">
          <legend class="fieldset-legend">{{ t('journal.columns.show') }}</legend>
          <label v-for="choice in choices" :key="choice.field" class="label gap-3 text-sm">
            <input
              type="checkbox"
              class="toggle toggle-sm"
              :checked="choice.on"
              :data-testid="`journal-column-${choice.field}`"
              @change="emit('toggle', choice.field, ($event.target as HTMLInputElement).checked)"
            />
            {{ choice.title }}
          </label>
        </fieldset>
        <button
          type="button"
          class="btn btn-sm"
          :disabled="!customised"
          data-testid="journal-columns-reset"
          @click="emit('reset')"
        >
          {{ t('journal.columns.reset') }}
        </button>
      </div>
    </div>
  </div>
</template>
