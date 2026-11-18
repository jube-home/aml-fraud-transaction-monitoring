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

import { describe, expect, it } from 'vitest'
import { maximumExpanded } from '../address'
import {
  addsOnExpand,
  byInterest,
  caseEdgeKindId,
  caseIdOf,
  collapse,
  edgeKindKey,
  isCaseNode,
  nearestTagOf,
  tagChips,
  emphasisForCommunity,
  emphasisForInterest,
  emphasisForPaths,
  emphasisForPattern,
  emphasisForTranche,
  followMoney,
  leaveFlow,
  mergeEmphasis,
  patternStateKey,
  setFlowDirection,
  setFlowTolerance,
  setFlowWindowHours,
  setPathsTarget,
  setView,
  comboNodeId,
  comboOf,
  daysOfWindow,
  defaultGraphState,
  edgeById,
  edgesForArchive,
  expand,
  hiddenAtCursor,
  isCombo,
  nearestDays,
  nodeById,
  select,
  selectEdge,
  setCursor,
  setExplode,
  setHops,
  setLayout,
  setMinimumAmount,
  setWindow,
  toggleEdge,
  windowFromDays,
  legendKeys,
  seedKeyChoices,
} from '../state'
import type {
  GraphCombo,
  GraphCommunity,
  GraphEdge,
  GraphEdgeDefinition,
  GraphFlow,
  GraphNode,
  GraphPaths,
  GraphPattern,
} from '../types'

function node(id: number, seed = false): GraphNode {
  return {
    id,
    key: 'AccountId',
    value: `ACC-${id}`,
    hub: false,
    degree: 5,
    degreeMoney: 2,
    degreeIdentity: 3,
    hubNeighbourCount: 0,
    componentId: null,
    componentSize: null,
    communityId: null,
    coreNumber: null,
    pageRank: null,
    clusteringCoefficient: null,
    distanceToConfirmed: null,
    confirmedWithin1: null,
    confirmedWithin2: null,
    confirmedWithin3: null,
    propagationScore: null,
    hubScore: null,
    authorityScore: null,
    distanceToNotConfirmed: null,
    interest: null,
    computedDate: null,
    hubOverrideId: 0,
    firstSeen: '2026-10-01T00:00:00Z',
    lastSeen: '2026-10-20T00:00:00Z',
    depth: seed ? 0 : 1,
    viaNodeId: seed ? null : 1,
    seed,
    cases: { open: 0, closed: 0, confirmed: 0, latestCaseId: null, latestOpenCaseId: null },
    nearestTag: null,
    nearestTagDistance: null,
    tags: [],
  }
}

function edge(
  id: number,
  from: number,
  to: number,
  first: string,
  archiveIds: number[],
): GraphEdge {
  return {
    id,
    definitionGuid: 'def',
    definitionName: 'Pays',
    kindId: 2,
    fromNodeId: from,
    toNodeId: to,
    count: 1,
    amountSum: 10,
    windowCount: 1,
    windowAmount: 10,
    firstSeen: '2026-09-01T00:00:00Z',
    lastSeen: first,
    windowFirst: first,
    windowLast: first,
    weight: 1,
    specificity: 1,
    restricted: false,
    archiveIds,
  }
}

const definitions: GraphEdgeDefinition[] = [
  {
    guid: 'a',
    name: 'A',
    kindId: 1,
    restricted: false,
    visible: true,
    iconName: null,
    fromNodeKey: null,
    toNodeKey: null,
    fromNodeIcon: null,
    toNodeIcon: null,
  },
  {
    guid: 'b',
    name: 'B',
    kindId: 2,
    restricted: false,
    visible: true,
    iconName: null,
    fromNodeKey: null,
    toNodeKey: null,
    fromNodeIcon: null,
    toNodeIcon: null,
  },
  {
    guid: 'c',
    name: 'C',
    kindId: 1,
    restricted: true,
    visible: false,
    iconName: null,
    fromNodeKey: null,
    toNodeKey: null,
    fromNodeIcon: null,
    toNodeIcon: null,
  },
]

