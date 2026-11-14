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

import AxeBuilder from '@axe-core/playwright'
import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {setLanguage, setTheme, settleAnimations} from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [1, 49],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const registry = {
  id: 3,
  guid: 'graph-guid',
  name: 'Demonstration payments network',
  active: true,
  contextKindId: 1,
  defaultLayoutId: 1,
  defaultHops: 2,
  defaultWindowDays: 30,
  renderCap: 150,
  hubPolicyId: 1,
}

const directory = [
  registry,
  {...registry, id: 4, guid: 'other-guid', name: 'Device sharing', defaultLayoutId: 2},
  {...registry, id: 5, guid: 'case-only', name: 'Case only network', contextKindId: 2},
]

const edges = [
  {
    id: 31,
    graphRegistryId: 3,
    guid: 'def-device',
    name: 'Shares device',
    active: true,
    kindId: 1,
    fromKey: 'AccountId',
    toKey: 'DeviceId',
    fromNodeKey: null,
    toNodeKey: null,
    amountKey: null,
    restricted: false,
  },
  {
    id: 32,
    graphRegistryId: 3,
    guid: 'def-pays',
    name: 'Pays',
    active: true,
    kindId: 2,
    fromKey: 'AccountId',
    toKey: 'BeneficiaryAccountId',
    fromNodeKey: null,
    toNodeKey: 'AccountId',
    amountKey: 'Amount',
    restricted: false,
  },
]

const node = (id: number, key: string, value: string, seed = false, extra = {}) => ({
  hubScore: null,
  authorityScore: null,
  distanceToNotConfirmed: null,
  interest: null,
  computedDate: null,
  hubOverrideId: 0,
  id,
  key,
  value,
  hub: false,
  degree: 4,
  degreeMoney: 1,
  degreeIdentity: 3,
  hubNeighbourCount: 0,
  componentId: 1,
  componentSize: 3,
  communityId: 812,
  coreNumber: 1,
  pageRank: 0.1,
  clusteringCoefficient: 0,
  distanceToConfirmed: 1,
  confirmedWithin1: 1,
  confirmedWithin2: 1,
  confirmedWithin3: 1,
  propagationScore: 0.1,
  firstSeen: '2026-10-01T00:00:00Z',
  lastSeen: '2026-10-20T00:00:00Z',
  depth: seed ? 0 : 1,
  viaNodeId: seed ? null : 1,
  seed,
  cases: {open: 0, closed: 0, confirmed: 0, latestCaseId: null, latestOpenCaseId: null},
  nearestTag: null,
  nearestTagDistance: null,
  tags: [],
  ...extra,
})

const caseEdgeGuid = '00000000-0000-4000-8000-00000000ca5e'

const taggedNode = node(5, 'DeviceId', 'DEV-8831A', false, {
  degree: 4,
  nearestTag: 'Mule',
  nearestTagDistance: 2,
  tags: [{tag: 'Mule', distance: 2, within1: 0, within2: 1, within3: 1}],
})

const caseNode = node(7, 'Case', '12', false, {
  degree: 1,
  nearestTag: 'Mule',
  nearestTagDistance: 0,
  tags: [{tag: 'Mule', distance: 0, within1: 1, within2: 1, within3: 1}],
})

const edge = (id: number, from: number, to: number, archiveIds: number[]) => ({
  id,
  definitionGuid: 'def-device',
  definitionName: 'Shares device',
  kindId: 1,
  fromNodeId: from,
  toNodeId: to,
  count: archiveIds.length,
  amountSum: 0,
  windowCount: archiveIds.length,
  windowAmount: 0,
  firstSeen: '2026-10-02T00:00:00Z',
  lastSeen: '2026-10-19T00:00:00Z',
  windowFirst: '2026-10-02T00:00:00Z',
  windowLast: '2026-10-19T00:00:00Z',
  weight: 0.5,
  specificity: 0.6,
  restricted: false,
  archiveIds,
})

