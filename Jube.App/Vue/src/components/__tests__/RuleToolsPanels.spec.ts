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
import {VueQueryPlugin, QueryClient} from '@tanstack/vue-query'
import {createPinia} from 'pinia'
import {createI18nInstance, loadLocale} from '../../i18n'
import {mountVapor, type MountedVapor} from '../../test-support/mountVapor'
import {integrityApi} from '../../integrity/api'
import {ruleToolsApi} from '../../ruleTools/api'
import RuleDependencies from '../RuleDependencies.vue'
import RuleIntegrity from '../RuleIntegrity.vue'

vi.mock('../../integrity/api', () => ({integrityApi: {report: vi.fn()}}))
vi.mock('../../ruleTools/api', () => ({
  ruleToolsApi: {dependencies: vi.fn(), dependents: vi.fn()},
}))

let mounted: MountedVapor | undefined

async function mount(component: object, props: Record<string, unknown>): Promise<MountedVapor> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')
  mounted = mountVapor(component, {
    props: {modelId: 1, ruleType: 'ActivationRule', ruleId: 7, ...props},
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

const text = () => mounted!.root.textContent ?? ''

const check = (entityId: number) => ({
  code: 'C1',
  title: 'Unknown field',
  severity: 'Warning',
  category: 'Compile',
  entityKind: 'ActivationRule',
  entityId,
  entityName: 'Rule',
  message: 'Field is not in the payload.',
})

const report = (checks: unknown[]) => ({checks})

beforeEach(() => {
  vi.mocked(integrityApi.report).mockReset()
  vi.mocked(ruleToolsApi.dependencies).mockReset()
  vi.mocked(ruleToolsApi.dependents).mockReset()
})

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
})

describe('RuleIntegrity', () => {
  it('asks for the rule to be saved first and makes no request', async () => {
    await mount(RuleIntegrity, {ruleId: null})

    expect(text()).toContain('Save the rule to check its integrity.')
    expect(integrityApi.report).not.toHaveBeenCalled()
  })

  it('says there are no findings when none concern this rule', async () => {
    vi.mocked(integrityApi.report).mockResolvedValue(report([check(99)]) as never)
    await mount(RuleIntegrity, {})

    await vi.waitFor(() =>
      expect(mounted!.root.querySelector('[data-testid="rule-integrity-none"]')).not.toBeNull(),
    )
  })

  it('shows the findings of this rule only', async () => {
    vi.mocked(integrityApi.report).mockResolvedValue(report([check(7), check(99)]) as never)
    await mount(RuleIntegrity, {})

    await vi.waitFor(() => expect(text()).toContain('Finding'))
    expect(mounted!.root.querySelector('[data-testid="rule-integrity-none"]')).toBeNull()
  })

  it('reports a failed load, which includes a refusal', async () => {
    vi.mocked(integrityApi.report).mockRejectedValue(new Error('403'))
    await mount(RuleIntegrity, {})

    await vi.waitFor(() => expect(text()).toContain('The integrity findings could not be loaded.'))
  })
})

describe('RuleDependencies', () => {
  it('asks for the rule to be saved first and makes no request', async () => {
    await mount(RuleDependencies, {ruleId: null})

    expect(text()).toContain('Save the rule to see what it uses and what uses it.')
    expect(ruleToolsApi.dependencies).not.toHaveBeenCalled()
    expect(ruleToolsApi.dependents).not.toHaveBeenCalled()
  })

  it('shows what the rule uses and what uses it', async () => {
    vi.mocked(ruleToolsApi.dependencies).mockResolvedValue({
      entity: null,
      dependencies: [],
    } as never)
    vi.mocked(ruleToolsApi.dependents).mockResolvedValue({entity: null, dependents: []} as never)
    await mount(RuleDependencies, {})

    await vi.waitFor(() => {
      expect(text()).toContain('Uses')
      expect(text()).toContain('Used by')
    })
    expect(ruleToolsApi.dependencies).toHaveBeenCalledWith(1, 'ActivationRule', 7)
    expect(ruleToolsApi.dependents).toHaveBeenCalledWith(1, 'ActivationRule', 7)
  })

  it('reports a failed load', async () => {
    vi.mocked(ruleToolsApi.dependencies).mockRejectedValue(new Error('500'))
    vi.mocked(ruleToolsApi.dependents).mockResolvedValue({entity: null, dependents: []} as never)
    await mount(RuleDependencies, {})

    await vi.waitFor(() => expect(text()).toContain('The dependencies could not be loaded.'))
  })
})
