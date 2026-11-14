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

import {
  defaultGraphAddress,
  maximumExpanded,
  maximumFlowTolerance,
  maximumFlowWindowHours,
  type FlowDirection,
  type GraphAddress,
  type GraphHops,
  type GraphView,
} from './address'
import type {
  GraphCombo,
  GraphCommunity,
  GraphEdge,
  GraphEdgeDefinition,
  GraphFlow,
  GraphLayoutId,
  GraphNeighbourhood,
  GraphNode,
  GraphNodeTag,
  GraphPaths,
  GraphPattern,
  GraphWindow,
} from './types'

export const maximumHops: GraphHops = 3

export const maximumFanOut = 100

export const defaultFanOut = 25

export const renderCapCeiling = 150

export const maximumMinimumAmount = 1_000_000_000

export const timeStripBins = 40

export const brushDebounceMilliseconds = 300

export const playbackStepMilliseconds = 700

export const comboPrefix = 'c:'

export const defaultFlowWindowHours = 72

export const defaultFlowTolerance = 0.2

export const defaultFlowDepth = 4

export const defaultPathsMaxHops = 4

export const defaultPathsK = 5

export const interestEmphasisFloor = 0.6

export const resolvePermission = 51

export const patternStates = {new: 1, seen: 2, dismissed: 3, cased: 4} as const

export const caseNodeKey = 'Case'

export const caseEdgeKindId = 5

export type GraphEdgeKindKey = 'identity' | 'money' | 'ownership' | 'resolution' | 'case'

const edgeKindKeys: Record<number, GraphEdgeKindKey> = {
  1: 'identity',
  2: 'money',
  3: 'ownership',
  4: 'resolution',
  [caseEdgeKindId]: 'case',
}

export function edgeKindKey(kindId: number): GraphEdgeKindKey | null {
  return edgeKindKeys[kindId] ?? null
}

export function isCaseNode(node: Pick<GraphNode, 'key'> | null | undefined): boolean {
  return node?.key === caseNodeKey
}

export function caseIdOf(node: Pick<GraphNode, 'key' | 'value'> | null | undefined): number | null {
  if (!isCaseNode(node) || !node?.value) return null

  const id = Number(node.value)

  return Number.isInteger(id) && id > 0 ? id : null
}

export function tagChips(node: Pick<GraphNode, 'tags'> | null | undefined): GraphNodeTag[] {
  return (node?.tags ?? [])
    .filter((item): item is GraphNodeTag & { tag: string } => item.tag !== null && item.tag !== '')
    .sort((left, right) => left.distance - right.distance || left.tag.localeCompare(right.tag))
}

export function nearestTagOf(
  node: Pick<GraphNode, 'nearestTag' | 'nearestTagDistance' | 'tags'> | null | undefined,
): { tag: string; distance: number } | null {
  if (!node) return null

  if (node.nearestTag && node.nearestTagDistance !== null)
    return {tag: node.nearestTag, distance: node.nearestTagDistance}

  const first = tagChips(node)[0]

  return first ? {tag: first.tag!, distance: first.distance} : null
}

export const hubOverrides = {none: 0, never: 1, always: 2} as const

export interface GraphState {
  address: GraphAddress
  window: GraphWindow | null
  minimumAmount: number
  includeHubs: boolean
  selectedEdge: string | null
}

export const defaultGraphState: GraphState = {
  address: defaultGraphAddress,
  window: null,
  minimumAmount: 0,
  includeHubs: false,
  selectedEdge: null,
}

export function withAddress(state: GraphState, address: Partial<GraphAddress>): GraphState {
  return {...state, address: {...state.address, ...address}}
}

export function expand(state: GraphState, nodeId: string): GraphState {
  if (nodeId === '' || state.address.expanded.includes(nodeId)) return state

  const expanded = [...state.address.expanded, nodeId].slice(-maximumExpanded)

  return withAddress(state, {expanded})
}

export function collapse(state: GraphState, nodeId: string): GraphState {
  if (!state.address.expanded.includes(nodeId)) return state

  return withAddress(state, {expanded: state.address.expanded.filter((id) => id !== nodeId)})
}

export function select(state: GraphState, nodeId: string | null): GraphState {
  return {
    ...withAddress(state, {
      selected: nodeId,
      pathsTo: nodeId === null ? null : state.address.pathsTo,
    }),
    selectedEdge: null,
  }
}

export function selectEdge(state: GraphState, edgeId: string | null): GraphState {
  return {...state, selectedEdge: edgeId}
}

export function setLayout(state: GraphState, layout: GraphLayoutId | null): GraphState {
  return withAddress(state, {layout})
}

export function setHops(state: GraphState, hops: number): GraphState {
  const bounded = Math.min(maximumHops, Math.max(1, Math.round(hops))) as GraphHops

  return withAddress(state, {hops: bounded, view: 'hops'})
}

export function setView(state: GraphState, view: GraphView): GraphState {
  if (view === 'flow' && state.address.flow === null) {
    if (state.address.selected === null || isCombo(state.address.selected)) return state

    return withAddress(state, {
      view,
      flow: {seed: state.address.selected, direction: 'forward'},
    })
  }

  return withAddress(state, {view})
}

