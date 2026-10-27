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
import { analyticsApi } from '../askJoobyAnalytics/api'
import ServerChart from './ServerChart.vue'

const props = defineProps<{ versionId: number }>()

const { t } = useI18n()

const narrative = useQuery({
  queryKey: computed(() => queryKeys.analyticsNarrative(props.versionId)),
  queryFn: () => analyticsApi.narrative(props.versionId),
  retry: false,
})

const paragraphs = computed(() => narrative.data.value?.groups?.ModelDiagram ?? [])
</script>

<template>
  <div class="flex min-w-0 flex-col gap-3" data-testid="analytics-diagram">
    <div
      v-if="paragraphs.length > 0"
      key="narrative"
      class="flex max-w-prose flex-col gap-2 text-sm"
    >
      <p v-for="paragraph in paragraphs" :key="paragraph">{{ paragraph }}</p>
    </div>
    <ServerChart
      :url="analyticsApi.chartPath('ModelDiagram', versionId)"
      :label="t('analytics.diagram.label')"
    />
  </div>
</template>
