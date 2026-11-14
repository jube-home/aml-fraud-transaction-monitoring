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
import {VueQueryPlugin, QueryClient} from '@tanstack/vue-query'
import {createPinia} from 'pinia'
import {createI18nInstance, loadLocale} from '../../i18n'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'
import RuleTools from '../RuleTools.vue'

let mounted: MountedVapor | undefined

async function mount(props: Record<string, unknown>, locale = 'en'): Promise<MountedVapor> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, locale)
  mounted = mountVapor(RuleTools, {
    props: {modelId: 1, ruleId: 7, ruleText: 'Amount > 5', ...props},
    plugins: [
      i18n,
      createPinia(),
      [
        VueQueryPlugin,
        {queryClient: new QueryClient({defaultOptions: {queries: {retry: false}}})},
      ],
    ],
  })
  await nextTick()

  return mounted
}

const has = (id: string) => mounted!.root.querySelector(`[data-testid="${id}"]`) !== null

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('RuleTools', () => {
  it('offers all three tools collapsed when every permission is held', async () => {
    await mount({canBacktest: true, canCheckIntegrity: true, canDependencies: true})

    expect(has('rule-tools-backtest')).toBe(true)
    expect(has('rule-tools-dependencies')).toBe(true)
    expect(has('rule-tools-integrity')).toBe(true)
    expect(has('rule-tools-denied')).toBe(false)
    expect(mounted!.root.querySelectorAll('details[open]')).toHaveLength(0)
    expect(has('backtest-panel')).toBe(false)
    expect(has('rule-integrity')).toBe(false)
  })

  it('leaves out the tools the user may not use', async () => {
    await mount({canBacktest: false, canCheckIntegrity: true, canDependencies: false})

    expect(has('rule-tools-backtest')).toBe(false)
    expect(has('rule-tools-dependencies')).toBe(false)
    expect(has('rule-tools-integrity')).toBe(true)
  })

  it('says so when no tool is permitted', async () => {
    await mount({canBacktest: false, canCheckIntegrity: false, canDependencies: false})

    expect(has('rule-tools-denied')).toBe(true)
    expect(mounted!.root.querySelectorAll('details')).toHaveLength(0)
  })

  it('mounts a tool only when it is first opened, so closed tools make no request', async () => {
    await mount({canBacktest: false, canCheckIntegrity: true, canDependencies: false})
    const details = mounted!.root.querySelector('details') as HTMLDetailsElement

    details.open = true
    details.dispatchEvent(new Event('toggle'))
    await nextTick()

    expect(has('rule-integrity')).toBe(true)
  })

  it('is labelled in the chosen language', async () => {
    await mount({canBacktest: false, canCheckIntegrity: false, canDependencies: true}, 'ar')

    expect(mounted!.root.querySelector('section')?.getAttribute('aria-label')).not.toBe(
      'Rule tools',
    )
  })
})
