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
import GridRowSummary from '../GridRowSummary.vue'
import {createI18nInstance, loadLocale} from '../../i18n'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'

let mounted: MountedVapor | undefined

async function mountSummary(props: Record<string, unknown> = {}): Promise<HTMLElement> {
  const i18n = createI18nInstance()

  await loadLocale(i18n.global, 'en')

  mounted = mountVapor(GridRowSummary, {props, plugins: [i18n]})
  await nextTick()

  return mounted.root
}

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('GridRowSummary', () => {
  it('renders each stat with its title, value and hint', async () => {
    const root = await mountSummary({
      stats: [
        {label: 'Runs', value: '12', hint: 'Last 30 days'},
        {label: 'Failed', value: '3'},
      ],
    })

    const titles = [...root.querySelectorAll('.stat-title')].map((node) => node.textContent)
    const values = [...root.querySelectorAll('.stat-value')].map((node) => node.textContent?.trim())

    expect(titles).toEqual(['Runs', 'Failed'])
    expect(values).toEqual(['12', '3'])
    expect(root.querySelectorAll('.stat-desc')).toHaveLength(1)
    expect(root.querySelector('.stat-desc')?.textContent).toBe('Last 30 days')
  })

  it('marks an error stat with the error colour and leaves others plain', async () => {
    const root = await mountSummary({
      stats: [
        {label: 'Failed', value: '3', tone: 'error'},
        {label: 'Runs', value: '12'},
      ],
    })

    const values = [...root.querySelectorAll('.stat-value')]

    expect(values[0]?.classList.contains('text-error')).toBe(true)
    expect(values[1]?.classList.contains('text-error')).toBe(false)
  })

  it('lists facts as term and definition pairs', async () => {
    const root = await mountSummary({
      facts: [
        {label: 'Owner', value: 'Ada'},
        {label: 'Version', value: '7', mono: true},
      ],
    })

    expect([...root.querySelectorAll('dt')].map((node) => node.textContent)).toEqual([
      'Owner',
      'Version',
    ])
    expect([...root.querySelectorAll('dd')].map((node) => node.textContent)).toEqual(['Ada', '7'])
    expect(root.querySelectorAll('dd')[1]?.classList.contains('font-mono')).toBe(true)
  })

  it('omits the stats strip and facts list when there are none', async () => {
    const root = await mountSummary()

    expect(root.querySelector('.stats')).toBeNull()
    expect(root.querySelector('dl')).toBeNull()
  })

  it('shows only a skeleton while loading', async () => {
    const root = await mountSummary({
      loading: true,
      stats: [{label: 'Runs', value: '12'}],
    })

    expect(root.querySelector('.skeleton')).not.toBeNull()
    expect(root.querySelector('.stats')).toBeNull()
  })

  it('shows the localised failure message instead of content when failed', async () => {
    const root = await mountSummary({
      failed: true,
      stats: [{label: 'Runs', value: '12'}],
    })

    expect(root.querySelector('[role="alert"]')?.textContent).toBe('Could not load the summary.')
    expect(root.querySelector('.stats')).toBeNull()
  })

  it('uses the supplied test id, defaulting to grid-summary', async () => {
    const root = await mountSummary({testid: 'custom-summary'})

    expect(root.querySelector('[data-testid="custom-summary"]')).not.toBeNull()
    expect(root.querySelector('[data-testid="grid-summary"]')).toBeNull()
  })
})
