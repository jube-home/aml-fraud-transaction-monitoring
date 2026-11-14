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
import {useQuery} from '@tanstack/vue-query'
import {AlertTriangle, Settings} from 'lucide-vue-next'
import {ApiError} from '../api/useApi'
import {hasPermission, meQueryOptions} from '../auth/me'
import ProblemAlert from '../components/ProblemAlert.vue'
import {useDebounced} from '../composables/useDebounced'
import {useFormat} from '../composables/useFormat'
import {useToasts} from '../stores/toasts'
import {
  mergeGraphAddress,
  parseGraphAddress,
  type FlowDirection,
  type GraphAddress,
  type GraphHops,
  type GraphView,
} from './address'
import {graphApi} from './api'
import GraphCanvas from './GraphCanvas.vue'
import GraphExplode from './GraphExplode.vue'
import GraphFlow from './GraphFlow.vue'
import GraphLegend from './GraphLegend.vue'
import GraphNodePanel from './GraphNodePanel.vue'
import GraphPaths from './GraphPaths.vue'
import GraphTimeStrip from './GraphTimeStrip.vue'
import GraphToolbar from './GraphToolbar.vue'
import {
  addsOnExpand,
  comboOf,
  defaultGraphState,
  edgeById,
  edgesForArchive,
  emphasisForCommunity,
  emphasisForInterest,
  emphasisForPaths,
  emphasisForPattern,
  emphasisForTranche,
  emptyEmphasis,
  expand as expandNode,
  followMoney,
  hiddenAtCursor,
  leaveFlow,
  mergeEmphasis,
  nodeById,
  renderCapCeiling,
  setCursor,
  setExplode,
  setFlowDirection,
  setFlowTolerance,
  setFlowWindowHours,
  setHops,
  setLayout,
  setPathsTarget,
  setCommunity,
  setPattern,
  setView,
  toggleEdge,
  windowFromDays,
  withAddress,
  type GraphState,
} from './state'
import type {GraphCombo, GraphLayoutId, GraphNode, GraphPattern, GraphWindow} from './types'
import {useGraphQueries} from './useGraphQueries'

const authorPermission = 50

const props = withDefaults(
  defineProps<{
    registryGuid: string
    seedKey: string
    seedValue: string
    embedded?: boolean
    caseId?: number
    workflow?: string
    window?: GraphWindow | null
    selection?: number | null
  }>(),
  {embedded: false, caseId: undefined, workflow: '', window: null, selection: null},
)

const emit = defineEmits<{
  'update:window': [window: GraphWindow]
  'update:selection': [archiveId: number | null]
  pivot: [target: { key: string; value: string }]
  filterJournal: [archiveIds: number[]]
  openCase: [caseId: number]
  link: [node: GraphNode]
  openPattern: [pattern: GraphPattern]
  openCommunity: [communityId: number]
}>()

const {t} = useI18n()
const route = useRoute()
const router = useRouter()
const toasts = useToasts()
const {dateTime} = useFormat()
const me = useQuery(meQueryOptions)

const address = computed<GraphAddress>(() => ({
  ...parseGraphAddress(route.query),
  registry: props.registryGuid,
  seedKey: props.seedKey,
  seedValue: props.seedValue,
}))

const ownWindow = ref<GraphWindow | null>(null)
const minimumAmountDraft = ref(defaultGraphState.minimumAmount)
const minimumAmount = useDebounced(minimumAmountDraft, 300)
const includeHubs = ref(defaultGraphState.includeHubs)
const selectedEdge = ref<string | null>(null)
const capturing = ref(false)
const pathsPicking = ref(false)
const pathsNotFound = ref(false)
const pathIndex = ref<number | null>(null)
const trancheDepth = ref<number | null>(null)

const registryGuid = computed(() => props.registryGuid)
const enabled = ref(true)

const queries = useGraphQueries({
  registryGuid,
  address,
  window: computed(() => (props.embedded ? props.window : ownWindow.value)),
  minimumAmount,
  includeHubs,
  enabled,
})

