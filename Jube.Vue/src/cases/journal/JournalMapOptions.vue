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
import { Gauge, Maximize2 } from 'lucide-vue-next'
import FieldHelp from '../../components/FieldHelp.vue'
import { useFormat } from '../../composables/useFormat'
import {
  defaultLimitKmh,
  defaultMinimumKilometres,
  maximumKilometres,
  maximumLimitKmh,
  type JournalState,
} from './state'

const props = defineProps<{ state: JournalState }>()

const emit = defineEmits<{ change: [state: JournalState]; fit: [] }>()

const { t } = useI18n()
const { number } = useFormat()

const limited = computed(
  () => props.state.speed !== defaultLimitKmh || props.state.distance !== defaultMinimumKilometres,
)

function update(patch: Partial<JournalState>): void {
  emit('change', { ...props.state, ...patch })
}

function pickLimit(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  update({
    speed: Number.isFinite(value) ? Math.min(Math.max(value, 1), maximumLimitKmh) : defaultLimitKmh,
  })
}

function pickDistance(event: Event): void {
  const value = Number((event.target as HTMLInputElement).value)

  update({
    distance: Number.isFinite(value)
      ? Math.min(Math.max(value, 0), maximumKilometres)
      : defaultMinimumKilometres,
  })
}

function resetLimits(): void {
  update({ speed: defaultLimitKmh, distance: defaultMinimumKilometres })
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-3" data-testid="journal-map-options">
    <div class="flex items-center gap-1">
      <button
        type="button"
        class="btn btn-sm"
        popovertarget="journal-map-limits"
        style="anchor-name: --journal-map-limits"
        data-testid="journal-map-limits-open"
      >
        <Gauge class="size-4" aria-hidden="true" />
        {{ t('journal.map.limits.title') }}
        <span v-if="limited" key="limited" class="badge badge-soft badge-sm font-mono tabular-nums">
          {{
            t('journal.map.limits.badge', {
              speed: number(state.speed),
              distance: number(state.distance),
            })
          }}
        </span>
      </button>
      <FieldHelp :label="t('journal.map.limits.title')" :text="t('journal.map.help.limits')" />
      <div
        id="journal-map-limits"
        class="dropdown card card-sm z-1 w-72 bg-base-100 shadow-md"
        popover
        style="position-anchor: --journal-map-limits"
        data-testid="journal-map-limits"
      >
        <div class="card-body gap-2">
          <label class="flex flex-col gap-1">
            <span class="text-xs">{{ t('journal.map.limits.speed') }}</span>
            <input
              type="number"
              min="1"
              :max="maximumLimitKmh"
              step="50"
              class="input input-sm w-full font-mono tabular-nums"
              :value="state.speed"
              data-testid="journal-map-limit"
              @change="pickLimit"
            />
          </label>
          <label class="flex flex-col gap-1">
            <span class="text-xs">{{ t('journal.map.limits.distance') }}</span>
            <input
              type="number"
              min="0"
              :max="maximumKilometres"
              step="10"
              class="input input-sm w-full font-mono tabular-nums"
              :value="state.distance"
              data-testid="journal-map-distance"
              @change="pickDistance"
            />
          </label>
          <button
            v-if="limited"
            key="reset"
            type="button"
            class="btn btn-ghost btn-sm"
            data-testid="journal-map-limits-reset"
            @click="resetLimits"
          >
            {{ t('journal.map.limits.reset') }}
          </button>
        </div>
      </div>
    </div>

    <label class="flex items-center gap-2">
      <input
        type="checkbox"
        class="toggle toggle-sm"
        :checked="state.trails"
        data-testid="journal-map-trails"
        @change="update({ trails: ($event.target as HTMLInputElement).checked })"
      />
      <span class="text-sm">{{ t('watcher.trails') }}</span>
    </label>

    <label class="flex items-center gap-2">
      <input
        type="checkbox"
        class="toggle toggle-sm"
        :checked="state.heat"
        data-testid="journal-map-heat"
        @change="update({ heat: ($event.target as HTMLInputElement).checked })"
      />
      <span class="text-sm">{{ t('watcher.heat') }}</span>
    </label>

    <div class="flex items-center gap-1">
      <label class="flex items-center gap-2">
        <input
          type="checkbox"
          class="toggle toggle-sm"
          :checked="state.improbable"
          data-testid="journal-map-only"
          @change="update({ improbable: ($event.target as HTMLInputElement).checked })"
        />
        <span class="text-sm">{{ t('journal.map.only.label') }}</span>
      </label>
      <FieldHelp :label="t('journal.map.only.label')" :text="t('journal.map.help.only')" />
    </div>

    <div v-if="state.filter" key="all" class="flex items-center gap-1">
      <label class="flex items-center gap-2">
        <input
          type="checkbox"
          class="toggle toggle-sm"
          :checked="state.all"
          data-testid="journal-map-all"
          @change="update({ all: ($event.target as HTMLInputElement).checked })"
        />
        <span class="text-sm">{{ t('journal.map.all.label') }}</span>
      </label>
      <FieldHelp :label="t('journal.map.all.label')" :text="t('journal.map.help.all')" />
    </div>

    <button
      type="button"
      class="btn btn-ghost btn-sm"
      data-testid="journal-map-fit"
      @click="emit('fit')"
    >
      <Maximize2 class="size-4" aria-hidden="true" />
      {{ t('watcher.fit') }}
    </button>
  </div>
</template>