describe('graph state reducers', () => {
  it('expands a node once, keeps order and caps the list', () => {
    let state = expand(defaultGraphState, '5')

    state = expand(state, '5')
    expect(state.address.expanded).toEqual(['5'])

    for (let index = 0; index < maximumExpanded + 5; index += 1) state = expand(state, `n${index}`)

    expect(state.address.expanded).toHaveLength(maximumExpanded)
    expect(state.address.expanded.includes('5')).toBe(false)
    expect(collapse(state, 'n24').address.expanded.includes('n24')).toBe(false)
    expect(expand(state, '')).toBe(state)
  })

  it('selecting a node clears the selected edge and vice versa is kept apart', () => {
    const withEdge = selectEdge(defaultGraphState, '9')

    expect(withEdge.selectedEdge).toBe('9')
    expect(select(withEdge, '3')).toMatchObject({ selectedEdge: null, address: { selected: '3' } })
    expect(select(withEdge, null).address.selected).toBeNull()
  })

  it('bounds hops to one to three and switches the view to hops', () => {
    expect(
      setHops(
        { ...defaultGraphState, address: { ...defaultGraphState.address, view: 'relevance' } },
        7,
      ).address,
    ).toMatchObject({
      hops: 3,
      view: 'hops',
    })
    expect(setHops(defaultGraphState, 0).address.hops).toBe(1)
    expect(setLayout(defaultGraphState, 4).address.layout).toBe(4)
    expect(setExplode(defaultGraphState, true).address.explode).toBe(true)
  })

  it('toggles edge kinds against the visible definitions and collapses back to all', () => {
    const one = toggleEdge(defaultGraphState, 'a', definitions)

    expect(one.address.edges).toEqual(['b'])

    const none = toggleEdge(one, 'b', definitions)

    expect(none.address.edges).toEqual([])

    const back = toggleEdge(toggleEdge(one, 'a', definitions), 'c', definitions)

    expect(back.address.edges).toEqual([])
  })

  it('bounds the minimum amount and clears the cursor when the window moves', () => {
    expect(setMinimumAmount(defaultGraphState, -4).minimumAmount).toBe(0)
    expect(setMinimumAmount(defaultGraphState, Number.NaN).minimumAmount).toBe(0)
    expect(setMinimumAmount(defaultGraphState, 1e12).minimumAmount).toBe(1_000_000_000)

    const cursor = setCursor(defaultGraphState, 1_700_000_000)
    const moved = setWindow(cursor, { from: '2026-01-01T00:00:00Z', to: '2026-02-01T00:00:00Z' })

    expect(moved.address.cursor).toBeNull()
    expect(moved.window?.from).toBe('2026-01-01T00:00:00Z')
  })
})

describe('time cursor hiding', () => {
  const graph = {
    seed: node(1, true),
    nodes: [node(1, true), node(2), node(3), node(4)],
    edges: [
      edge(11, 1, 2, '2026-10-02T00:00:00Z', [201]),
      edge(12, 2, 3, '2026-10-10T00:00:00Z', [202, 203]),
      edge(13, 1, 4, '2026-10-20T00:00:00Z', [204]),
    ],
  }

  it('hides nothing without a cursor', () => {
    expect(hiddenAtCursor(graph, null).size).toBe(0)
  })

  it('hides edges first seen after the cursor and the nodes they orphan, never the seed', () => {
    const cursor = Math.floor(Date.parse('2026-10-05T00:00:00Z') / 1000)
    const hidden = hiddenAtCursor(graph, cursor)

    expect([...hidden].sort()).toEqual(['12', '13', '3', '4'])
    expect(hidden.has('1')).toBe(false)
    expect(hidden.has('2')).toBe(false)
  })

  it('hides every edge and every non-seed node before the first event', () => {
    const hidden = hiddenAtCursor(graph, 1)

    expect([...hidden].sort()).toEqual(['11', '12', '13', '2', '3', '4'])
  })

  it('finds the edges created by a journal transaction', () => {
    expect([...edgesForArchive(graph.edges, 203)]).toEqual(['12'])
    expect(edgesForArchive(graph.edges, null).size).toBe(0)
    expect(edgeById(graph.edges, '13')?.archiveIds).toEqual([204])
    expect(nodeById(graph.nodes, '3')?.value).toBe('ACC-3')
    expect(nodeById(graph.nodes, 'c:1:AccountId')).toBeNull()
  })

  it('counts what an expansion adds from the degree less what is already drawn', () => {
    expect(addsOnExpand(node(1, true), graph)).toBe(3)
    expect(addsOnExpand({ ...node(9), degree: 1 }, graph)).toBe(1)
  })
})

