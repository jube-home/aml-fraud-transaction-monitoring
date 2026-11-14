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

import {useApi} from '../api/useApi'
import type {
  GraphCaptureRequest,
  GraphCaptureResult,
  GraphCommunity,
  GraphEdgeTransactions,
  GraphExplode,
  GraphExplodeRequest,
  GraphExploration,
  GraphFlow,
  GraphFlowRequest,
  GraphHubOverrideRequest,
  GraphNeighbourhood,
  GraphNeighbourhoodRequest,
  GraphNode,
  GraphNodeFacts,
  GraphPaths,
  GraphPathsRequest,
  GraphPattern,
  GraphPatternDecideRequest,
  GraphPatternListRequest,
  GraphRegistry,
  GraphRegistryEdge,
  GraphRegistryEdgeKey,
  GraphRenderRequest,
  GraphRenderResult,
} from './types'

export function neighbourhoodBody(request: GraphNeighbourhoodRequest): GraphNeighbourhoodRequest {
  return {
    registryGuid: request.registryGuid,
    key: request.key,
    value: request.value,
    hops: request.hops,
    fanOut: request.fanOut,
    from: request.from,
    to: request.to,
    minimumAmount: request.minimumAmount || undefined,
    edges: request.edges || undefined,
    includeHubs: request.includeHubs || undefined,
    expanded: request.expanded || undefined,
    relevance: request.relevance || undefined,
    cap: request.cap,
  }
}

export const graphApi = {
  neighbourhood: (request: GraphNeighbourhoodRequest, signal?: AbortSignal) =>
    useApi<GraphNeighbourhood>('/api/Graph/Neighbourhood', {
      method: 'POST',
      body: neighbourhoodBody(request),
      signal,
    }),

  explode: (request: GraphExplodeRequest, signal?: AbortSignal) =>
    useApi<GraphExplode>('/api/Graph/Explode', {
      method: 'POST',
      body: {
        registryGuid: request.registryGuid,
        nodeId: request.nodeId,
        from: request.from,
        to: request.to,
        edges: request.edges || undefined,
        limit: request.limit,
      },
      signal,
    }),

  edgeTransactions: (edgeId: number, from?: string, to?: string, limit?: number) =>
    useApi<GraphEdgeTransactions>(`/api/Graph/Edge/${edgeId}/Transactions`, {
      query: {from, to, limit},
    }),

  node: (lookup: { nodeId: number } | { key: string; value: string }, signal?: AbortSignal) =>
    useApi<GraphNodeFacts>('/api/Graph/Node', {
      method: 'POST',
      body: 'nodeId' in lookup ? {nodeId: lookup.nodeId} : lookup,
      signal,
    }),

  render: (request: GraphRenderRequest, signal?: AbortSignal) =>
    useApi<GraphRenderResult>('/api/Graph/Render', {method: 'POST', body: request, signal}),

  timeStripSvg: (request: GraphNeighbourhoodRequest, bins: number, dark: boolean) =>
    useApi<string>(`/api/Graph/TimeStrip/Svg?bins=${bins}&dark=${dark}`, {
      method: 'POST',
      body: neighbourhoodBody(request),
    }),

  capture: (request: GraphCaptureRequest) =>
    useApi<GraphCaptureResult>('/api/Graph/Capture', {method: 'POST', body: request}),

  paths: (request: GraphPathsRequest, signal?: AbortSignal) =>
    useApi<GraphPaths>('/api/Graph/Paths', {
      method: 'POST',
      body: {
        registryGuid: request.registryGuid,
        fromNodeId: request.fromNodeId,
        toNodeId: request.toNodeId,
        maxHops: request.maxHops,
        k: request.k,
        includeHubs: request.includeHubs || undefined,
        edges: request.edges || undefined,
        from: request.from,
        to: request.to,
      },
      signal,
    }),

  flow: (request: GraphFlowRequest, signal?: AbortSignal) =>
    useApi<GraphFlow>('/api/Graph/Flow', {
      method: 'POST',
      body: {
        registryGuid: request.registryGuid,
        nodeId: request.nodeId,
        archiveId: request.archiveId,
        at: request.at,
        amount: request.amount,
        forward: request.forward,
        hopWindowHours: request.hopWindowHours,
        tolerance: request.tolerance,
        depth: request.depth,
        fanOut: request.fanOut,
        edges: request.edges || undefined,
        from: request.from,
        to: request.to,
      },
      signal,
    }),

  patterns: (request: GraphPatternListRequest = {}) =>
    useApi<GraphPattern[]>('/api/Graph/Patterns/Search', {
      method: 'POST',
      body: {stateId: request.stateId, kindId: request.kindId, limit: request.limit},
    }),

  pattern: (id: number) => useApi<GraphPattern>(`/api/Graph/Patterns/${id}`),

  patternDecide: (id: number, request: GraphPatternDecideRequest) =>
    useApi<GraphPattern>(`/api/Graph/Patterns/${id}/Decide`, {
      method: 'POST',
      body: {id, stateId: request.stateId, caseId: request.caseId},
    }),

  communities: (limit?: number) =>
    useApi<GraphCommunity[]>('/api/Graph/Communities', {query: {limit}}),

  community: (id: number) => useApi<GraphCommunity>(`/api/Graph/Communities/${id}`),

  hubs: () => useApi<GraphNode[]>('/api/Graph/Hubs'),

  hubOverride: (request: GraphHubOverrideRequest) =>
    useApi<GraphNode>('/api/Graph/Hubs/Override', {method: 'POST', body: request}),

  explorations: () => useApi<GraphExploration[]>('/api/Graph/Explorations'),

  saveExploration: (exploration: GraphExploration) =>
    useApi<GraphExploration>('/api/Graph/Explorations', {method: 'PUT', body: exploration}),

  deleteExploration: (id: number) =>
    useApi<void>(`/api/Graph/Explorations/${id}`, {method: 'DELETE'}),

  directory: () => useApi<GraphRegistry[]>('/api/GraphRegistry/GetByShowInDirectory'),

  registry: (guid: string) =>
    useApi<GraphRegistry>(`/api/GraphRegistry/GetByGuidActiveOnly/${encodeURIComponent(guid)}`),

  registries: () => useApi<GraphRegistry[]>('/api/GraphRegistry'),

  edges: (registryId: number) =>
    useApi<GraphRegistryEdge[]>(`/api/GraphRegistryEdge/ByGraphRegistryId/${registryId}`),

  edgeKeys: () => useApi<GraphRegistryEdgeKey[]>('/api/GraphRegistryEdge/Keys'),
}