export function followMoney(
  state: GraphState,
  nodeId: string,
  archiveId: number | null = null,
): GraphState {
  return withAddress(state, {
    view: 'flow',
    flow: {seed: nodeId, direction: 'forward'},
    flowArchiveId: archiveId,
    selected: nodeId,
    pathsTo: null,
  })
}

export function setFlowDirection(state: GraphState, direction: FlowDirection): GraphState {
  if (!state.address.flow) return state

  return withAddress(state, {flow: {...state.address.flow, direction}})
}

export function setFlowWindowHours(state: GraphState, hours: number | null): GraphState {
  const bounded =
    hours === null || !Number.isFinite(hours)
      ? null
      : Math.min(maximumFlowWindowHours, Math.max(1, Math.round(hours)))

  return withAddress(state, {
    flowWindowHours: bounded === defaultFlowWindowHours ? null : bounded,
  })
}

export function setFlowTolerance(state: GraphState, tolerance: number | null): GraphState {
  const bounded =
    tolerance === null || !Number.isFinite(tolerance)
      ? null
      : Math.min(maximumFlowTolerance, Math.max(0, Math.round(tolerance * 100) / 100))

  return withAddress(state, {flowTolerance: bounded === defaultFlowTolerance ? null : bounded})
}

export function leaveFlow(state: GraphState): GraphState {
  return withAddress(state, {view: 'hops', flow: null, flowArchiveId: null})
}

export function setPathsTarget(state: GraphState, nodeId: string | null): GraphState {
  if (nodeId !== null && (isCombo(nodeId) || nodeId === state.address.selected)) return state

  return withAddress(state, {pathsTo: nodeId})
}

export function setPattern(state: GraphState, patternId: string | null): GraphState {
  return withAddress(state, {pattern: patternId, community: null})
}

export function setCommunity(state: GraphState, communityId: string | null): GraphState {
  return withAddress(state, {community: communityId, pattern: null})
}

export function toggleEdge(
  state: GraphState,
  guid: string,
  definitions: GraphEdgeDefinition[],
): GraphState {
  const definition = definitions.find((candidate) => candidate.guid === guid)

  if (!definition || !definition.visible) return state

  const current =
    state.address.edges.length === 0
      ? definitions.filter((definition) => definition.visible).map((definition) => definition.guid)
      : state.address.edges
  const next = current.includes(guid) ? current.filter((id) => id !== guid) : [...current, guid]
  const every = definitions.filter((definition) => definition.visible).map((d) => d.guid)
  const all = every.length === next.length && every.every((id) => next.includes(id))

  return withAddress(state, {edges: all ? [] : next})
}

export function activeEdges(address: GraphAddress, definitions: GraphEdgeDefinition[]): string[] {
  if (address.edges.length === 0) {
    return definitions.filter((definition) => definition.visible).map((d) => d.guid)
  }

  return address.edges
}

export function setWindow(state: GraphState, window: GraphWindow | null): GraphState {
  return {...withAddress(state, {cursor: null}), window}
}

export function setCursor(state: GraphState, cursor: number | null): GraphState {
  return withAddress(state, {cursor})
}

export function setMinimumAmount(state: GraphState, amount: number): GraphState {
  const bounded = Number.isFinite(amount) ? Math.min(maximumMinimumAmount, Math.max(0, amount)) : 0

  return {...state, minimumAmount: bounded}
}

export function setIncludeHubs(state: GraphState, includeHubs: boolean): GraphState {
  return {...state, includeHubs}
}

export function setExplode(state: GraphState, explode: boolean): GraphState {
  return withAddress(state, {explode})
}

export function isCombo(id: string | null): boolean {
  return id !== null && id.startsWith(comboPrefix)
}

export function comboOf(id: string | null, combos: GraphCombo[]): GraphCombo | null {
  if (!isCombo(id)) return null

  const comboId = id!.slice(comboPrefix.length)

  return combos.find((combo) => combo.id === comboId) ?? null
}

export function comboNodeId(combo: GraphCombo): string {
  return `${comboPrefix}${combo.id ?? ''}`
}

export function edgeFirst(edge: GraphEdge): number {
  return Math.floor(Date.parse(edge.windowFirst ?? edge.firstSeen) / 1000)
}

export function hiddenAtCursor(
  graph: Pick<GraphNeighbourhood, 'nodes' | 'edges' | 'seed'>,
  cursor: number | null,
): Set<string> {
  const hidden = new Set<string>()

  if (cursor === null) return hidden

  const visibleTouching = new Set<number>()

  for (const edge of graph.edges) {
    if (edgeFirst(edge) > cursor) hidden.add(String(edge.id))
    else {
      visibleTouching.add(edge.fromNodeId)
      visibleTouching.add(edge.toNodeId)
    }
  }

  for (const node of graph.nodes) {
    if (node.seed || graph.seed?.id === node.id) continue
    if (!visibleTouching.has(node.id)) hidden.add(String(node.id))
  }

  return hidden
}