describe('combos', () => {
  const combos: GraphCombo[] = [
    { id: '1:AccountId', viaNodeId: 1, key: 'AccountId', count: 40, nodeIds: [] },
  ]

  it('recognises the combo prefix and finds the combo behind a picked node id', () => {
    expect(isCombo('c:1:AccountId')).toBe(true)
    expect(isCombo('12')).toBe(false)
    expect(isCombo(null)).toBe(false)
    expect(comboOf('c:1:AccountId', combos)?.count).toBe(40)
    expect(comboOf('c:missing', combos)).toBeNull()
    expect(comboOf('12', combos)).toBeNull()
    expect(comboNodeId(combos[0]!)).toBe('c:1:AccountId')
  })
})

describe('windows', () => {
  it('builds a window of whole minutes ending now and measures it back in days', () => {
    const now = new Date('2026-10-20T10:30:45Z')
    const window = windowFromDays(30, now)

    expect(window.to).toBe('2026-10-20T10:30:00.000Z')
    expect(window.from).toBe('2026-09-20T10:30:00.000Z')
    expect(daysOfWindow(window)).toBe(30)
    expect(nearestDays(25, [7, 30, 90, 365])).toBe(30)
    expect(nearestDays(200, [7, 30, 90, 365])).toBe(90)
  })
})

describe('flow and paths reducers', () => {
  it('enters flow only with a selected node and remembers the seed, direction and options', () => {
    expect(setView(defaultGraphState, 'flow')).toBe(defaultGraphState)

    const selected = select(defaultGraphState, '5')
    const flow = setView(selected, 'flow')

    expect(flow.address).toMatchObject({ view: 'flow', flow: { seed: '5', direction: 'forward' } })
    expect(setFlowDirection(flow, 'backward').address.flow?.direction).toBe('backward')
    expect(setFlowWindowHours(flow, 72).address.flowWindowHours).toBeNull()
    expect(setFlowWindowHours(flow, 9000).address.flowWindowHours).toBe(720)
    expect(setFlowTolerance(flow, 0.2).address.flowTolerance).toBeNull()
    expect(setFlowTolerance(flow, 1.5).address.flowTolerance).toBe(0.9)
    expect(leaveFlow(flow).address).toMatchObject({ view: 'hops', flow: null })
  })

  it('follows the money from a node with the journal transaction as the anchor', () => {
    const followed = followMoney(defaultGraphState, '9', 4412)

    expect(followed.address).toMatchObject({
      view: 'flow',
      selected: '9',
      flow: { seed: '9', direction: 'forward' },
      flowArchiveId: 4412,
    })
  })

  it('sets a paths target other than the selected node and clears it with the selection', () => {
    const selected = select(defaultGraphState, '5')

    expect(setPathsTarget(selected, '5')).toBe(selected)
    expect(setPathsTarget(selected, 'c:1:AccountId')).toBe(selected)

    const targeted = setPathsTarget(selected, '7')

    expect(targeted.address.pathsTo).toBe('7')
    expect(select(targeted, '8').address.pathsTo).toBe('7')
    expect(select(targeted, null).address.pathsTo).toBeNull()
  })
})