const defaultWindow = computed(() =>
  windowFromDays(queries.registry.data.value?.defaultWindowDays ?? 30, new Date()),
)

watch(
  () => queries.registry.data.value,
  (registry) => {
    if (registry && !props.embedded && ownWindow.value === null) {
      ownWindow.value = windowFromDays(registry.defaultWindowDays, new Date())
    }
  },
  {immediate: true},
)

const state = computed<GraphState>(() => ({
  address: address.value,
  window: props.embedded ? props.window : ownWindow.value,
  minimumAmount: minimumAmount.value,
  includeHubs: includeHubs.value,
  selectedEdge: selectedEdge.value,
}))

function write(next: GraphState): void {
  const target = props.embedded
    ? {...next.address, registry: '', seedKey: '', seedValue: ''}
    : next.address

  void router.replace({query: mergeGraphAddress(route.query, target)})
}

const result = computed(() => queries.render.data.value)
const graph = computed(() => result.value?.graph)
const definitions = computed(() => graph.value?.definitions ?? [])
const flowing = computed(() => address.value.view === 'flow' && address.value.flow !== null)
const layout = computed<GraphLayoutId>(
  () =>
    address.value.layout ??
    ((result.value?.layoutId ??
      queries.registry.data.value?.defaultLayoutId ??
      1) as GraphLayoutId),
)
const cap = computed(() =>
  Math.min(renderCapCeiling, queries.registry.data.value?.renderCap ?? renderCapCeiling),
)

const hidden = computed(() =>
  graph.value ? hiddenAtCursor(graph.value, address.value.cursor) : new Set<string>(),
)

const emphasised = computed(() => {
  if (!graph.value) return new Set<string>()

  const selection = emptyEmphasis()

  for (const id of edgesForArchive(graph.value.edges, props.selection ?? null))
    selection.edges.add(id)

  return mergeEmphasis(
    selection,
    emphasisForPaths(queries.paths.data.value, pathIndex.value),
    emphasisForPattern(queries.pattern.data.value, graph.value.edges),
    emphasisForCommunity(queries.community.data.value),
    flowing.value
      ? emphasisForTranche(queries.flow.data.value, trancheDepth.value)
      : emptyEmphasis(),
    address.value.view === 'relevance' ? emphasisForInterest(graph.value.nodes) : emptyEmphasis(),
  )
})

const selectedNode = computed(() =>
  graph.value ? nodeById(graph.value.nodes, address.value.selected) : null,
)

const hasSelection = computed(() => address.value.selected !== null || combo.value !== null)
const combo = computed<GraphCombo | null>(() =>
  graph.value ? comboOf(address.value.selected, graph.value.combos) : null,
)

const adds = computed(() =>
  selectedNode.value && graph.value ? addsOnExpand(selectedNode.value, graph.value) : 0,
)

const expanded = computed(
  () => address.value.selected !== null && address.value.expanded.includes(address.value.selected),
)

const keys = computed(() => {
  const seen: string[] = []

  for (const node of graph.value?.nodes ?? []) {
    const key = node.key ?? ''

    if (key !== '' && !seen.includes(key)) seen.push(key)
  }

  return seen
})

const errorKind = computed(() => {
  const error = queries.render.error.value

  return error instanceof ApiError ? error.kind : error ? 'server' : null
})

const notNode = computed(() => graph.value !== undefined && graph.value.seed === null)

const staleHours = computed(() => {
  const built = graph.value?.builtDate

  if (!built) return null

  return (Date.now() - Date.parse(built)) / 3_600_000
})

const canAuthor = computed(() => hasPermission(me.data.value, authorPermission))

const noSeed = computed(() => props.seedKey === '' || props.seedValue === '')

const showPaths = computed(() => pathsPicking.value || address.value.pathsTo !== null)

function pick(id: string | null): void {
  if (pathsPicking.value && id !== null) {
    pathsPicking.value = false
    pathsNotFound.value = false
    pathIndex.value = null
    write(setPathsTarget(state.value, id))

    return
  }

  selectedEdge.value = null
  pathsPicking.value = false
  write(
    withAddress(state.value, {selected: id, pathsTo: id === null ? null : address.value.pathsTo}),
  )
}

