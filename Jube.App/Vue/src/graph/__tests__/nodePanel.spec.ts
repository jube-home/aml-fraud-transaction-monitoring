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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {nextTick} from 'vue'
import {createI18nInstance, loadLocale} from '../../i18n'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'
import GraphNodePanel from '../GraphNodePanel.vue'
import type {GraphCombo, GraphNode, GraphNodeFacts} from '../types'

const node: GraphNode = {
  id: 5,
  key: 'DeviceId',
  value: 'DEV-8831A',
  hub: false,
  degree: 14,
  degreeMoney: 0,
  degreeIdentity: 14,
  hubNeighbourCount: 0,
  componentId: 1,
  componentSize: 37,
  communityId: 812,
  coreNumber: 2,
  pageRank: 0.1,
  clusteringCoefficient: 0.2,
  distanceToConfirmed: 1,
  confirmedWithin1: 1,
  confirmedWithin2: 1,
  confirmedWithin3: 1,
  propagationScore: 0.3,
  hubScore: null,
  authorityScore: null,
  distanceToNotConfirmed: null,
  interest: 0.8,
  computedDate: null,
  hubOverrideId: 0,
  firstSeen: '2026-10-01T09:00:00Z',
  lastSeen: '2026-10-20T09:00:00Z',
  depth: 1,
  viaNodeId: 1,
  seed: false,
  cases: {open: 2, closed: 1, confirmed: 1, latestCaseId: 9044, latestOpenCaseId: 9044},
  nearestTag: 'Mule',
  nearestTagDistance: 2,
  tags: [{tag: 'Mule', distance: 2, within1: 0, within2: 1, within3: 1}],
}

const facts: GraphNodeFacts = {
  node,
  definitions: [
    {guid: 'def-device', name: 'Shares device', counterparts: 4, count: 12, total: 0},
  ],
  community: null,
  patterns: [],
  sentences: ['Shared by 4 accounts.', 'One confirmed true positive within one hop.'],
}

let mounted: MountedVapor | undefined

async function mount(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  mounted = mountVapor(GraphNodePanel, {
    props: {nodeId: null, node: null, combo: null, ...props},
    events: [
      'expand',
      'pivot',
      'link',
      'openCase',
      'capture',
      'explodeCombo',
      'paths',
      'follow',
      'pattern',
      'community',
    ],
    plugins: [i18n],
  })
  await nextTick()

  return mounted
}

function byTestId(id: string): HTMLElement | null {
  return mounted!.root.querySelector(`[data-testid="${id}"]`)
}

