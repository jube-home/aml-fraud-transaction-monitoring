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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../../api/queryKeys'
import { dashboardApi } from '../../dashboard/api'
import VisualisationDashboard from '../../dashboard/VisualisationDashboard.vue'

const { t } = useI18n()

const guid = ref('')
const hideParameters = ref(false)
const generation = ref(0)

const directory = useQuery({
  queryKey: queryKeys.visualisationDirectory(),
  queryFn: () => dashboardApi.directory(),
})

function pick(): void {
  generation.value += 1
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex shrink-0 flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('visualisation.bench.directory') }}</span>
        <select
          v-model="guid"
          class="select select-sm"
          data-testid="bench-directory"
          @change="pick"
        >
          <option value="">{{ t('visualisation.bench.pick') }}</option>
          <option v-for="entry in directory.data.value ?? []" :key="entry.guid" :value="entry.guid">
            {{ entry.name }}
          </option>
        </select>
      </label>
      <label class="flex items-center gap-2 text-sm">
        <input
          v-model="hideParameters"
          type="checkbox"
          class="toggle toggle-sm toggle-primary"
          data-testid="bench-hide-parameters"
          @change="pick"
        />
        <span>{{ t('visualisation.bench.hideParameters') }}</span>
      </label>
    </div>

    <VisualisationDashboard
      v-if="guid"
      :key="`${guid}-${hideParameters}-${generation}`"
      :guid="guid"
      :show-parameters="!hideParameters"
    />
    <p v-else key="pick" class="text-sm text-secondary" data-testid="bench-pick">
      {{ t('visualisation.bench.pick') }}
    </p>
  </div>
</template>