function pickEdge(id: string | null): void {
  selectedEdge.value = id

  const edge = graph.value ? edgeById(graph.value.edges, id) : null

  if (edge) emit('filterJournal', edge.archiveIds)
}

function onExpand(nodeId: string): void {
  write(expandNode(state.value, nodeId))
}

function onView(view: GraphView): void {
  if (view === 'flow') {
    write(setView(state.value, 'flow'))

    return
  }

  write(flowing.value ? withAddress(leaveFlow(state.value), {view}) : setView(state.value, view))
}

function onHops(hops: GraphHops): void {
  write(setHops(flowing.value ? leaveFlow(state.value) : state.value, hops))
}

function onLayout(next: GraphLayoutId): void {
  write(setLayout(state.value, next))
}

function onToggleEdge(guid: string): void {
  write(toggleEdge(state.value, guid, definitions.value))
}

function onExplode(value: boolean): void {
  write(setExplode(state.value, value))
}

function onWindow(window: GraphWindow): void {
  if (!props.embedded) ownWindow.value = window

  write(setCursor(state.value, null))
  emit('update:window', window)
}

function onReset(): void {
  onWindow(defaultWindow.value)
}

function onCursor(cursor: number | null): void {
  write(setCursor(state.value, cursor))
}

function onExplodeCombo(target: GraphCombo): void {
  write(withAddress(setExplode(state.value, true), {selected: String(target.viaNodeId)}))
}

function onPivot(target: { key: string; value: string }): void {
  emit('pivot', target)
}

function onPaths(): void {
  if (pathsPicking.value) {
    pathsPicking.value = false

    return
  }

  if (address.value.pathsTo !== null) {
    write(setPathsTarget(state.value, null))

    return
  }

  pathsNotFound.value = false
  pathsPicking.value = true
}

async function onPathsTarget(target: { key: string; value: string }): Promise<void> {
  pathsNotFound.value = false

  try {
    const facts = await graphApi.node(target)

    pathsPicking.value = false
    pathIndex.value = null
    write(setPathsTarget(state.value, String(facts.node.id)))
  } catch (error) {
    if (error instanceof ApiError && error.kind === 'notFound') pathsNotFound.value = true
    else toasts.apiError(error)
  }
}

function onPathsClear(): void {
  pathIndex.value = null
  write(setPathsTarget(state.value, null))
}

function onFollow(nodeId: string): void {
  trancheDepth.value = null
  write(followMoney(state.value, nodeId, props.selection ?? null))
}

function onLeaveFlow(): void {
  trancheDepth.value = null
  write(leaveFlow(state.value))
}

function onFlowDirection(direction: FlowDirection): void {
  write(setFlowDirection(state.value, direction))
}

function onFlowHours(hours: number | null): void {
  write(setFlowWindowHours(state.value, hours))
}

function onFlowTolerance(tolerance: number | null): void {
  write(setFlowTolerance(state.value, tolerance))
}

function onPattern(pattern: GraphPattern): void {
  write(
    setPattern(
      state.value,
      address.value.pattern === String(pattern.id) ? null : String(pattern.id),
    ),
  )
  emit('openPattern', pattern)
}

function onCommunity(communityId: number): void {
  if (props.embedded) write(setCommunity(state.value, String(communityId)))
  else emit('openCommunity', communityId)
}

async function capture(): Promise<void> {
  if (props.caseId === undefined || capturing.value) return

  capturing.value = true

  try {
    const captured = await graphApi.capture({
      caseId: props.caseId,
      render: {...queries.renderRequest.value, anchors: queries.anchors.value},
    })

    toasts.success(t('graph.explorer.captured', {nodes: captured.nodes, edges: captured.edges}))
  } catch (error) {
    toasts.apiError(error)
  } finally {
    capturing.value = false
  }
}

