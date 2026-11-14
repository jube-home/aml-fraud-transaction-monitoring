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

import {afterEach, describe, expect, it} from 'vitest'
import {nextTick} from 'vue'
import {createPinia} from 'pinia'
import JubeChart from '../JubeChart.vue'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'

const svg =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">' +
  '<g data-edge="11" data-from="1" data-to="2" data-first="100"><path class="ep" d="M0 0 L10 10"/></g>' +
  '<g data-node="1"><circle class="nb" r="4"/></g>' +
  '<g data-node="2"><circle class="nb" r="4"/></g>' +
  '<script>alert(1)</script>' +
  '</svg>'

let mounted: MountedVapor | undefined

async function mount(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  mounted = mountVapor(JubeChart, {
    props: {svg, label: 'Test chart', interactive: true, ...props},
    events: ['pick', 'pickEdge'],
    plugins: [createPinia()],
  })
  await nextTick()

  return mounted
}

function query(selector: string): Element | null {
  return mounted!.root.querySelector(selector)
}

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('JubeChart', () => {
  it('mounts the sanitised SVG and makes nodes and edges reachable by keyboard', async () => {
    await mount()

    expect(query('script')).toBeNull()
    expect(query('[data-node="1"]')?.getAttribute('tabindex')).toBe('0')
    expect(query('[data-edge="11"]')?.getAttribute('tabindex')).toBe('0')
  })

  it('emits pick for a node, pickEdge for an edge and pick null for the blank canvas', async () => {
    const chart = await mount()

    ;(query('[data-node="2"] circle') as SVGElement).dispatchEvent(
      new MouseEvent('click', {bubbles: true}),
    )
    ;(query('[data-edge="11"] path') as SVGElement).dispatchEvent(
      new MouseEvent('click', {bubbles: true}),
    )
    ;(query('svg') as SVGElement).dispatchEvent(new MouseEvent('click', {bubbles: true}))

    expect(chart.emitted.pick).toEqual([['2'], [null]])
    expect(chart.emitted.pickEdge).toEqual([['11']])
  })

  it('emits pickEdge on Enter and clears both on Escape', async () => {
    const chart = await mount()
    const edge = query('[data-edge="11"]') as SVGElement

    edge.dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', bubbles: true}))
    edge.dispatchEvent(new KeyboardEvent('keydown', {key: 'Escape', bubbles: true}))

    expect(chart.emitted.pickEdge).toEqual([['11'], [null]])
    expect(chart.emitted.pick).toEqual([[null]])
  })

  it('marks the selected edge and node with data-selected', async () => {
    const chart = await mount({selected: '1', selectedEdge: '11'})

    expect(query('[data-node="1"]')?.getAttribute('data-selected')).toBe('true')
    expect(query('[data-node="2"]')?.hasAttribute('data-selected')).toBe(false)
    expect(query('[data-edge="11"]')?.getAttribute('data-selected')).toBe('true')

    chart.setProps({selectedEdge: null})
    await nextTick()

    expect(query('[data-edge="11"]')?.hasAttribute('data-selected')).toBe(false)
  })

  it('toggles data-hidden and data-emphasis from the sets without re-parsing the SVG', async () => {
    const chart = await mount({hidden: new Set(['11', '2']), emphasised: new Set(['11'])})
    const before = query('svg')

    expect(query('[data-edge="11"]')?.getAttribute('data-hidden')).toBe('true')
    expect(query('[data-node="2"]')?.getAttribute('data-hidden')).toBe('true')
    expect(query('[data-node="1"]')?.hasAttribute('data-hidden')).toBe(false)
    expect(query('[data-edge="11"]')?.getAttribute('data-emphasis')).toBe('true')

    chart.setProps({hidden: new Set<string>(), emphasised: new Set<string>()})
    await nextTick()

    expect(query('[data-edge="11"]')?.hasAttribute('data-hidden')).toBe(false)
    expect(query('[data-node="2"]')?.hasAttribute('data-hidden')).toBe(false)
    expect(query('[data-edge="11"]')?.hasAttribute('data-emphasis')).toBe(false)
    expect(query('svg')).toBe(before)
  })

  it('strips external and javascript links and animation elements from the SVG', async () => {
    await mount({
      svg:
        '<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">' +
        '<a id="bad" href="javascript:alert(1)"><text>x</text></a>' +
        '<a id="legacy" xlink:href="https://example.test/"><text>y</text></a>' +
        '<use id="local" href="#sym"/>' +
        '<animate attributeName="href" to="javascript:alert(1)"/>' +
        '<set attributeName="href" to="javascript:alert(1)"/>' +
        '</svg>',
    })

    expect(query('#bad')?.hasAttribute('href')).toBe(false)
    expect(query('#legacy')?.attributes.length).toBe(1)
    expect(query('#legacy')?.getAttribute('xlink:href')).toBeNull()
    expect(query('#local')?.getAttribute('href')).toBe('#sym')
    expect(query('animate')).toBeNull()
    expect(query('set')).toBeNull()
  })

  it('does not pick when not interactive', async () => {
    const chart = await mount({interactive: false})

    ;(query('[data-node="2"]') as SVGElement).dispatchEvent(
      new MouseEvent('click', {bubbles: true}),
    )

    expect(chart.emitted.pick).toBeUndefined()
    expect(query('[data-node="1"]')?.hasAttribute('tabindex')).toBe(false)
  })
})
