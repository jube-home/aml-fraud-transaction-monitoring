/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

import {computed, onBeforeUnmount, ref, watch, type Ref} from 'vue'
import {keepPreviousData, useQuery} from '@tanstack/vue-query'
import {queryKeys} from '../api/queryKeys'
import {useUiStore} from '../stores/ui'
import type {GraphAddress} from './address'
import {graphApi} from './api'
import {
  activeEdges,
  defaultFlowDepth,
  defaultFlowTolerance,
  defaultFlowWindowHours,
  defaultPathsK,
  defaultPathsMaxHops,
  isCombo,
  timeStripBins,
} from './state'
import type {
  GraphAnchor,
  GraphExplodeRequest,
  GraphFlowRequest,
  GraphLayoutId,
  GraphNeighbourhoodRequest,
  GraphPathsRequest,
  GraphRenderRequest,
  GraphWindow,
} from './types'

export const skeletonDelayMilliseconds = 150

export const discoveryLimit = 50

export function useDelayedPending(
  pending: Ref<boolean>,
  delay = skeletonDelayMilliseconds,
): Ref<boolean> {
  const shown = ref(false)
  let timer: ReturnType<typeof setTimeout> | undefined

  watch(
    pending,
    (value) => {
      clearTimeout(timer)

      if (!value) {
        shown.value = false

        return
      }

      timer = setTimeout(() => {
        shown.value = pending.value
      }, delay)
    },
    {immediate: true},
  )

  onBeforeUnmount(() => clearTimeout(timer))

  return shown
}

function numericId(id: string | null): number | null {
  if (id === null || isCombo(id)) return null

  const numeric = Number(id)

  return Number.isSafeInteger(numeric) && numeric > 0 ? numeric : null
}

export interface GraphQueryInput {
  registryGuid: Ref<string>
  address: Ref<GraphAddress>
  window: Ref<GraphWindow | null>
  minimumAmount: Ref<number>
  includeHubs: Ref<boolean>
  enabled: Ref<boolean>
}