watch(
  () => queries.render.error.value,
  (error) => {
    if (error instanceof ApiError && (error.kind === 'server' || error.kind === 'network'))
      toasts.apiError(error)
  },
)
</script>

<template>
  <section class="flex min-w-0 flex-col gap-3" data-testid="graph-explorer">
    <p
      v-if="noSeed"
      key="noSeed"
      class="text-sm text-secondary"
      role="status"
      data-testid="graph-no-seed"
    >
      {{ t('graph.explorer.noSeed') }}
    </p>

    <div
      v-else-if="errorKind === 'forbidden'"
      key="forbidden"
      role="alert"
      class="alert alert-error"
      data-testid="graph-forbidden"
    >
      <span>{{ t('graph.explorer.forbidden') }}</span>
    </div>

    <div
      v-else-if="errorKind === 'notFound'"
      key="notFound"
      role="alert"
      class="alert alert-error"
      data-testid="graph-not-found"
    >
      <span>{{ t('graph.explorer.notFound') }}</span>
    </div>

    <div v-else-if="errorKind !== null && !graph" key="failed" class="flex flex-col gap-2">
      <ProblemAlert :reasons="[t('graph.explorer.failed')]"/>
      <button
        type="button"
        class="btn btn-sm w-fit"
        data-testid="graph-retry"
        @click="queries.render.refetch()"
      >
        {{ t('graph.explorer.retry') }}
      </button>
    </div>

    <div
      v-else-if="graph?.noDefinitions"
      key="noDefinitions"
      role="status"
      class="alert alert-soft"
      data-testid="graph-no-definitions"
    >
      <span>{{ t('graph.explorer.noDefinitions') }}</span>
      <RouterLink
        v-if="canAuthor"
        key="configure"
        :to="{ name: 'adminGraphs' }"
        class="btn btn-sm"
        data-testid="graph-configure"
      >
        <Settings class="size-4" aria-hidden="true"/>
        {{ t('graph.explorer.configure') }}
      </RouterLink>
    </div>

    <template v-else>
      <GraphToolbar
        :view="address.view"
        :hops="address.hops"
        :layout="layout"
        :edges="address.edges"
        :definitions="definitions"
        :hidden-hubs="graph?.hiddenHubs ?? 0"
        :include-hubs="includeHubs"
        :minimum-amount="minimumAmountDraft"
        :nodes="graph?.nodes.length ?? 0"
        :cap="cap"
        :explode="address.explode"
        :can-capture="caseId !== undefined"
        :capturing="capturing"
        :disabled="queries.render.isPending.value"
        :flow-available="selectedNode !== null"
        :flow-direction="address.flow?.direction ?? null"
        :flow-window-hours="address.flowWindowHours"
        :flow-tolerance="address.flowTolerance"
        @update:view="onView"
        @update:hops="onHops"
        @update:layout="onLayout"
        @toggle-edge="onToggleEdge"
        @update:include-hubs="includeHubs = $event"
        @update:minimum-amount="minimumAmountDraft = $event"
        @update:explode="onExplode"
        @update:flow-direction="onFlowDirection"
        @update:flow-window-hours="onFlowHours"
        @update:flow-tolerance="onFlowTolerance"
        @capture="capture"
      />

      <div
        v-if="graph && !graph.builtDate"
        key="notBuilt"
        role="status"
        class="alert alert-warning alert-soft text-sm"
        data-testid="graph-not-built"
      >
        <AlertTriangle class="size-4" aria-hidden="true"/>
        <span>{{ t('graph.explorer.notBuilt') }}</span>
      </div>
      <div
        v-else-if="staleHours !== null && staleHours > 24"
        key="stale"
        role="status"
        class="alert alert-warning alert-soft text-sm"
        data-testid="graph-stale"
      >
        <AlertTriangle class="size-4" aria-hidden="true"/>
        <span>{{ t('graph.explorer.stale', {when: dateTime(graph!.builtDate!)}) }}</span>
      </div>

      <p
        v-if="notNode"
        key="notNode"
        role="status"
        class="text-sm text-secondary"
        data-testid="graph-not-node"
      >
        {{ t('graph.explorer.notNode', {key: seedKey, value: seedValue}) }}
      </p>

      <p
        v-if="graph && graph.overCap > 0"
        key="overCap"
        class="text-xs text-secondary"
        data-testid="graph-over-cap"
      >
        {{
          graph.relevance
            ? t('graph.explorer.overCapRelevance', {count: graph.overCap})
            : t('graph.explorer.overCap', {count: graph.overCap})
        }}
      </p>

      <div
        class="grid min-w-0 gap-3"
        :class="{ 'xl:grid-cols-[minmax(0,1fr)_18rem]': hasSelection }"
        data-testid="graph-workbench"
      >
        <div class="flex min-w-0 flex-col gap-3">
          <GraphExplode
            v-if="address.explode"
            key="explode"
            :data="queries.explode.data.value"
            :pending="queries.explode.isPending.value"
            :failed="queries.explode.isError.value"
            :selected="address.selected"
            :window="state.window"
            :workflow="workflow"
            :by-interest="address.view === 'relevance'"
            @select="pick"
            @pivot="onPivot"
          />
          <GraphCanvas
            v-else
            key="canvas"
            :svg="result?.svg ?? null"
            :selected="address.selected"
            :selected-edge="selectedEdge"
            :hidden="hidden"
            :emphasised="emphasised"
            :skeleton="queries.renderSkeleton.value"
            :refreshing="queries.render.isFetching.value && !queries.render.isPending.value"
            :picking="pathsPicking"
            @pick="pick"
            @pick-edge="pickEdge"
          />

          <GraphFlow
            v-if="flowing"
            key="flow"
            :data="queries.flow.data.value"
            :pending="queries.flow.isPending.value"
            :failed="queries.flow.isError.value"
            :selected-depth="trancheDepth"
            @tranche="trancheDepth = $event"
            @filter-journal="emit('filterJournal', $event)"
            @leave="onLeaveFlow"
          />

          <GraphPaths
            v-if="showPaths"
            key="paths"
            :picking="pathsPicking"
            :data="queries.paths.data.value"
            :pending="queries.paths.isPending.value"
            :failed="queries.paths.isError.value"
            :not-found="pathsNotFound"
            :selected-index="pathIndex"
            @cancel="pathsPicking = false"
            @clear="onPathsClear"
            @pick="pathIndex = $event"
            @target="onPathsTarget"
          />

          <GraphTimeStrip
            :svg="queries.timeStrip.data.value ?? null"
            :cursor="address.cursor"
            :pending="queries.timeStrip.isPending.value"
            :failed="queries.timeStrip.isError.value"
            @update:window="onWindow"
            @update:cursor="onCursor"
            @reset="onReset"
          />

          <GraphLegend :definitions="definitions" :keys="keys"/>
        </div>

        <GraphNodePanel
          v-if="hasSelection"
          :node-id="address.selected"
          :node="selectedNode"
          :combo="combo"
          :facts="queries.node.data.value"
          :pending="queries.node.isPending.value && queries.selectedNodeId.value !== null"
          :failed="queries.node.isError.value"
          :expanded="expanded"
          :adds="adds"
          :can-capture="caseId !== undefined"
          :can-link="embedded && caseId !== undefined"
          :paths-picking="pathsPicking"
          :flowing="flowing && address.flow?.seed === address.selected"
          @expand="onExpand"
          @pivot="onPivot"
          @link="emit('link', $event)"
          @open-case="emit('openCase', $event)"
          @capture="capture"
          @explode-combo="onExplodeCombo"
          @paths="onPaths"
          @follow="onFollow"
          @pattern="onPattern"
          @community="onCommunity"
        />
        <p
          v-else
          key="hint"
          class="text-xs text-secondary"
          role="status"
          data-testid="graph-panel-hint"
        >
          {{ t('graph.panel.hint') }}
        </p>
      </div>
    </template>
  </section>
</template>