describe('emphasis', () => {
  const paths: GraphPaths = {
    from: null,
    to: null,
    nodes: [],
    edges: [],
    sentences: [],
    paths: [
      { hops: 2, weight: 1, nodeIds: [1, 5, 2], edgeIds: [11, 12], sentence: null },
      { hops: 1, weight: 2, nodeIds: [1, 4], edgeIds: [13], sentence: null },
    ],
  }

  it('emphasises every path or one path', () => {
    expect([...mergeEmphasis(emphasisForPaths(paths, null))].sort()).toEqual(
      ['1', '11', '12', '13', '2', '4', '5'].sort(),
    )
    expect([...mergeEmphasis(emphasisForPaths(paths, 1))].sort()).toEqual(['1', '13', '4'])
    expect(mergeEmphasis(emphasisForPaths(undefined, null)).size).toBe(0)
  })

  it('emphasises a pattern through its members and the edges carrying its archive ids', () => {
    const pattern: GraphPattern = {
      id: 3,
      kindId: 4,
      kind: 'Scatter-gather',
      anchor: node(1, true),
      score: 0.9,
      windowFrom: '2026-10-01T00:00:00Z',
      windowTo: '2026-10-02T00:00:00Z',
      parameters: {},
      archiveIds: [203],
      memberCount: 2,
      members: [{ node: node(3), role: 'intermediary' }],
      stateId: 1,
      caseId: null,
      firstDetectedDate: '2026-10-02T00:00:00Z',
      lastDetectedDate: '2026-10-02T00:00:00Z',
      decidedUser: null,
      decidedDate: null,
      sentence: null,
    }
    const edges = [
      edge(12, 2, 3, '2026-10-10T00:00:00Z', [202, 203]),
      edge(13, 1, 4, '2026-10-20T00:00:00Z', [204]),
    ]

    expect([...mergeEmphasis(emphasisForPattern(pattern, edges))].sort()).toEqual(['1', '12', '3'])
    expect(patternStateKey(pattern.stateId)).toBe('new')
    expect(patternStateKey(4)).toBe('cased')
  })

  it('emphasises community members, a tranche and the nodes above the interest floor', () => {
    const community: GraphCommunity = {
      id: 812,
      size: 2,
      density: 0.5,
      confirmedCount: 1,
      closedCount: 2,
      confirmedRate: 0.5,
      tenantRateRatio: 2.1,
      label: null,
      computedDate: '2026-10-20T00:00:00Z',
      members: [node(2), node(3)],
      sentence: null,
    }

    expect([...mergeEmphasis(emphasisForCommunity(community))].sort()).toEqual(['2', '3'])

    const flow: GraphFlow = {
      forward: true,
      graph: {
        seed: null,
        nodes: [],
        edges: [],
        combos: [],
        definitions: [],
        hiddenHubs: 0,
        overCap: 0,
        from: '',
        to: '',
        hops: 0,
        builtDate: null,
        noDefinitions: false,
        relevance: false,
      },
      tranches: [
        {
          depth: 1,
          at: '2026-10-02T00:00:00Z',
          amount: 9500,
          nodeId: 2,
          nodeIds: [2],
          edgeIds: [11],
          archiveIds: [201],
        },
        {
          depth: 2,
          at: '2026-10-02T01:00:00Z',
          amount: 9400,
          nodeId: 3,
          nodeIds: [3],
          edgeIds: [12],
          archiveIds: [202],
        },
      ],
      passThroughNodeIds: [2],
      sentences: [],
    }

    expect([...mergeEmphasis(emphasisForTranche(flow, 2))].sort()).toEqual(['12', '3'])
    expect([...mergeEmphasis(emphasisForTranche(flow, null))].sort()).toEqual([
      '11',
      '12',
      '2',
      '3',
    ])

    const ranked = [
      { ...node(1, true), interest: 1 },
      { ...node(2), interest: 0.7 },
      { ...node(3), interest: 0.2 },
      node(4),
    ]

    expect([...mergeEmphasis(emphasisForInterest(ranked))]).toEqual(['2'])
    expect(
      byInterest(ranked.map((current) => ({ node: current }))).map((row) => row.node.id),
    ).toEqual([1, 2, 3, 4])
  })
})

