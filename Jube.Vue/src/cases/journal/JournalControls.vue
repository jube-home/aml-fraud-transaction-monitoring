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
import GridExportButtons from '../../components/GridExportButtons.vue'
import FieldHelp from '../../components/FieldHelp.vue'
import { useFormat } from '../../composables/useFormat'
import type { JournalState } from './state'
import { journalLenses, type JournalLens, type JournalLensCount } from './types'

const props = defineProps<{
  state: JournalState
  lenses: JournalLensCount[]
  pinCount: number
  empty: boolean
  formatted?: boolean
}>()

const emit = defineEmits<{
  change: [state: JournalState]
  csv: []
  excel: []
}>()

const { t } = useI18n()
const { number } = useFormat()

const counts = computed(() => new Map(props.lenses.map((item) => [item.lens, item.rows])))

function lensName(lens: JournalLens): string {
  if (lens === 'rules') return t('journal.lens.rules')
  if (lens === 'features') return t('journal.lens.features')
  if (lens === 'models') return t('journal.lens.models')
  if (lens === 'sanctions') return t('journal.lens.sanctions')

  return t('journal.lens.payload')
}

function update(patch: Partial<JournalState>): void {
  emit('change', { ...props.state, ...patch })
}

function pickPinned(event: Event): void {
  update({ pinnedOnly: (event.target as HTMLInputElement).checked })
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="journal-controls">
    <div class="flex flex-wrap items-center gap-x-4 gap-y-2">
      <div role="tablist" class="tabs tabs-box tabs-sm" :aria-label="t('journal.lens.label')">
        <button
          v-for="lens in journalLenses"
          :key="lens"
          type="button"
          role="tab"
          class="tab gap-2"
          :class="{ 'tab-active': state.lens === lens }"
          :aria-selected="state.lens === lens"
          :data-testid="`journal-lens-${lens}`"
          @click="update({ lens })"
        >
          {{ lensName(lens) }}
          <span class="badge badge-soft badge-sm font-mono tabular-nums">
            {{ number(counts.get(lens) ?? 0) }}
          </span>
        </button>
      </div>
      <FieldHelp :label="t('journal.lens.label')" :text="t('journal.help.lens')" />

      <div class="flex items-center gap-1">
        <label class="flex items-center gap-2">
          <input
            type="checkbox"
            class="toggle toggle-sm"
            :checked="state.pinnedOnly"
            data-testid="journal-pinned-only"
            @change="pickPinned"
          />
          <span class="text-sm">{{ t('journal.pin.only', { n: number(pinCount) }) }}</span>
        </label>
        <FieldHelp
          :label="t('journal.pin.only', { n: number(pinCount) })"
          :text="t('journal.help.pinnedOnly')"
        />
      </div>

      <FieldHelp
        v-if="formatted"
        key="format"
        :label="t('journal.format.column')"
        :text="t('journal.help.format')"
      />

      <slot />

      <slot name="tags" />

      <GridExportButtons v-if="!empty" key="export" @csv="emit('csv')" @excel="emit('excel')" />
    </div>
  </div>
</template>