beforeEach(() => {
  HTMLDialogElement.prototype.showModal ??= vi.fn()
  HTMLDialogElement.prototype.close ??= vi.fn()
})

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('GraphNodePanel', () => {
  it('asks for a pick when nothing is selected', async () => {
    await mount()

    expect(byTestId('graph-panel-pick')).not.toBeNull()
  })

  it('shows the facts, the sentences and the counterparts of the selected node', async () => {
    await mount({nodeId: '5', node, facts, adds: 10})

    expect(byTestId('graph-panel-value')?.textContent).toBe('DEV-8831A')
    expect(byTestId('graph-panel-confirmed')?.textContent).toContain('1 confirmed')
    expect(byTestId('graph-panel-why')?.textContent).toContain('Shared by 4 accounts.')
    expect(byTestId('graph-panel-definitions')?.textContent).toContain('4 counterparts')
    expect(byTestId('graph-panel-expand')?.textContent?.trim()).toBe('Expand (adds 10)')
    expect(byTestId('graph-panel-tags')?.textContent).toContain('Mule · 2 hops')
  })

  it('isolates the node value as left-to-right and the sentences as auto direction', async () => {
    await mount({nodeId: '5', node, facts, adds: 10})

    expect(byTestId('graph-panel-value')?.getAttribute('dir')).toBe('ltr')
    expect(
      Array.from(byTestId('graph-panel-why')?.querySelectorAll('li') ?? []).every(
        (item) => item.getAttribute('dir') === 'auto',
      ),
    ).toBe(true)
  })

  it('shows a tag carried by the node itself without a hop count', async () => {
    await mount({
      nodeId: '5',
      node: {
        ...node,
        nearestTag: 'Victim',
        nearestTagDistance: 0,
        tags: [
          {tag: 'Victim', distance: 0, within1: 1, within2: 1, within3: 1},
          {tag: 'Mule', distance: 1, within1: 1, within2: 1, within3: 1},
        ],
      },
    })

    expect(byTestId('graph-panel-tag-Victim')?.textContent).toContain('Victim · on this node')
    expect(byTestId('graph-panel-tag-Mule')?.textContent).toContain('Mule · 1 hop')
  })

  it('heads a Case node as the case, drops Pivot and opens that case', async () => {
    const panel = await mount({
      nodeId: '7',
      node: {
        ...node,
        id: 7,
        key: 'Case',
        value: '12',
        tags: [],
        cases: {...node.cases, latestCaseId: null, latestOpenCaseId: null},
      },
    })

    expect(byTestId('graph-panel-value')?.textContent?.trim()).toBe('Case 12')
    expect(byTestId('graph-panel-pivot')).toBeNull()
    expect(byTestId('graph-panel-open-case')?.textContent?.trim()).toBe('Open case 12')

    byTestId('graph-panel-open-case')!.click()

    expect(panel.emitted.openCase).toEqual([[12]])
  })

  it('emits expand with the node id, pivot with the key and value and the open case id', async () => {
    const panel = await mount({
      nodeId: '5',
      node,
      facts,
      adds: 10,
      canCapture: true,
      canLink: true,
    })

    byTestId('graph-panel-expand')!.click()
    byTestId('graph-panel-pivot')!.click()
    byTestId('graph-panel-open-case')!.click()
    byTestId('graph-panel-capture')!.click()
    byTestId('graph-panel-link')!.click()

    expect(panel.emitted.expand).toEqual([['5']])
    expect(panel.emitted.pivot).toEqual([[{key: 'DeviceId', value: 'DEV-8831A'}]])
    expect(panel.emitted.openCase).toEqual([[9044]])
    expect(panel.emitted.capture).toEqual([[]])
    expect(panel.emitted.link).toEqual([[node]])
  })

  it('disables Expand once expanded or when there is nothing to add', async () => {
    await mount({nodeId: '5', node, expanded: true, adds: 3})

    expect((byTestId('graph-panel-expand') as HTMLButtonElement).disabled).toBe(true)
    expect(byTestId('graph-panel-expand')?.textContent?.trim()).toBe('Expanded')

    mounted!.unmount()
    await mount({nodeId: '5', node, adds: 0})

    expect((byTestId('graph-panel-expand') as HTMLButtonElement).disabled).toBe(true)
  })

  it('asks before expanding a hub and expands only after confirming', async () => {
    const panel = await mount({nodeId: '5', node: {...node, hub: true, degree: 480}, adds: 400})

    byTestId('graph-panel-expand')!.click()
    await nextTick()

    expect(panel.emitted.expand).toBeUndefined()

    const dialog = byTestId('graph-hub-expand-dialog') as HTMLElement

    expect(dialog.textContent).toContain('480 neighbours')
    ;(byTestId('graph-hub-expand-confirm') as HTMLButtonElement).click()
    await nextTick()

    expect(panel.emitted.expand).toEqual([['5']])
  })

  it('starts a paths pick, follows the money and opens patterns and the community', async () => {
    const pattern = {
      id: 3,
      kindId: 4,
      kind: 'Scatter-gather',
      anchor: null,
      score: 0.9,
      windowFrom: '2026-10-01T00:00:00Z',
      windowTo: '2026-10-02T00:00:00Z',
      parameters: {},
      archiveIds: [],
      memberCount: 2,
      members: [],
      stateId: 2,
      caseId: null,
      firstDetectedDate: '2026-10-02T00:00:00Z',
      lastDetectedDate: '2026-10-02T00:00:00Z',
      decidedUser: null,
      decidedDate: null,
      sentence: null,
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
      members: [],
      sentence: null,
    }
    const panel = await mount({
      nodeId: '5',
      node,
      facts: {...facts, patterns: [pattern], community},
    })

    expect(byTestId('graph-panel-interest')?.textContent?.trim()).toBe('0.80')
    expect(byTestId('graph-panel-pattern-3')?.textContent).toContain('Scatter-gather')
    expect(byTestId('graph-panel-pattern-3')?.textContent).toContain('Seen')
    expect(byTestId('graph-panel-community')?.textContent).toContain('2.1× tenant rate')

    byTestId('graph-panel-paths')!.click()
    byTestId('graph-panel-follow')!.click()
    byTestId('graph-panel-pattern-3')!.click()
    byTestId('graph-panel-community')!.click()

    expect(panel.emitted.paths).toEqual([[]])
    expect(panel.emitted.follow).toEqual([['5']])
    expect(panel.emitted.pattern).toEqual([[pattern]])
    expect(panel.emitted.community).toEqual([[812]])
  })

  it('describes a collapsed cluster and offers to list it in Explode', async () => {
    const combo: GraphCombo = {
      id: '1:AccountId',
      viaNodeId: 1,
      key: 'AccountId',
      count: 40,
      nodeIds: [],
    }
    const panel = await mount({nodeId: 'c:1:AccountId', node: null, combo})

    expect(byTestId('graph-combo-panel')?.textContent).toContain('40 AccountId nodes')

    byTestId('graph-combo-explode')!.click()

    expect(panel.emitted.explodeCombo).toEqual([[combo]])
  })
})