const svg = (expanded: boolean, withCase = false) =>
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 120" width="300" height="120" role="img" aria-label="Network">' +
  '<g data-edge="11" data-from="1" data-to="5" data-first="1759363200" data-last="1760832000" data-kind="undirected"><path class="ep" d="M40 60 L150 60" stroke="#000"/></g>' +
  '<g data-edge="12" data-from="5" data-to="2" data-first="1759363200" data-last="1760832000" data-kind="undirected"><path class="ep" d="M150 60 L260 60" stroke="#000"/></g>' +
  (expanded
    ? '<g data-edge="13" data-from="5" data-to="3" data-first="1760400000" data-last="1760832000" data-kind="undirected"><path class="ep" d="M150 60 L150 110" stroke="#000"/></g>'
    : '') +
  '<g data-node="1" data-kind="circle"><circle class="nb" cx="40" cy="60" r="12" fill="#ccc"/></g>' +
  '<g data-node="5" data-kind="square"><rect class="nb" x="138" y="48" width="24" height="24" fill="#ccc"/></g>' +
  '<g data-node="2" data-kind="circle"><circle class="nb" cx="260" cy="60" r="12" fill="#ccc"/></g>' +
  (expanded
    ? '<g data-node="3" data-kind="circle"><circle class="nb" cx="150" cy="110" r="12" fill="#ccc"/></g>'
    : '') +
  (withCase
    ? '<g data-edge="14" data-from="1" data-to="7" data-first="1759363200" data-last="1760832000" data-kind="undirected"><path class="ep" d="M40 60 L40 110" stroke="#000"/></g>' +
    '<g data-node="7" data-kind="circle"><circle class="nb" cx="40" cy="110" r="12" fill="#ccc"/></g>'
    : '') +
  '</svg>'

const render = (expanded: boolean, relevance = false, withCase = false) => ({
  svg: svg(expanded, withCase),
  positions: [
    {id: '1', x: 40, y: 60},
    {id: '5', x: 150, y: 60},
    {id: '2', x: 260, y: 60},
    ...(expanded ? [{id: '3', x: 150, y: 110}] : []),
    ...(withCase ? [{id: '7', x: 40, y: 110}] : []),
  ],
  layoutId: 1,
  graph: {
    seed: node(1, 'AccountId', 'ACC-441', true),
    nodes: [
      node(1, 'AccountId', 'ACC-441', true, {interest: relevance ? 1 : null}),
      node(5, 'DeviceId', 'DEV-8831A', false, {degree: 4, interest: relevance ? 0.8 : null}),
      node(2, 'AccountId', 'ACC-778', false, {interest: relevance ? 0.2 : null}),
      ...(expanded ? [node(3, 'AccountId', 'ACC-102')] : []),
      ...(withCase ? [caseNode] : []),
    ],
    edges: [
      edge(11, 1, 5, [1101, 1102]),
      edge(12, 5, 2, [1201]),
      ...(expanded ? [edge(13, 5, 3, [1301])] : []),
      ...(withCase
        ? [
          {
            ...edge(14, 1, 7, []),
            definitionGuid: caseEdgeGuid,
            definitionName: 'In case',
            kindId: 5,
          },
        ]
        : []),
    ],
    combos: [],
    definitions: [
      {guid: 'def-device', name: 'Shares device', kindId: 1, restricted: false, visible: true},
      {guid: 'def-pays', name: 'Pays', kindId: 2, restricted: false, visible: true},
      ...(withCase
        ? [{guid: caseEdgeGuid, name: 'In case', kindId: 5, restricted: false, visible: true}]
        : []),
    ],
    hiddenHubs: 1,
    overCap: 0,
    from: '2026-09-20T00:00:00Z',
    to: '2026-10-20T00:00:00Z',
    hops: 2,
    builtDate: new Date().toISOString(),
    noDefinitions: false,
    relevance,
  },
})

const timeStrip =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 40" width="300" height="40" role="img" aria-label="Time strip">' +
  '<rect data-bin="0" data-from="1758326400" data-to="1759622400" x="0" y="10" width="140" height="30" fill="#005F7F"/>' +
  '<rect data-bin="1" data-from="1759622400" data-to="1760918400" x="150" y="20" width="140" height="20" fill="#005F7F"/>' +
  '</svg>'

