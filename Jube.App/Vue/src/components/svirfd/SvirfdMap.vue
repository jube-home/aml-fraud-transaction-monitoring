<!--  Copyright (C) 2022-present Jube Holdings Limited.

  This file is part of Jube™ software.

  Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
  as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
  Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
  of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

  You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
  see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {legendLabels, legendOf, recordPoints, recordRings} from '../../svirfd/points'
import type {IpRecord} from '../../svirfd/types'
import type {WatcherSource} from '../../watcher/source'
import ActivationWatcher from '../ActivationWatcher.vue'

const props = defineProps<{ record: IpRecord; stored?: IpRecord | null }>()

const {t} = useI18n()

const rows = computed(() => recordPoints(props.record, props.stored ?? null))
const rings = computed(() => recordRings(props.record))
const legend = computed(() => legendOf(rows.value))
const generation = ref(0)

const source = computed<WatcherSource>(() => ({
  replay: async () => rows.value,
}))

watch(
  () => [props.record.ip, props.stored?.ip],
  () => {
    generation.value += 1
  },
)
</script>

<template>
  <div v-if="rows.length > 0" class="flex flex-col gap-2" data-testid="svirfd-map">
    <ActivationWatcher
      :key="generation"
      :source="source"
      mode="all"
      :realtime="false"
      :fade-minutes="0"
      :show-controls="false"
      :show-chart="false"
      :show-feed="false"
      :show-legend="false"
      :heat="false"
      :trails="true"
      :travel-limit-kmh="Number.POSITIVE_INFINITY"
      :minimum-kilometres="0"
      :rings="rings"
      auto-fit
      :zoom-boost="1"
      auto-replay
      hide-elevation
      compact
    />
    <ul class="flex flex-wrap gap-x-4 gap-y-1 text-xs" data-testid="svirfd-legend">
      <li v-for="entry in legend" :key="entry.kind" class="flex items-center gap-1">
        <span
          class="inline-block size-2.5 rounded-full"
          :style="{ backgroundColor: entry.colour }"
          aria-hidden="true"
        ></span>
        {{ t(legendLabels[entry.kind] ?? 'svirfd.legend.location') }}
      </li>
    </ul>
  </div>
</template>