export function edgesForArchive(edges: GraphEdge[], archiveId: number | null): Set<string> {
  const found = new Set<string>()

  if (archiveId === null) return found

  for (const edge of edges) if (edge.archiveIds.includes(archiveId)) found.add(String(edge.id))

  return found
}

export function nodeById(nodes: GraphNode[], id: string | null): GraphNode | null {
  if (id === null || isCombo(id)) return null

  const numeric = Number(id)

  return nodes.find((node) => node.id === numeric) ?? null
}

export function edgeById(edges: GraphEdge[], id: string | null): GraphEdge | null {
  if (id === null) return null

  const numeric = Number(id)

  return edges.find((edge) => edge.id === numeric) ?? null
}

export function addsOnExpand(node: GraphNode, drawn: Pick<GraphNeighbourhood, 'edges'>): number {
  const touching = new Set<number>()

  for (const edge of drawn.edges) {
    if (edge.fromNodeId === node.id) touching.add(edge.toNodeId)
    if (edge.toNodeId === node.id) touching.add(edge.fromNodeId)
  }

  return Math.max(0, node.degree - touching.size)
}

export function windowFromDays(days: number, now: Date): GraphWindow {
  const to = new Date(
    Date.UTC(
      now.getUTCFullYear(),
      now.getUTCMonth(),
      now.getUTCDate(),
      now.getUTCHours(),
      now.getUTCMinutes(),
    ),
  )
  const from = new Date(to.getTime() - days * 86_400_000)

  return {from: from.toISOString(), to: to.toISOString()}
}

export function daysOfWindow(window: GraphWindow): number {
  const span = Date.parse(window.to) - Date.parse(window.from)

  return Math.max(1, Math.ceil(span / 86_400_000))
}

export function nearestDays(days: number, choices: readonly number[]): number {
  let best = choices[0] ?? days

  for (const choice of choices) {
    if (Math.abs(choice - days) < Math.abs(best - days)) best = choice
  }

  return best
}

export interface Emphasis {
  nodes: Set<string>
  edges: Set<string>
}

export function emptyEmphasis(): Emphasis {
  return {nodes: new Set(), edges: new Set()}
}

export function emphasisForPaths(
  paths: GraphPaths | undefined,
  pathIndex: number | null,
): Emphasis {
  const found = emptyEmphasis()

  if (!paths) return found

  const chosen =
    pathIndex === null ? paths.paths : paths.paths.filter((_, index) => index === pathIndex)

  for (const path of chosen) {
    for (const id of path.nodeIds) found.nodes.add(String(id))
    for (const id of path.edgeIds) found.edges.add(String(id))
  }

  return found
}

export function emphasisForPattern(
  pattern: GraphPattern | undefined,
  edges: GraphEdge[],
): Emphasis {
  const found = emptyEmphasis()

  if (!pattern) return found

  for (const member of pattern.members) found.nodes.add(String(member.node.id))
  if (pattern.anchor) found.nodes.add(String(pattern.anchor.id))

  const archive = new Set(pattern.archiveIds)

  for (const edge of edges) {
    if (edge.archiveIds.some((id) => archive.has(id))) found.edges.add(String(edge.id))
  }

  return found
}

export function emphasisForCommunity(community: GraphCommunity | undefined): Emphasis {
  const found = emptyEmphasis()

  for (const member of community?.members ?? []) found.nodes.add(String(member.id))

  return found
}

export function emphasisForTranche(flow: GraphFlow | undefined, depth: number | null): Emphasis {
  const found = emptyEmphasis()

  if (!flow) return found

  for (const tranche of flow.tranches) {
    if (depth !== null && tranche.depth !== depth) continue

    found.nodes.add(String(tranche.nodeId))
    for (const id of tranche.nodeIds) found.nodes.add(String(id))
    for (const id of tranche.edgeIds) found.edges.add(String(id))
  }

  return found
}

export function emphasisForInterest(nodes: GraphNode[], floor = interestEmphasisFloor): Emphasis {
  const found = emptyEmphasis()

  for (const node of nodes) {
    if (!node.seed && node.interest !== null && node.interest >= floor)
      found.nodes.add(String(node.id))
  }

  return found
}

export function mergeEmphasis(...parts: Emphasis[]): Set<string> {
  const merged = new Set<string>()

  for (const part of parts) {
    for (const id of part.nodes) merged.add(id)
    for (const id of part.edges) merged.add(id)
  }

  return merged
}

export function byInterest<T extends { node: GraphNode }>(rows: T[]): T[] {
  return [...rows].sort((a, b) => (b.node.interest ?? -1) - (a.node.interest ?? -1))
}

export function patternStateKey(stateId: number): 'new' | 'seen' | 'dismissed' | 'cased' {
  if (stateId === patternStates.seen) return 'seen'
  if (stateId === patternStates.dismissed) return 'dismissed'
  if (stateId === patternStates.cased) return 'cased'

  return 'new'
}
