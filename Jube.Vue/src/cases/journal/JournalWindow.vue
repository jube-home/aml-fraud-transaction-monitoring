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
import FieldHelp from '../../components/FieldHelp.vue'
import { windowDays, type JournalState } from './state'

const props = defineProps<{ state: JournalState }>()

const emit = defineEmits<{ change: [state: JournalState] }>()

const { t } = useI18n()

function update(patch: Partial<JournalState>): void {
  emit('change', { ...props.state, transaction: null, ...patch })
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-x-4 gap-y-2" data-testid="journal-window-bar">
    <div class="flex items-center gap-1">
      <label class="flex items-center gap-2">
        <span class="text-xs font-semibold">{{ t('journal.window.label') }}</span>
        <select
          class="select select-sm"
          :value="state.days"
          data-testid="journal-window"
          @change="update({ days: Number(($event.target as HTMLSelectElement).value) })"
        >
          <option v-for="days in windowDays" :key="days" :value="days">
            {{ t('journal.window.days', { n: days }) }}
          </option>
        </select>
      </label>
      <FieldHelp :label="t('journal.window.label')" :text="t('journal.help.window')" />
    </div>

    <div class="flex items-center gap-1">
      <label class="flex items-center gap-2">
        <input
          type="checkbox"
          class="toggle toggle-sm"
          :checked="state.activationsOnly"
          data-testid="journal-activations-only"
          @change="update({ activationsOnly: ($event.target as HTMLInputElement).checked })"
        />
        <span class="text-sm">{{ t('journal.activationsOnly') }}</span>
      </label>
      <FieldHelp :label="t('journal.activationsOnly')" :text="t('journal.help.activationsOnly')" />
    </div>
  </div>
</template>