export function useGraphQueries(input: GraphQueryInput) {
  const ui = useUiStore()
  const dark = computed(() => ui.resolvedTheme === 'jube-dark')
  const anchors = ref<GraphAnchor[]>([])

  const registry = useQuery({
    queryKey: computed(() => queryKeys.graphRegistry(input.registryGuid.value)),
    queryFn: () => graphApi.registry(input.registryGuid.value),
    enabled: computed(() => input.registryGuid.value !== ''),
    staleTime: 60_000,
    retry: false,
  })

  const seeded = computed(
    () =>
      input.enabled.value &&
      input.registryGuid.value !== '' &&
      input.address.value.seedKey !== '' &&
      input.address.value.seedValue !== '',
  )

  const edges = computed(() =>
    input.address.value.edges.length > 0 ? input.address.value.edges.join(',') : undefined,
  )

  const request = computed<GraphNeighbourhoodRequest>(() => {
    const address = input.address.value

    return {
      registryGuid: input.registryGuid.value,
      key: address.seedKey,
      value: address.seedValue,
      hops: address.hops,
      from: input.window.value?.from,
      to: input.window.value?.to,
      minimumAmount: input.minimumAmount.value || undefined,
      edges: edges.value,
      includeHubs: input.includeHubs.value || undefined,
      expanded: address.expanded.length > 0 ? address.expanded.join(',') : undefined,
      relevance: address.view === 'relevance' || undefined,
    }
  })

  const flowing = computed(
    () => input.address.value.view === 'flow' && input.address.value.flow !== null,
  )

  const flowRequest = computed<GraphFlowRequest | undefined>(() => {
    const address = input.address.value

    if (!flowing.value || !address.flow) return undefined

    const nodeId = numericId(address.flow.seed)

    if (nodeId === null) return undefined

    return {
      registryGuid: input.registryGuid.value,
      nodeId,
      archiveId: address.flowArchiveId ?? undefined,
      forward: address.flow.direction !== 'backward',
      hopWindowHours: address.flowWindowHours ?? defaultFlowWindowHours,
      tolerance: address.flowTolerance ?? defaultFlowTolerance,
      depth: defaultFlowDepth,
      edges: edges.value,
      from: input.window.value?.from,
      to: input.window.value?.to,
    }
  })

  const layout = computed<GraphLayoutId | undefined>(() => input.address.value.layout ?? undefined)

  const renderRequest = computed<GraphRenderRequest>(() => ({
    query: request.value,
    flow: flowRequest.value,
    layoutId: layout.value,
    measureId: 1,
    anchors: [],
    dark: dark.value,
    captions: [],
  }))

  const render = useQuery({
    queryKey: computed(() =>
      queryKeys.graphRender(
        {...request.value, flow: flowRequest.value, layoutId: layout.value},
        dark.value,
      ),
    ),
    queryFn: ({signal}) =>
      graphApi.render({...renderRequest.value, anchors: anchors.value}, signal),
    enabled: computed(() => seeded.value && (!flowing.value || flowRequest.value !== undefined)),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  watch(
    () => render.data.value,
    (result) => {
      if (result) anchors.value = result.positions
    },
  )

  const flow = useQuery({
    queryKey: computed(() => queryKeys.graphFlow(flowRequest.value)),
    queryFn: ({signal}) => graphApi.flow(flowRequest.value!, signal),
    enabled: computed(() => seeded.value && flowRequest.value !== undefined),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  const timeStrip = useQuery({
    queryKey: computed(() => queryKeys.graphTimeStrip(request.value, timeStripBins, dark.value)),
    queryFn: () => graphApi.timeStripSvg(request.value, timeStripBins, dark.value),
    enabled: seeded,
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  const selectedNodeId = computed(() => numericId(input.address.value.selected))

  const node = useQuery({
    queryKey: computed(() => queryKeys.graphNode(selectedNodeId.value ?? 0)),
    queryFn: ({signal}) => graphApi.node({nodeId: selectedNodeId.value ?? 0}, signal),
    enabled: computed(() => input.enabled.value && selectedNodeId.value !== null),
    staleTime: 30_000,
    retry: false,
  })

  const pathsTargetId = computed(() => numericId(input.address.value.pathsTo))

  const pathsRequest = computed<GraphPathsRequest>(() => ({
    registryGuid: input.registryGuid.value,
    fromNodeId: selectedNodeId.value ?? 0,
    toNodeId: pathsTargetId.value ?? 0,
    maxHops: defaultPathsMaxHops,
    k: defaultPathsK,
    includeHubs: input.includeHubs.value || undefined,
    edges: edges.value,
    from: input.window.value?.from,
    to: input.window.value?.to,
  }))

  const paths = useQuery({
    queryKey: computed(() => queryKeys.graphPaths(pathsRequest.value)),
    queryFn: ({signal}) => graphApi.paths(pathsRequest.value, signal),
    enabled: computed(
      () => seeded.value && selectedNodeId.value !== null && pathsTargetId.value !== null,
    ),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  const patternId = computed(() => numericId(input.address.value.pattern))

  const pattern = useQuery({
    queryKey: computed(() => queryKeys.graphPattern(patternId.value ?? 0)),
    queryFn: () => graphApi.pattern(patternId.value ?? 0),
    enabled: computed(() => input.enabled.value && patternId.value !== null),
    staleTime: 60_000,
    retry: false,
  })

  const communityId = computed(() => numericId(input.address.value.community))

  const community = useQuery({
    queryKey: computed(() => queryKeys.graphCommunity(communityId.value ?? 0)),
    queryFn: () => graphApi.community(communityId.value ?? 0),
    enabled: computed(() => input.enabled.value && communityId.value !== null),
    staleTime: 60_000,
    retry: false,
  })

  const explodeNodeId = computed(
    () => selectedNodeId.value ?? render.data.value?.graph.seed?.id ?? null,
  )

  const explodeRequest = computed<GraphExplodeRequest>(() => ({
    registryGuid: input.registryGuid.value,
    nodeId: explodeNodeId.value ?? 0,
    from: input.window.value?.from,
    to: input.window.value?.to,
    edges:
      input.address.value.edges.length > 0
        ? activeEdges(input.address.value, render.data.value?.graph.definitions ?? []).join(',')
        : undefined,
  }))

  const explode = useQuery({
    queryKey: computed(() => queryKeys.graphExplode(explodeRequest.value)),
    queryFn: ({signal}) => graphApi.explode(explodeRequest.value, signal),
    enabled: computed(
      () => seeded.value && input.address.value.explode && explodeNodeId.value !== null,
    ),
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  const renderSkeleton = useDelayedPending(computed(() => render.isPending.value && seeded.value))

  return {
    dark,
    registry,
    request,
    flowRequest,
    renderRequest,
    render,
    renderSkeleton,
    flow,
    timeStrip,
    selectedNodeId,
    node,
    pathsTargetId,
    paths,
    pattern,
    community,
    explodeNodeId,
    explode,
    anchors,
  }
}

export interface GraphDiscoveryInput {
  enabled: Ref<boolean>
  patternState: Ref<number | null>
}

export function useGraphDiscoveryQueries(input: GraphDiscoveryInput) {
  const patternRequest = computed(() => ({
    stateId: input.patternState.value ?? undefined,
    limit: discoveryLimit,
  }))

  const patterns = useQuery({
    queryKey: computed(() => queryKeys.graphPatterns(patternRequest.value)),
    queryFn: () => graphApi.patterns(patternRequest.value),
    enabled: input.enabled,
    placeholderData: keepPreviousData,
    staleTime: 30_000,
    retry: false,
  })

  const communities = useQuery({
    queryKey: queryKeys.graphCommunities(discoveryLimit),
    queryFn: () => graphApi.communities(discoveryLimit),
    enabled: input.enabled,
    staleTime: 60_000,
    retry: false,
  })

  const hubs = useQuery({
    queryKey: queryKeys.graphHubs(),
    queryFn: () => graphApi.hubs(),
    enabled: input.enabled,
    staleTime: 60_000,
    retry: false,
  })

  return {patterns, communities, hubs}
}
