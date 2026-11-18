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

import { afterEach, describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import { createI18nInstance, loadLocale } from '../../i18n'
import { mountVapor, type MountedVapor } from '../../test-support/mountVapor'
import GraphToolbar from '../GraphToolbar.vue'
import type { GraphEdgeDefinition } from '../types'

const definitions: GraphEdgeDefinition[] = [
  {
    guid: 'def-device',
    name: 'Shares device',
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
    guid: 'def-address',
    name: 'Shares address',
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

let mounted: MountedVapor | undefined

async function mount(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  mounted = mountVapor(GraphToolbar, {
    props: {
      view: 'hops',
      hops: 2,
      layout: 1,
      edges: [],
      definitions,
      hiddenHubs: 2,
      includeHubs: false,
      minimumAmount: 0,
      nodes: 23,
      cap: 150,
      explode: false,
      ...props,
    },
    events: [
      'update:view',
      'update:flowDirection',
      'update:flowWindowHours',
      'update:flowTolerance',
      'update:hops',
      'update:layout',
      'toggleEdge',
      'update:includeHubs',
      'update:minimumAmount',
      'update:explode',
      'capture',
    ],
    plugins: [i18n],
  })
  await nextTick()

  return mounted
}

function byTestId(id: string): HTMLElement {
  return mounted!.root.querySelector(`[data-testid="${id}"]`) as HTMLElement
}

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('GraphToolbar', () => {
  it('offers Relevance, marks the active hop count and keeps Flow for a selected node', async () => {
    const toolbar = await mount()

    const relevance = byTestId('graph-view-relevance') as HTMLButtonElement

    expect(relevance.disabled).toBe(false)
    relevance.click()
    expect(toolbar.emitted['update:view']).toEqual([['relevance']])

    const flow = byTestId('graph-view-flow') as HTMLButtonElement

    expect(flow.disabled).toBe(true)
    expect(flow.parentElement?.getAttribute('data-tip')).toBe('Select a node to follow its money')
    expect(byTestId('graph-hops-2').getAttribute('aria-pressed')).toBe('true')
    expect(byTestId('graph-hops-1').getAttribute('aria-pressed')).toBe('false')
    expect(byTestId('graph-hops-1').textContent?.trim()).toBe('1 hop')
    expect(byTestId('graph-hops-3').textContent?.trim()).toBe('3 hops')
  })

  it('emits the hop count, layout and explode toggle', async () => {
    const toolbar = await mount()

    byTestId('graph-hops-3').click()
    byTestId('graph-layout-3').click()
    byTestId('graph-explode-toggle').click()

    expect(toolbar.emitted['update:hops']).toEqual([[3]])
    expect(toolbar.emitted['update:layout']).toEqual([[3]])
    expect(toolbar.emitted['update:explode']).toEqual([[true]])
  })

  it('lists edge kinds, disables restricted ones the caller may not see and emits toggles', async () => {
    const toolbar = await mount()

    const device = byTestId('graph-edge-def-device') as HTMLInputElement
    const address = byTestId('graph-edge-def-address') as HTMLInputElement

    expect(device.checked).toBe(true)
    expect(device.disabled).toBe(false)
    expect(address.disabled).toBe(true)
    expect(address.checked).toBe(false)
    expect(byTestId('graph-edges').textContent).toContain('Restricted')

    device.click()

    expect(toolbar.emitted.toggleEdge).toEqual([['def-device']])
  })

  it('shows the hidden hubs, the node count against the cap and hides Capture without a case', async () => {
    await mount()

    expect(byTestId('graph-hubs-hidden').textContent?.trim()).toBe('2 hubs hidden')
    expect(byTestId('graph-nodes').textContent?.trim()).toBe('Nodes 23 of 150')
    expect(mounted!.root.querySelector('[data-testid="graph-capture"]')).toBeNull()
  })

  it('offers Capture only with a case and emits it', async () => {
    const toolbar = await mount({ canCapture: true })

    byTestId('graph-capture').click()

    expect(toolbar.emitted.capture).toEqual([[]])
  })

  it('shows the flow controls in the flow view and emits direction and bounds', async () => {
    const toolbar = await mount({ view: 'flow', flowDirection: 'forward', flowAvailable: true })

    expect(mounted!.root.querySelector('[data-testid="graph-layout-1"]')).toBeNull()
    expect(byTestId('graph-flow-forward').getAttribute('aria-pressed')).toBe('true')

    byTestId('graph-flow-backward').click()

    const hours = byTestId('graph-flow-hours') as HTMLInputElement

    hours.value = '48'
    hours.dispatchEvent(new Event('change'))

    const tolerance = byTestId('graph-flow-tolerance') as HTMLInputElement

    tolerance.value = '0.3'
    tolerance.dispatchEvent(new Event('change'))

    expect(toolbar.emitted['update:flowDirection']).toEqual([['backward']])
    expect(toolbar.emitted['update:flowWindowHours']).toEqual([[48]])
    expect(toolbar.emitted['update:flowTolerance']).toEqual([[0.3]])
  })

  it('emits a bounded minimum amount when the field changes', async () => {
    const toolbar = await mount()
    const field = byTestId('graph-minimum-amount') as HTMLInputElement

    field.value = '250'
    field.dispatchEvent(new Event('input'))
    field.dispatchEvent(new Event('change'))

    field.value = 'abc'
    field.dispatchEvent(new Event('input'))
    field.dispatchEvent(new Event('change'))

    expect(toolbar.emitted['update:minimumAmount']).toEqual([[250], [0]])
  })
})
