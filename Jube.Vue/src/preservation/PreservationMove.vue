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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import PanelCard from '../components/PanelCard.vue'
import PreservationExport from './PreservationExport.vue'
import PreservationImport from './PreservationImport.vue'
import { preservationApi } from './api'
import { defaultMinimumPasswordLength, defaultOptions, passwordAcceptable } from './state'
import type { MoveOptions } from './types'

const { t } = useI18n()

const options = ref<MoveOptions>(defaultOptions())

const summaryQuery = useQuery({
  queryKey: queryKeys.preservationSummary(),
  queryFn: () => preservationApi.summary(),
  staleTime: 60_000,
})

const acceptable = computed(() =>
  passwordAcceptable(options.value.password, summaryQuery.data.value),
)
const minimum = computed(
  () => summaryQuery.data.value?.minimumPasswordLength ?? defaultMinimumPasswordLength,
)
</script>

<template>
  <div class="flex min-w-0 flex-col gap-4" data-testid="preservation-move">
    <PanelCard
      :level="2"
      :title="t('preservation.password.title')"
      :hint="t('preservation.password.hint')"
    >
      <div class="flex flex-col gap-1">
        <label class="label text-xs" for="preservation-password">{{
          t('preservation.password.label')
        }}</label>
        <input
          id="preservation-password"
          v-model="options.password"
          type="password"
          autocomplete="off"
          class="input input-sm w-full max-w-sm"
          data-testid="preservation-password"
        />
      </div>
      <p
        v-if="!acceptable"
        key="short"
        role="status"
        class="alert alert-warning text-sm"
        data-testid="password-short"
      >
        {{ t('preservation.password.short', { count: minimum }) }}
      </p>
    </PanelCard>

    <PanelCard :level="2" :title="t('preservation.export.title')" :step="1">
      <PreservationExport v-model="options" :acceptable="acceptable" />
    </PanelCard>

    <PanelCard :level="2" :title="t('preservation.import.title')" :step="2">
      <PreservationImport :password="options.password" :acceptable="acceptable" />
    </PanelCard>
  </div>
</template>
