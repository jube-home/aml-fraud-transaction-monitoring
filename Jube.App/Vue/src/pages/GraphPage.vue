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
import {computed, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {RouterLink, useRoute, useRouter} from 'vue-router'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {ArrowLeft, PanelRightOpen, Search} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {ApiError} from '../api/useApi'
import {hasPermission, meQueryOptions} from '../auth/me'
import {
  graphAddressText,
  mergeGraphAddress,
  parseGraphAddress,
  parseGraphAddressText,
  type GraphAddress,
} from '../graph/address'
import {graphApi} from '../graph/api'
import GraphExplorer from '../graph/GraphExplorer.vue'
import GraphRail from '../graph/GraphRail.vue'
import {resolvePermission} from '../graph/state'
import type {GraphCommunity, GraphExploration, GraphNode, GraphPattern} from '../graph/types'
import {useGraphDiscoveryQueries} from '../graph/useGraphQueries'
import {useToasts} from '../stores/toasts'
import {useJoobyPageContext} from '../composables/useJoobyPageContext'

const {t} = useI18n()
const route = useRoute()
const router = useRouter()
const toasts = useToasts()
const queryClient = useQueryClient()
const me = useQuery(meQueryOptions)

const guid = computed(() => String(route.params.guid ?? ''))
const address = computed(() => parseGraphAddress(route.query))
const canResolve = computed(() => hasPermission(me.data.value, resolvePermission))
const railOpen = ref(false)

const registry = useQuery({
  queryKey: computed(() => queryKeys.graphRegistry(guid.value)),
  queryFn: () => graphApi.registry(guid.value),
  enabled: computed(() => guid.value !== ''),
  staleTime: 60_000,
  retry: false,
})

const registryId = computed(() => registry.data.value?.id ?? 0)

const edges = useQuery({
  queryKey: computed(() => queryKeys.graphEdges(registryId.value)),
  queryFn: () => graphApi.edges(registryId.value),
  enabled: computed(() => registryId.value > 0),
  staleTime: 60_000,
  retry: false,
})

const keyChoices = computed(() => {
  const seen: string[] = []

  for (const edge of edges.data.value ?? []) {
    for (const key of [edge.fromNodeKey || edge.fromKey, edge.toNodeKey || edge.toKey]) {
      if (key && !seen.includes(key)) seen.push(key)
    }
  }

  return seen.sort((a, b) => a.localeCompare(b))
})

const keyDraft = ref('')
const valueDraft = ref('')

watch(
  address,
  (value) => {
    keyDraft.value = value.seedKey
    valueDraft.value = value.seedValue
  },
  {immediate: true},
)

watch(keyChoices, (choices) => {
  if (keyDraft.value === '' && choices.length > 0) keyDraft.value = choices[0]!
})

function reseed(seedKey: string, seedValue: string, extra: Partial<GraphAddress> = {}): void {
  void router.replace({
    query: mergeGraphAddress(route.query, {
      ...address.value,
      registry: guid.value,
      seedKey,
      seedValue,
      selected: null,
      expanded: [],
      cursor: null,
      pathsTo: null,
      pattern: null,
      community: null,
      ...extra,
    }),
  })
}

function go(): void {
  const seedKey = keyDraft.value.trim()
  const seedValue = valueDraft.value.trim()

  if (seedKey === '' || seedValue === '') return

  reseed(seedKey, seedValue)
}

const explorations = useQuery({
  queryKey: queryKeys.graphExplorations(),
  queryFn: () => graphApi.explorations(),
  staleTime: 30_000,
  retry: false,
})

const mine = computed(() =>
  (explorations.data.value ?? [])
    .filter((exploration) => exploration.graphRegistryGuid === guid.value)
    .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
)

const save = useMutation({
  mutationFn: (name: string) =>
    graphApi.saveExploration({
      id: 0,
      name,
      graphRegistryGuid: guid.value,
      address: graphAddressText({...address.value, registry: guid.value}),
      pinned: null,
    }),
  onSuccess: async () => {
    await queryClient.invalidateQueries({queryKey: queryKeys.graphExplorations()})
    toasts.success(t('graph.rail.saved'))
  },
  onError: (error) => toasts.apiError(error),
})

const remove = useMutation({
  mutationFn: (id: number) => graphApi.deleteExploration(id),
  onSuccess: () => queryClient.invalidateQueries({queryKey: queryKeys.graphExplorations()}),
  onError: (error) => toasts.apiError(error),
})

function restore(exploration: GraphExploration): void {
  const restored = parseGraphAddressText(exploration.address ?? '')

  void router.replace({
    query: mergeGraphAddress(route.query, {...restored, registry: guid.value}),
  })
}

const patternState = ref<number | null>(null)

const discovery = useGraphDiscoveryQueries({
  enabled: computed(() => guid.value !== '' && !registry.isError.value),
  patternState,
})

const decide = useMutation({
  mutationFn: (change: { pattern: GraphPattern; stateId: number; caseId: number | null }) =>
    graphApi.patternDecide(change.pattern.id, {
      stateId: change.stateId,
      caseId: change.caseId ?? undefined,
    }),
  onSuccess: async () => {
    await queryClient.invalidateQueries({queryKey: ['graph', 'patterns']})
    await queryClient.invalidateQueries({queryKey: ['graph', 'pattern']})
    toasts.success(t('graph.rail.decided'))
  },
  onError: (error) => toasts.apiError(error),
})

const override = useMutation({
  mutationFn: (change: { node: GraphNode; overrideId: number }) =>
    graphApi.hubOverride({nodeId: change.node.id, overrideId: change.overrideId}),
  onSuccess: async () => {
    await queryClient.invalidateQueries({queryKey: queryKeys.graphHubs()})
    await queryClient.invalidateQueries({queryKey: ['graph', 'render']})
    toasts.success(t('graph.rail.overridden'))
  },
  onError: (error) => toasts.apiError(error),
})

function openPattern(pattern: GraphPattern): void {
  const anchor = pattern.anchor ?? pattern.members[0]?.node ?? null

  if (!anchor?.key || !anchor.value) {
    toasts.error(t('graph.rail.noAnchor'))

    return
  }

  reseed(anchor.key, anchor.value, {pattern: String(pattern.id), selected: String(anchor.id)})
}

async function openCommunity(community: GraphCommunity | number): Promise<void> {
  const id = typeof community === 'number' ? community : community.id

  try {
    const full =
      typeof community !== 'number' && community.members.length > 0
        ? community
        : await graphApi.community(id)
    const member = full.members[0]

    if (!member?.key || !member.value) {
      toasts.error(t('graph.rail.noMembers'))

      return
    }

    reseed(member.key, member.value, {community: String(id), selected: String(member.id)})
  } catch (error) {
    toasts.apiError(error)
  }
}

const seeded = computed(() => address.value.seedKey !== '' && address.value.seedValue !== '')

const registryError = computed(() => {
  const error = registry.error.value

  return error instanceof ApiError ? error.kind : error ? 'server' : null
})

useJoobyPageContext(() => ({pageName: 'Graph', state: 'readonly', record: {guid: guid.value}}))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="graph-page">
    <h1 class="sr-only">{{ t('nav.graph') }}</h1>

    <header class="flex flex-wrap items-center gap-3">
      <RouterLink :to="{ name: 'graph' }" class="btn btn-sm" data-testid="graph-back">
        <ArrowLeft class="size-4 rtl:rotate-180" aria-hidden="true"/>
        {{ t('graph.page.back') }}
      </RouterLink>
      <h2 class="text-lg font-semibold" data-testid="graph-registry-name">
        {{ registry.data.value?.name ?? t('graph.page.registry') }}
      </h2>

      <form class="ms-auto flex flex-wrap items-end gap-2" @submit.prevent="go">
        <label class="flex flex-col gap-1 text-xs">
          <span class="text-secondary">{{ t('graph.page.seedKey') }}</span>
          <select
            v-if="keyChoices.length > 0"
            key="choices"
            v-model="keyDraft"
            class="select select-sm"
            data-testid="graph-seed-key"
          >
            <option v-for="choice in keyChoices" :key="choice" :value="choice">{{ choice }}</option>
          </select>
          <input
            v-else
            key="free"
            v-model="keyDraft"
            type="text"
            class="input input-sm font-mono"
            :placeholder="t('graph.page.keyPlaceholder')"
            data-testid="graph-seed-key"
          />
        </label>
        <label class="flex flex-col gap-1 text-xs">
          <span class="text-secondary">{{ t('graph.page.seedValue') }}</span>
          <input
            v-model="valueDraft"
            type="text"
            class="input input-sm font-mono"
            data-testid="graph-seed-value"
          />
        </label>
        <button type="submit" class="btn btn-sm btn-primary" data-testid="graph-seed-go">
          <Search class="size-4" aria-hidden="true"/>
          {{ t('graph.page.go') }}
        </button>
      </form>
    </header>

    <div
      v-if="registryError !== null"
      key="registryFailed"
      role="alert"
      class="alert alert-error"
      data-testid="graph-registry-failed"
    >
      <span>{{
          registryError === 'forbidden' ? t('graph.explorer.forbidden') : t('graph.page.notFound')
        }}</span>
    </div>

    <div
      v-else
      class="drawer drawer-end min-w-0 2xl:drawer-open"
      data-testid="graph-workspace"
      @keydown.esc="railOpen = false"
    >
      <input id="graph-rail-drawer" v-model="railOpen" type="checkbox" class="drawer-toggle"/>
      <div class="drawer-content flex min-w-0 flex-col gap-3">
        <div class="flex items-center justify-end gap-2 2xl:hidden">
          <label
            for="graph-rail-drawer"
            class="btn btn-sm drawer-button"
            data-testid="graph-rail-toggle"
          >
            <PanelRightOpen class="size-4 rtl:rotate-180" aria-hidden="true"/>
            {{ t('graph.page.rail') }}
            <span
              v-if="discovery.patterns.data.value"
              key="count"
              class="badge badge-ghost badge-xs font-mono tabular-nums"
            >
              {{ discovery.patterns.data.value.length }}
            </span>
          </label>
        </div>

        <p
          v-if="!seeded"
          key="pick"
          class="text-sm text-secondary"
          role="status"
          data-testid="graph-pick-seed"
        >
          {{ t('graph.page.pickSeed') }}
        </p>
        <GraphExplorer
          v-else
          key="explorer"
          :registry-guid="guid"
          :seed-key="address.seedKey"
          :seed-value="address.seedValue"
          @open-case="router.push({ name: 'caseDetail', params: { caseId: $event } })"
          @open-community="openCommunity"
          @pivot="reseed($event.key, $event.value)"
        />
      </div>

      <div class="drawer-side z-30 2xl:z-auto">
        <label
          for="graph-rail-drawer"
          :aria-label="t('graph.page.closeRail')"
          class="drawer-overlay"
        ></label>
        <aside
          class="min-h-full w-80 bg-base-100 p-4 text-base-content 2xl:w-72 2xl:bg-transparent 2xl:p-0 2xl:ps-4"
        >
          <GraphRail
            :explorations="mine"
            :pending="explorations.isPending.value"
            :failed="explorations.isError.value"
            :saving="save.isPending.value"
            :can-save="seeded"
            :patterns="discovery.patterns.data.value"
            :patterns-pending="discovery.patterns.isPending.value"
            :patterns-failed="discovery.patterns.isError.value"
            :pattern-state="patternState"
            :communities="discovery.communities.data.value"
            :communities-pending="discovery.communities.isPending.value"
            :communities-failed="discovery.communities.isError.value"
            :hubs="discovery.hubs.data.value"
            :hubs-pending="discovery.hubs.isPending.value"
            :hubs-failed="discovery.hubs.isError.value"
            :can-resolve="canResolve"
            :deciding="decide.isPending.value || override.isPending.value"
            :active-pattern="address.pattern"
            :active-community="address.community"
            @save="save.mutate($event)"
            @restore="(exploration) => ((railOpen = false), restore(exploration))"
            @remove="remove.mutate($event)"
            @update:pattern-state="patternState = $event"
            @open-pattern="(pattern) => ((railOpen = false), openPattern(pattern))"
            @decide="(pattern, stateId, caseId) => decide.mutate({ pattern, stateId, caseId })"
            @open-community="(community) => ((railOpen = false), openCommunity(community))"
            @hub-override="(node, overrideId) => override.mutate({ node, overrideId })"
          />
        </aside>
      </div>
    </div>
  </section>
</template>