const pattern = {
  id: 3,
  kindId: 4,
  kind: 'Scatter-gather',
  anchor: node(1, 'AccountId', 'ACC-441', true),
  score: 0.91,
  windowFrom: '2026-10-01T00:00:00Z',
  windowTo: '2026-10-02T00:00:00Z',
  parameters: {intermediaries: 2},
  archiveIds: [1101],
  memberCount: 3,
  members: [
    {node: node(1, 'AccountId', 'ACC-441', true), role: 'source'},
    {node: node(5, 'DeviceId', 'DEV-8831A'), role: 'intermediary'},
  ],
  stateId: 1,
  caseId: null,
  firstDetectedDate: '2026-10-02T00:00:00Z',
  lastDetectedDate: '2026-10-02T00:00:00Z',
  decidedUser: null,
  decidedDate: null,
  sentence: 'Three accounts fanned out and back within six hours.',
}

const community = {
  id: 812,
  size: 37,
  density: 0.2,
  confirmedCount: 3,
  closedCount: 10,
  confirmedRate: 0.3,
  tenantRateRatio: 2.1,
  label: 'Community 812',
  computedDate: '2026-10-20T00:00:00Z',
  members: [node(2, 'AccountId', 'ACC-778'), node(5, 'DeviceId', 'DEV-8831A')],
  sentence: 'Confirmed at 2.1 times the tenant rate.',
}

const hubNode = node(9, 'MacAddress', 'AA:BB:CC:DD:EE:FF', false, {
  hub: true,
  degree: 480,
  hubOverrideId: 1,
})

const pathsData = {
  from: node(5, 'DeviceId', 'DEV-8831A'),
  to: node(2, 'AccountId', 'ACC-778'),
  paths: [
    {
      hops: 1,
      weight: 0.5,
      nodeIds: [5, 2],
      edgeIds: [12],
      sentence: 'DEV-8831A shares a device with ACC-778.',
    },
  ],
  nodes: [],
  edges: [],
  sentences: ['One path of one hop.'],
}

const flowData = (forward: boolean) => ({
  forward,
  graph: render(false).graph,
  tranches: [
    {
      depth: 1,
      at: '2026-10-02T09:00:00Z',
      amount: 9500,
      nodeId: 2,
      nodeIds: [2],
      edgeIds: [12],
      archiveIds: [1201],
    },
  ],
  passThroughNodeIds: [2],
  sentences: ['9,500 moved to ACC-778 within 53 seconds.'],
})

const facts = {
  node: taggedNode,
  definitions: [{guid: 'def-device', name: 'Shares device', counterparts: 4, count: 9, total: 0}],
  community,
  patterns: [pattern],
  sentences: ['Shared by 4 accounts.', '2 hops from a transaction or case tagged Mule.'],
}

const caseFacts = {
  node: caseNode,
  definitions: [{guid: caseEdgeGuid, name: 'In case', counterparts: 1, count: 1, total: 0}],
  community: null,
  patterns: [],
  sentences: ['Case 12 on this key.'],
}

const explode = {
  node: node(5, 'DeviceId', 'DEV-8831A'),
  from: '2026-09-20T00:00:00Z',
  to: '2026-10-20T00:00:00Z',
  lanes: [
    {
      node: node(1, 'AccountId', 'ACC-441', true, {nearestTag: 'Mule', nearestTagDistance: 1}),
      count: 2,
      total: 0,
      first: '2026-10-02T00:00:00Z',
      last: '2026-10-19T00:00:00Z',
      definitions: ['Shares device'],
      edgeIds: [11],
    },
    {
      node: node(2, 'AccountId', 'ACC-778'),
      count: 1,
      total: 0,
      first: '2026-10-05T00:00:00Z',
      last: '2026-10-05T00:00:00Z',
      definitions: ['Shares device'],
      edgeIds: [12],
    },
  ],
}

interface Options {
  permissions?: number[]
  directoryStatus?: number
  renderStatus?: number
  edgesStatus?: number
  explorations?: unknown[]
  decisions?: string[]
  overrides?: string[]
  caseNode?: boolean
}

