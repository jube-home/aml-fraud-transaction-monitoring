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

export type GraphLayoutId = 1 | 2 | 3 | 4

export type GraphMeasureId = 0 | 1 | 2 | 3

export interface GraphAnchor {
  id: string | null
  x: number
  y: number
}

export interface GraphNodeCases {
  open: number
  closed: number
  confirmed: number
  latestCaseId: number | null
  latestOpenCaseId: number | null
}

export interface GraphNodeTag {
  tag: string | null
  distance: number
  within1: number
  within2: number
  within3: number
}

export interface GraphNode {
  id: number
  key: string | null
  value: string | null
  hub: boolean
  degree: number
  degreeMoney: number
  degreeIdentity: number
  hubNeighbourCount: number
  componentId: number | null
  componentSize: number | null
  communityId: number | null
  coreNumber: number | null
  pageRank: number | null
  clusteringCoefficient: number | null
  distanceToConfirmed: number | null
  confirmedWithin1: number | null
  confirmedWithin2: number | null
  confirmedWithin3: number | null
  propagationScore: number | null
  hubScore: number | null
  authorityScore: number | null
  distanceToNotConfirmed: number | null
  interest: number | null
  computedDate: string | null
  hubOverrideId: number
  firstSeen: string
  lastSeen: string
  depth: number
  viaNodeId: number | null
  seed: boolean
  cases: GraphNodeCases
  nearestTag: string | null
  nearestTagDistance: number | null
  tags: GraphNodeTag[]
}

export interface GraphEdge {
  id: number
  definitionGuid: string
  definitionName: string | null
  kindId: number
  fromNodeId: number
  toNodeId: number
  count: number
  amountSum: number
  windowCount: number
  windowAmount: number
  firstSeen: string
  lastSeen: string
  windowFirst: string | null
  windowLast: string | null
  weight: number
  specificity: number
  restricted: boolean
  archiveIds: number[]
}

export interface GraphCombo {
  id: string | null
  viaNodeId: number
  key: string | null
  count: number
  nodeIds: number[]
}

export interface GraphEdgeDefinition {
  guid: string
  name: string | null
  kindId: number
  restricted: boolean
  visible: boolean
}

export interface GraphNeighbourhood {
  seed: GraphNode | null
  nodes: GraphNode[]
  edges: GraphEdge[]
  combos: GraphCombo[]
  definitions: GraphEdgeDefinition[]
  hiddenHubs: number
  overCap: number
  from: string
  to: string
  hops: number
  builtDate: string | null
  noDefinitions: boolean
  relevance: boolean
}

export interface GraphNeighbourhoodRequest {
  registryGuid: string
  key: string
  value: string
  hops?: number
  fanOut?: number
  from?: string
  to?: string
  minimumAmount?: number
  edges?: string
  includeHubs?: boolean
  expanded?: string
  relevance?: boolean
  cap?: number
}

export interface GraphRenderRequest {
  query: GraphNeighbourhoodRequest
  flow?: GraphFlowRequest
  layoutId?: GraphLayoutId
  measureId?: GraphMeasureId
  anchors: GraphAnchor[]
  dark: boolean
  captions: string[]
}

export interface GraphRenderResult {
  svg: string | null
  positions: GraphAnchor[]
  graph: GraphNeighbourhood
  layoutId: number
}

export interface GraphExplodeRequest {
  registryGuid: string
  nodeId: number
  from?: string
  to?: string
  edges?: string
  limit?: number
}

export interface GraphExplodeLane {
  node: GraphNode
  count: number
  total: number
  first: string
  last: string
  definitions: string[]
  edgeIds: number[]
}

export interface GraphExplode {
  node: GraphNode | null
  lanes: GraphExplodeLane[]
  from: string
  to: string
}

export interface GraphEdgeTransaction {
  archiveId: number
  entityAnalysisModelInstanceEntryGuid: string
  occurredDate: string
  amount: number | null
}

export interface GraphEdgeTransactions {
  edge: GraphEdge | null
  transactions: GraphEdgeTransaction[]
  archiveIds: number[]
}