describe('case nodes and tags', () => {
  it('recognises a Case node and reads its case id from the value', () => {
    expect(isCaseNode({ key: 'Case' })).toBe(true)
    expect(isCaseNode({ key: 'AccountId' })).toBe(false)
    expect(isCaseNode(null)).toBe(false)
    expect(caseIdOf({ key: 'Case', value: '12' })).toBe(12)
    expect(caseIdOf({ key: 'Case', value: 'x' })).toBeNull()
    expect(caseIdOf({ key: 'AccountId', value: '12' })).toBeNull()
  })

  it('labels every edge kind, the synthetic case edge included', () => {
    expect(edgeKindKey(1)).toBe('identity')
    expect(edgeKindKey(2)).toBe('money')
    expect(edgeKindKey(caseEdgeKindId)).toBe('case')
    expect(edgeKindKey(9)).toBeNull()
  })

  it('orders the tag chips by distance and falls back to them for the nearest tag', () => {
    const tagged = {
      ...node(1),
      tags: [
        { tag: 'Victim', distance: 3, within1: 0, within2: 0, within3: 1 },
        { tag: null, distance: 0, within1: 1, within2: 1, within3: 1 },
        { tag: 'Mule', distance: 1, within1: 1, within2: 1, within3: 1 },
      ],
    }

    expect(tagChips(tagged).map((item) => item.tag)).toEqual(['Mule', 'Victim'])
    expect(nearestTagOf(tagged)).toEqual({ tag: 'Mule', distance: 1 })
    expect(nearestTagOf({ ...tagged, nearestTag: 'Victim', nearestTagDistance: 3 })).toEqual({
      tag: 'Victim',
      distance: 3,
    })
    expect(nearestTagOf(node(2))).toBeNull()
    expect(tagChips(null)).toEqual([])
  })
})

describe('the legend against the shapes the server draws', () => {
  it('puts the seed key first and the rest in ordinal order, whatever order they arrived in', () => {
    expect(legendKeys(['DeviceId', 'AccountId', 'Case', 'DeviceId'], 'DeviceId')).toEqual([
      { key: 'DeviceId', seed: true },
      { key: 'AccountId', seed: false },
      { key: 'Case', seed: false },
    ])
  })

  it('takes the first key in ordinal order as the seed when the graph names none', () => {
    expect(legendKeys(['b', 'B', 'a'], null)).toEqual([
      { key: 'B', seed: true },
      { key: 'a', seed: false },
      { key: 'b', seed: false },
    ])
  })

  it('marks no key as the seed when the seed key is not among the nodes', () => {
    expect(legendKeys(['AccountId'], 'Elsewhere')).toEqual([{ key: 'AccountId', seed: false }])
  })
})

describe('the keys a graph can be seeded from', () => {
  const edge = (kindId: number, fromKey: string, toKey: string) => ({
    kindId,
    fromKey,
    toKey,
    fromNodeKey: null,
    toNodeKey: null,
  })

  it('lists each node key once, sorted', () => {
    expect(
      seedKeyChoices([edge(1, 'DeviceId', 'AccountId'), edge(2, 'AccountId', 'AccountId')]),
    ).toEqual(['AccountId', 'DeviceId'])
  })

  it('prefers the node key an edge end is stored under', () => {
    expect(
      seedKeyChoices([
        { ...edge(2, 'DebtorId', 'CreditorId'), fromNodeKey: 'AccountId', toNodeKey: 'AccountId' },
      ]),
    ).toEqual(['AccountId'])
  })

  it('leaves out the from side of the case edge, which is never a node key', () => {
    expect(seedKeyChoices([edge(1, 'DeviceId', 'AccountId'), edge(5, 'CaseKey', 'Case')])).toEqual([
      'AccountId',
      'Case',
      'DeviceId',
    ])
  })
})