async function stub(page: Page, renders: string[] = [], options: Options = {}) {
  await page.route('**/api/Me', (route) =>
    route.fulfill({json: {...me, permissions: options.permissions ?? me.permissions}}),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/GraphRegistry/GetByShowInDirectory', (route) =>
    options.directoryStatus
      ? route.fulfill({status: options.directoryStatus, body: ''})
      : route.fulfill({json: directory}),
  )
  await page.route('**/api/GraphRegistry/GetByGuidActiveOnly/*', (route) =>
    route.fulfill({json: registry}),
  )
  await page.route('**/api/GraphRegistryEdge/ByGraphRegistryId/*', (route) =>
    options.edgesStatus
      ? route.fulfill({status: options.edgesStatus, body: ''})
      : route.fulfill({json: edges}),
  )
  await page.route('**/api/Graph/Explorations**', (route) => {
    if (route.request().method() === 'GET')
      return route.fulfill({json: options.explorations ?? []})
    if (route.request().method() === 'DELETE') return route.fulfill({status: 204, body: ''})

    return route.fulfill({
      json: {...JSON.parse(route.request().postData() ?? '{}'), id: 77},
    })
  })
  await page.route('**/api/Graph/Render', (route) => {
    const body = route.request().postData() ?? ''

    renders.push(body)

    if (options.renderStatus) return route.fulfill({status: options.renderStatus, body: ''})

    const parsed = JSON.parse(body)

    return route.fulfill({
      json: render(
        parsed.query.expanded === '5',
        parsed.query.relevance === true,
        options.caseNode === true,
      ),
    })
  })
  await page.route('**/api/Graph/Paths**', (route) => route.fulfill({json: pathsData}))
  await page.route('**/api/Graph/Flow**', (route) =>
    route.fulfill({
      json: flowData(JSON.parse(route.request().postData() ?? '{}').forward !== false),
    }),
  )
  await page.route('**/api/Graph/Patterns**', (route) => {
    const searching = new URL(route.request().url()).pathname.endsWith('/Patterns/Search')

    if (route.request().method() === 'POST' && !searching) {
      options.decisions?.push(route.request().postData() ?? '')

      return route.fulfill({json: {...pattern, stateId: 3}})
    }

    return route.fulfill({
      json: searching ? [pattern] : pattern,
    })
  })
  await page.route('**/api/Graph/Communities**', (route) =>
    route.fulfill({
      json: new URL(route.request().url()).pathname.endsWith('/Communities')
        ? [community]
        : community,
    }),
  )
  await page.route('**/api/Graph/Hubs**', (route) => {
    if (route.request().method() === 'POST') {
      options.overrides?.push(route.request().postData() ?? '')

      return route.fulfill({json: {...hubNode, hubOverrideId: 2}})
    }

    return route.fulfill({json: [hubNode]})
  })
  await page.route('**/api/Graph/TimeStrip/Svg**', (route) =>
    route.fulfill({contentType: 'image/svg+xml', body: timeStrip}),
  )
  await page.route('**/api/Graph/Node**', (route) =>
    route.fulfill({
      json: JSON.parse(route.request().postData() ?? '{}').nodeId === 7 ? caseFacts : facts,
    }),
  )
  await page.route('**/api/Graph/Explode**', (route) => route.fulfill({json: explode}))
}

test.describe('graph directory', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('lists the graphs shown in the directory and opens one', async ({page}) => {
    await stub(page)
    await page.goto('/graph')

    const list = page.getByTestId('graph-directory-list')

    await expect(list).toContainText('Demonstration payments network')
    await expect(list).toContainText('Device sharing')
    await expect(list).not.toContainText('Case only network')
    await expect(page.getByTestId('graph-directory-count')).toHaveText('2 graphs')
    await expect(list.locator('.tabulator-row', {hasText: 'Device sharing'})).toContainText(
      'Radial',
    )

    await list
      .locator('.tabulator-row', {hasText: 'Demonstration payments network'})
      .getByRole('button', {name: 'Open'})
      .click()

    await expect(page).toHaveURL(/\/graph\/graph-guid$/)
    await expect(page.getByTestId('graph-registry-name')).toHaveText(
      'Demonstration payments network',
    )
    await expect(page.getByTestId('graph-pick-seed')).toBeVisible()
  })

  test('says so when the directory is refused or cannot be loaded', async ({page}) => {
    await stub(page, [], {directoryStatus: 403})
    await page.goto('/graph')

    await expect(page.getByTestId('graph-directory-failed')).toContainText('permission')
    await expect(page.getByTestId('graph-directory-retry')).toHaveCount(0)
  })

  test('is reached from the menu with the view permission only', async ({page}) => {
    await stub(page)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')
    await expect(page.getByTestId('graph-canvas').locator('[data-node="1"]')).toBeVisible()

    const link = page.getByRole('link', {name: 'Graph'}).first()

    await expect(link).toHaveAttribute('href', '/graph')
    await link.dispatchEvent('click')

    await expect(page).toHaveURL(/\/graph$/)
    await expect(page.getByTestId('graph-directory-list')).toBeVisible()
  })
})