export interface GraphNodeDefinitionCount {
  guid: string
  name: string | null
  counterparts: number
  count: number
  total: number
}

export interface GraphNodeFacts {
  node: GraphNode
  definitions: GraphNodeDefinitionCount[]
  community: GraphCommunity | null
  patterns: GraphPattern[]
  sentences: string[]
}

export interface GraphCaptureRequest {
  caseId: number
  render: GraphRenderRequest
  note?: string
}

export interface GraphCaptureResult {
  svgCaseFileId: number
  definitionCaseFileId: number
  caseNoteId: number
  hash: string | null
  nodes: number
  edges: number
}

export interface GraphExploration {
  id: number
  name: string | null
  graphRegistryGuid: string
  address: string | null
  pinned: string | null
  createdDate?: string | null
  updatedDate?: string | null
}

export interface GraphRegistry {
  id: number
  guid: string
  name: string | null
  active: boolean
  locked?: boolean
  contextKindId: number
  defaultLayoutId: number
  defaultHops: number
  defaultWindowDays: number
  renderCap: number
  hubPolicyId: number
  version?: number
}

export interface GraphRegistryEdge {
  id: number
  graphRegistryId: number
  guid: string
  name: string | null
  active: boolean
  kindId: number
  fromKey: string | null
  toKey: string | null
  fromNodeKey: string | null
  toNodeKey: string | null
  amountKey: string | null
  restricted: boolean
}

export interface GraphRegistryEdgeKey {
  key: string | null
  label: string | null
  models: string[]
  unflaggedModels: string[]
}

export interface GraphPath {
  hops: number
  weight: number
  nodeIds: number[]
  edgeIds: number[]
  sentence: string | null
}

export interface GraphPaths {
  from: GraphNode | null
  to: GraphNode | null
  paths: GraphPath[]
  nodes: GraphNode[]
  edges: GraphEdge[]
  sentences: string[]
}

export interface GraphPathsRequest {
  registryGuid: string
  fromNodeId: number
  toNodeId: number
  maxHops?: number
  k?: number
  includeHubs?: boolean
  edges?: string
  from?: string
  to?: string
}

export interface GraphFlowRequest {
  registryGuid: string
  nodeId: number
  archiveId?: number
  at?: string
  amount?: number
  forward?: boolean
  hopWindowHours?: number
  tolerance?: number
  depth?: number
  fanOut?: number
  edges?: string
  from?: string
  to?: string
}

export interface GraphFlowTranche {
  depth: number
  at: string
  amount: number
  nodeId: number
  nodeIds: number[]
  edgeIds: number[]
  archiveIds: number[]
}

export interface GraphFlow {
  forward: boolean
  graph: GraphNeighbourhood
  tranches: GraphFlowTranche[]
  passThroughNodeIds: number[]
  sentences: string[]
}

export interface GraphPatternMember {
  node: GraphNode
  role: string | null
}

export interface GraphPattern {
  id: number
  kindId: number
  kind: string | null
  anchor: GraphNode | null
  score: number
  windowFrom: string
  windowTo: string
  parameters: Record<string, number>
  archiveIds: number[]
  memberCount: number
  members: GraphPatternMember[]
  stateId: number
  caseId: number | null
  firstDetectedDate: string
  lastDetectedDate: string
  decidedUser: string | null
  decidedDate: string | null
  sentence: string | null
}

export interface GraphPatternListRequest {
  stateId?: number
  kindId?: number
  limit?: number
}

export interface GraphPatternDecideRequest {
  stateId: number
  caseId?: number
}

export interface GraphCommunity {
  id: number
  size: number
  density: number
  confirmedCount: number
  closedCount: number
  confirmedRate: number | null
  tenantRateRatio: number | null
  label: string | null
  computedDate: string
  members: GraphNode[]
  sentence: string | null
}

export interface GraphHubOverrideRequest {
  nodeId: number
  overrideId: number
}

export interface GraphWindow {
  from: string
  to: string
}
