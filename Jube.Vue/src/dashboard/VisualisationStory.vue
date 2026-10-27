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
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { ArrowLeft } from 'lucide-vue-next'
import { useQuery } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import StorySpine from '../components/StorySpine.vue'
import type { Beat } from '../story/model'
import VisualisationDashboard from './VisualisationDashboard.vue'
import VisualisationDirectory from './VisualisationDirectory.vue'
import { dashboardApi } from './api'
import { listedEntries, parametersFromQuery, sameValues } from './url'

const props = withDefaults(defineProps<{ guid?: string }>(), { guid: '' })

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const directory = useQuery({
  queryKey: queryKeys.visualisationDirectory(),
  queryFn: () => dashboardApi.directory(),
  retry: false,
})

const registry = useQuery({
  queryKey: computed(() => queryKeys.visualisationRegistry(props.guid)),
  queryFn: () => dashboardApi.registry(props.guid),
  enabled: computed(() => props.guid !== ''),
  retry: false,
})

const registryId = computed(() => registry.data.value?.id ?? 0)

const definitions = useQuery({
  queryKey: computed(() => queryKeys.visualisationParameters(registryId.value)),
  queryFn: () => dashboardApi.parameters(registryId.value),
  enabled: computed(() => registryId.value > 0),
})

const tiles = useQuery({
  queryKey: computed(() => queryKeys.visualisationTiles(registryId.value)),
  queryFn: () => dashboardApi.tiles(registryId.value),
  enabled: computed(() => registryId.value > 0),
})

const parameters = computed(() => parametersFromQuery(route.query))

const beats = computed<Beat[]>(() => {
  const listed = directory.data.value ? listedEntries(directory.data.value).length : undefined

  const first: Beat = {
    id: 'directory',
    title: t('visualisation.directory.title'),
    facts:
      listed === undefined ? [] : [{ text: t('visualisation.directory.count', { count: listed }) }],
  }

  if (props.guid === '') return [first]

  const facts: Beat['facts'] = []

  if (tiles.data.value) {
    facts.push({ text: t('visualisation.story.tiles', { count: tiles.data.value.length }) })
  }

  if (definitions.data.value) {
    facts.push({
      text: t('visualisation.story.parameters', { count: definitions.data.value.length }),
    })
  }

  return [
    first,
    {
      id: `dashboard-${props.guid}`,
      title: t('visualisation.story.dashboard'),
      name: registry.data.value?.name ?? undefined,
      facts,
    },
  ]
})

function open(guid: string): void {
  void router.push({ name: 'visualisationView', params: { guid } })
}

function reopen(index: number): void {
  if (index === 0) void router.push({ name: 'visualisation' })
}

function remember(values: Record<string, string>): void {
  if (sameValues(values, parameters.value)) return

  void router.push({ name: 'visualisationView', params: { guid: props.guid }, query: values })
}
</script>

<template>
  <StorySpine :beats="beats" @reopen="reopen">
    <template #default="{ index }">
      <VisualisationDirectory v-if="index === 0" key="directory" @open="open" />
      <VisualisationDashboard
        v-else
        key="dashboard"
        :guid="guid"
        :parameters="parameters"
        :show-title="false"
        @run="remember"
      />
      <RouterLink
        v-if="index > 0 && registry.isError.value"
        key="back"
        :to="{ name: 'visualisation' }"
        class="btn btn-sm w-fit"
        data-testid="visualisation-back"
      >
        <ArrowLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
        {{ t('visualisation.directory.back') }}
      </RouterLink>
    </template>
  </StorySpine>
</template>