test.describe('graph page', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('seeds from the registry keys, draws the network and holds the seed in the address', async ({
                                                                                                     page,
                                                                                                   }) => {
    const renders: string[] = []

    await stub(page, renders)
    await page.goto('/graph/graph-guid')

    const key = page.getByTestId('graph-seed-key')

    await expect(key.locator('option')).toHaveText(['AccountId', 'DeviceId'])
    await key.selectOption('AccountId')
    await page.getByTestId('graph-seed-value').fill('ACC-441')
    await page.getByTestId('graph-seed-go').click()

    await expect(page).toHaveURL(/gk=AccountId/)
    await expect(page).toHaveURL(/gq=ACC-441/)
    await expect(page.getByTestId('graph-canvas').locator('[data-node="1"]')).toBeVisible()
    await expect(page.getByTestId('graph-nodes')).toHaveText('Nodes 3 of 150')
    await expect(page.getByTestId('graph-hubs-hidden')).toHaveText('1 hub hidden')
    await expect(page.getByTestId('graph-legend')).toContainText('Shares device')

    const first = JSON.parse(renders[0]!)

    expect(first.query).toMatchObject({
      registryGuid: 'graph-guid',
      key: 'AccountId',
      value: 'ACC-441',
      hops: 2,
    })
    expect(first.anchors).toEqual([])
  })

  test('shows the tags of a node, labels the In case edge and opens a case from its node', async ({
                                                                                                    page,
                                                                                                  }) => {
    await stub(page, [], {caseNode: true})
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')

    const canvas = page.getByTestId('graph-canvas')

    await expect(page.getByTestId('graph-nodes')).toHaveText('Nodes 4 of 150')
    await expect(page.getByTestId('graph-edges')).toContainText('In case')
    await expect(page.getByTestId(`graph-legend-edge-${caseEdgeGuid}`)).toHaveText('In caseCase')
    await expect(page.getByTestId('graph-legend-tags')).toContainText('Tagged')

    await canvas.locator('[data-node="5"]').click()
    await expect(page.getByTestId('graph-panel-tag-Mule')).toHaveText(/Mule · 2 hops/)
    await expect(page.getByTestId('graph-panel-why')).toContainText('tagged Mule')

    await page.getByTestId('graph-explode-toggle').click()

    const explodeGrid = page.getByTestId('graph-explode')

    await expect(explodeGrid).toContainText('Nearest tag')
    await expect(explodeGrid).toContainText('Mule · 1 hop')

    await page.getByTestId('graph-explode-toggle').click()
    await canvas.locator('[data-node="7"]').click()

    await expect(page).toHaveURL(/gn=7/)
    await expect(page.getByTestId('graph-panel-value')).toHaveText('Case 12')
    await expect(page.getByTestId('graph-panel-tag-Mule')).toHaveText(/Mule · on this node/)
    await expect(page.getByTestId('graph-panel-pivot')).toHaveCount(0)
    await expect(page.getByTestId('graph-panel-open-case')).toHaveText('Open case 12')

    await page.getByTestId('graph-panel-open-case').click()
    await expect(page).toHaveURL(/\/cases\/12/)
  })

  test('falls back to a typed key when the edge definitions are refused', async ({page}) => {
    await stub(page, [], {edgesStatus: 403})
    await page.goto('/graph/graph-guid')

    const key = page.getByTestId('graph-seed-key')

    await expect(key).toHaveAttribute('type', 'text')
    await key.fill('AccountId')
    await page.getByTestId('graph-seed-value').fill('ACC-441')
    await page.getByTestId('graph-seed-go').click()

    await expect(page.getByTestId('graph-canvas').locator('[data-node="1"]')).toBeVisible()
  })

  test('selects a node, expands it with anchors and switches to Explode and back', async ({
                                                                                            page,
                                                                                          }) => {
    const renders: string[] = []

    await stub(page, renders)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')

    const canvas = page.getByTestId('graph-canvas')

    await canvas.locator('[data-node="5"]').click()
    await expect(page).toHaveURL(/gn=5/)
    await expect(page.getByTestId('graph-panel-value')).toHaveText('DEV-8831A')
    await expect(page.getByTestId('graph-panel-expand')).toContainText('adds')

    await page.getByTestId('graph-panel-expand').click()

    await expect(page).toHaveURL(/gx=5/)
    await expect(canvas.locator('[data-node="3"]')).toBeVisible()
    await expect(page.getByTestId('graph-nodes')).toHaveText('Nodes 4 of 150')

    const last = JSON.parse(renders.at(-1)!)

    expect(last.query.expanded).toBe('5')
    expect(last.anchors.map((anchor: { id: string }) => anchor.id)).toEqual(['1', '5', '2'])
    await expect(page.getByTestId('graph-panel-expand')).toHaveText('Expanded')

    await page.getByTestId('graph-explode-toggle').click()

    await expect(page).toHaveURL(/gd=1/)
    await expect(page.getByTestId('graph-explode')).toBeVisible()
    await expect(page.getByTestId('graph-explode-node')).toHaveText('DEV-8831A')
    await expect(page.getByTestId('graph-explode').locator('.tabulator-row')).toHaveCount(2)

    await page.getByTestId('graph-explode-toggle').click()

    await expect(page.getByTestId('graph-explode')).toHaveCount(0)
    await expect(canvas).toBeVisible()
  })

  test('changes hops and layout through the address and keeps edge kinds in it', async ({
                                                                                          page,
                                                                                        }) => {
    const renders: string[] = []

    await stub(page, renders)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')
    await expect(page.getByTestId('graph-canvas').locator('[data-node="1"]')).toBeVisible()

    await page.getByTestId('graph-hops-3').click()
    await expect(page).toHaveURL(/gh=3/)
    await page.getByTestId('graph-layout-3').click()
    await expect(page).toHaveURL(/gl=3/)
    await page.locator('label', {has: page.getByTestId('graph-edge-def-pays')}).click()
    await expect(page).toHaveURL(/ge=def-device/)

    await expect.poll(() => JSON.parse(renders.at(-1)!).query.edges).toBe('def-device')
    expect(JSON.parse(renders.at(-1)!).layoutId).toBe(3)
    expect(JSON.parse(renders.at(-1)!).query.hops).toBe(3)
  })

  test('hides edges after the time cursor and the nodes they orphan, client-side', async ({
                                                                                            page,
                                                                                          }) => {
    await stub(page)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')

    const canvas = page.getByTestId('graph-canvas')

    await expect(canvas.locator('[data-node="2"]')).toBeVisible()
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441&gt=1759000000')

    await expect(canvas.locator('[data-edge="11"]')).toHaveAttribute('data-hidden', 'true')
    await expect(canvas.locator('[data-node="2"]')).toHaveAttribute('data-hidden', 'true')
    await expect(canvas.locator('[data-node="1"]')).not.toHaveAttribute('data-hidden', 'true')

    await page.getByTestId('graph-cursor-clear').click()

    await expect(page).not.toHaveURL(/gt=/)
    await expect(canvas.locator('[data-edge="11"]')).not.toHaveAttribute('data-hidden', 'true')
  })

  test('saves, restores and deletes an exploration from the rail', async ({page}) => {
    await stub(page, [], {
      explorations: [
        {
          id: 9,
          name: 'Ring around DEV-8831A',
          graphRegistryGuid: 'graph-guid',
          address: 'gk=DeviceId&gq=DEV-8831A&gh=1',
          pinned: null,
        },
        {id: 10, name: 'Other graph', graphRegistryGuid: 'other-guid', address: '', pinned: null},
      ],
    })
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')

    const rail = page.getByTestId('graph-rail')

    await page.getByTestId('graph-rail-toggle').click()
    await expect(rail.getByTestId('graph-rail-patterns')).toContainText('Scatter-gather')

    await rail.getByTestId('graph-rail-tab-explorations').check()
    await expect(rail.getByTestId('graph-exploration-9')).toBeVisible()
    await expect(rail).not.toContainText('Other graph')

    await rail.getByTestId('graph-exploration-9').click()
    await expect(page).toHaveURL(/gk=DeviceId/)
    await expect(page).toHaveURL(/gh=1/)
    await expect(rail.getByTestId('graph-exploration-9')).toBeHidden()

    await page.getByTestId('graph-rail-toggle').click()
    await rail.getByTestId('graph-rail-tab-explorations').check()
    await rail.getByTestId('graph-exploration-name').fill('Payroll hub review')
    await rail.getByTestId('graph-exploration-save').click()
    await expect(page.getByText('Exploration saved.')).toBeVisible()

    await rail.getByTestId('graph-exploration-delete-9').click()
  })

  test('says so when the render is refused and offers a retry when it fails', async ({page}) => {
    await stub(page, [], {renderStatus: 403})
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')
    await expect(page.getByTestId('graph-forbidden')).toBeVisible()

    await stub(page, [], {renderStatus: 500})
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')
    await expect(page.getByTestId('graph-retry')).toBeVisible()
  })

  test('ranks by relevance when asked and emphasises the most relevant nodes', async ({page}) => {
    const renders: string[] = []

    await stub(page, renders)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441')
    await expect(page.getByTestId('graph-canvas').locator('[data-node="1"]')).toBeVisible()

    await page.getByTestId('graph-view-relevance').click()

    await expect(page).toHaveURL(/gv=r/)
    await expect.poll(() => JSON.parse(renders.at(-1)!).query.relevance).toBe(true)
    await expect(page.getByTestId('graph-canvas').locator('[data-node="5"]')).toHaveAttribute(
      'data-emphasis',
      'true',
    )
    await expect(page.getByTestId('graph-canvas').locator('[data-node="2"]')).not.toHaveAttribute(
      'data-emphasis',
      'true',
    )
  })

  test('follows the money from a node, lists the tranches and turns the flow around', async ({
                                                                                               page,
                                                                                             }) => {
    const renders: string[] = []

    await stub(page, renders)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441&gn=5')
    await expect(page.getByTestId('graph-panel-value')).toHaveText('DEV-8831A')

    await page.getByTestId('graph-panel-follow').click()

    await expect(page).toHaveURL(/gv=f/)
    await expect(page).toHaveURL(/gf=5(%3A|:)f/)
    await expect(page.getByTestId('graph-flow')).toBeVisible()
    await expect(page.getByTestId('graph-flow-count')).toHaveText('1 tranche')
    await expect(page.getByTestId('graph-tranche-0')).toContainText('ACC-778')
    await expect(page.getByTestId('graph-tranche-pass-through')).toBeVisible()
    await expect(page.getByTestId('graph-flow-sentences')).toContainText('53 seconds')
    await expect.poll(() => JSON.parse(renders.at(-1)!).flow?.nodeId).toBe(5)

    await page.getByTestId('graph-flow-backward').click()

    await expect(page).toHaveURL(/gf=5(%3A|:)b/)
    await expect.poll(() => JSON.parse(renders.at(-1)!).flow?.forward).toBe(false)

    await page.getByTestId('graph-flow-leave').click()

    await expect(page).not.toHaveURL(/gv=f/)
    await expect(page.getByTestId('graph-flow')).toHaveCount(0)
  })

  test('finds the paths between the selected node and a picked target', async ({page}) => {
    await stub(page)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441&gn=5')
    await expect(page.getByTestId('graph-panel-value')).toHaveText('DEV-8831A')

    await page.getByTestId('graph-panel-paths').click()
    await expect(page.getByTestId('graph-paths-picking')).toBeVisible()

    await page.getByTestId('graph-canvas').locator('[data-node="2"]').click()

    await expect(page).toHaveURL(/gy=2/)
    await expect(page.getByTestId('graph-paths-list')).toContainText('1 hops, weight 0.50')
    await expect(page.getByTestId('graph-paths-sentences')).toContainText('One path of one hop.')
    await expect(page.getByTestId('graph-canvas').locator('[data-edge="12"]')).toHaveAttribute(
      'data-emphasis',
      'true',
    )

    await page.getByTestId('graph-paths-close').click()

    await expect(page).not.toHaveURL(/gy=/)
  })

  test('lists patterns, communities and hubs in the rail and opens a pattern on its anchor', async ({
                                                                                                      page,
                                                                                                    }) => {
    await stub(page)
    await page.goto('/graph/graph-guid')

    const rail = page.getByTestId('graph-rail')

    await expect(rail.getByTestId('graph-pattern-3')).toBeHidden()
    await expect(page.getByTestId('graph-rail-toggle')).toContainText('1')
    await page.getByTestId('graph-rail-toggle').click()

    await expect(rail.getByTestId('graph-pattern-3')).toBeVisible()
    await expect(rail.getByTestId('graph-pattern-3')).toContainText('Scatter-gather')
    await expect(rail.getByTestId('graph-pattern-3')).toContainText('score 0.91')
    await expect(rail.getByTestId('graph-pattern-seen-3')).toHaveCount(0)

    await rail.getByTestId('graph-rail-tab-communities').check()
    await expect(rail.getByTestId('graph-community-812')).toContainText('#812')
    await expect(rail.getByTestId('graph-community-ratio-812')).toHaveText('2.1×')

    await rail.getByTestId('graph-rail-tab-hubs').check()
    await expect(rail.getByTestId('graph-hub-9')).toContainText('AA:BB:CC:DD:EE:FF')

    await rail.getByTestId('graph-rail-tab-patterns').check()
    await rail.getByTestId('graph-pattern-3').getByRole('button').first().click()

    await expect(page).toHaveURL(/gp=3/)
    await expect(page).toHaveURL(/gk=AccountId/)
    await expect(page.getByTestId('graph-canvas').locator('[data-node="5"]')).toHaveAttribute(
      'data-emphasis',
      'true',
    )
    await expect(page.getByTestId('graph-panel-pattern-3')).toContainText('Scatter-gather')
    await expect(rail.getByTestId('graph-pattern-3')).toBeHidden()

    await page.getByTestId('graph-rail-toggle').click()
    await rail.getByTestId('graph-rail-tab-communities').check()
    await rail.getByTestId('graph-community-812').click()

    await expect(page).toHaveURL(/gc=812/)
    await expect(page).toHaveURL(/gq=ACC-778/)
  })

  test('decides a pattern and overrides a hub with the resolve permission, asking first', async ({
                                                                                                   page,
                                                                                                 }) => {
    const decisions: string[] = []
    const overrides: string[] = []

    await stub(page, [], {permissions: [1, 49, 51], decisions, overrides})
    await page.goto('/graph/graph-guid')

    const rail = page.getByTestId('graph-rail')

    await page.getByTestId('graph-rail-toggle').click()
    await rail.getByTestId('graph-pattern-dismiss-3').click()
    await expect(page.getByTestId('graph-pattern-decide-dialog')).toContainText('Dismiss')
    await page.getByTestId('graph-pattern-decide-confirm').click()

    await expect.poll(() => decisions.length).toBe(1)
    expect(JSON.parse(decisions[0]!)).toMatchObject({id: 3, stateId: 3})
    await expect(page.getByText('Pattern updated.')).toBeVisible()

    await rail.getByTestId('graph-pattern-case-3').click()
    await page.getByTestId('graph-pattern-case-id').fill('9044')
    await page.getByTestId('graph-pattern-case-confirm').click()

    await expect.poll(() => decisions.length).toBe(2)
    expect(JSON.parse(decisions[1]!)).toMatchObject({id: 3, stateId: 4, caseId: 9044})

    await rail.getByTestId('graph-rail-tab-hubs').check()
    await expect(rail.getByTestId('graph-hub-override-9')).toHaveValue('1')
    await rail.getByTestId('graph-hub-override-9').selectOption('2')
    await expect(page.getByTestId('graph-hub-override-dialog')).toContainText('AA:BB:CC:DD:EE:FF')
    await page.getByTestId('graph-hub-override-confirm').click()

    await expect.poll(() => overrides.length).toBe(1)
    expect(JSON.parse(overrides[0]!)).toEqual({nodeId: 9, overrideId: 2})
    await expect(page.getByText('Hub override saved.')).toBeVisible()
  })

  test('has no accessibility violations in both themes and in Arabic', async ({page}) => {
    await stub(page)
    await page.goto('/graph/graph-guid?gk=AccountId&gq=ACC-441&gn=5')
    await expect(page.getByTestId('graph-panel-value')).toHaveText('DEV-8831A')

    for (const theme of ['light', 'dark'] as const) {
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await settleAnimations(page)

      const results = await new AxeBuilder({page}).exclude('.tabulator').analyze()

      expect(results.violations).toEqual([])
    }

    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await settleAnimations(page)

    const arabic = await new AxeBuilder({page}).exclude('.tabulator').analyze()

    expect(arabic.violations).toEqual([])
  })
})
