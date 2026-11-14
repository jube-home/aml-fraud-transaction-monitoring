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
import GraphRail from '../GraphRail.vue'
import type {GraphCommunity, GraphNode, GraphPattern} from '../types'

const anchor: GraphNode = {
  id: 1,
  key: 'AccountId',
  value: 'ACC-441',
  hub: false,
  degree: 4,
  degreeMoney: 2,
  degreeIdentity: 2,
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
  depth: 0,
  viaNodeId: null,
  seed: true,
  cases: {open: 0, closed: 0, confirmed: 0, latestCaseId: null, latestOpenCaseId: null},
  nearestTag: null,
  nearestTagDistance: null,
  tags: [],
}

const pattern: GraphPattern = {
  id: 3,
  kindId: 4,
  kind: 'Scatter-gather',
  anchor,
  score: 0.91,
  windowFrom: '2026-10-01T00:00:00Z',
  windowTo: '2026-10-02T00:00:00Z',
  parameters: {intermediaries: 2},
  archiveIds: [201],
  memberCount: 6,
  members: [],
  stateId: 1,
  caseId: null,
  firstDetectedDate: '2026-10-02T00:00:00Z',
  lastDetectedDate: '2026-10-02T00:00:00Z',
  decidedUser: null,
  decidedDate: null,
  sentence: 'Six accounts fanned out and back within six hours.',
}

const community: GraphCommunity = {
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

const hub: GraphNode = {
  ...anchor,
  id: 9,
  key: 'MacAddress',
  value: 'AA:BB',
  hub: true,
  degree: 480,
  hubOverrideId: 2,
}

let mounted: MountedVapor | undefined

async function mount(props: Record<string, unknown> = {}): Promise<MountedVapor> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  mounted = mountVapor(GraphRail, {
    props: {
      explorations: [],
      patterns: [pattern],
      communities: [community],
      hubs: [hub],
      patternState: null,
      ...props,
    },
    events: [
      'save',
      'restore',
      'remove',
      'update:patternState',
      'openPattern',
      'decide',
      'openCommunity',
      'hubOverride',
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

describe('GraphRail', () => {
  it('lists patterns with kind, state, anchor, score and members and opens one', async () => {
    const rail = await mount()
    const row = byTestId('graph-pattern-3')!

    expect(row.textContent).toContain('Scatter-gather')
    expect(row.textContent).toContain('New')
    expect(row.textContent).toContain('ACC-441')
    expect(row.textContent).toContain('score 0.91')
    expect(row.textContent).toContain('6 members')
    expect(row.textContent).toContain('Six accounts fanned out')

    ;(row.querySelector('button') as HTMLButtonElement).click()

    expect(rail.emitted.openPattern).toEqual([[pattern]])
  })

  it('hides the decide actions and hub overrides without the resolve permission', async () => {
    await mount()

    expect(byTestId('graph-pattern-seen-3')).toBeNull()
    expect(byTestId('graph-pattern-dismiss-3')).toBeNull()
    expect(byTestId('graph-hub-override-9')).toBeNull()
  })

  it('asks before marking seen or dismissing and emits the decision on confirm', async () => {
    const rail = await mount({canResolve: true})

    byTestId('graph-pattern-dismiss-3')!.click()
    await nextTick()

    expect(rail.emitted.decide).toBeUndefined()
    expect(byTestId('graph-pattern-decide-dialog')?.textContent).toContain(
      'Dismiss Scatter-gather ACC-441?',
    )

    ;(byTestId('graph-pattern-decide-confirm') as HTMLButtonElement).click()
    await nextTick()

    expect(rail.emitted.decide).toEqual([[pattern, 3, null]])

    byTestId('graph-pattern-seen-3')!.click()
    await nextTick()
    ;(byTestId('graph-pattern-decide-confirm') as HTMLButtonElement).click()

    expect(rail.emitted.decide?.at(-1)).toEqual([pattern, 2, null])
  })

  it('asks for a case id before attaching a pattern to a case', async () => {
    const rail = await mount({canResolve: true})

    byTestId('graph-pattern-case-3')!.click()
    await nextTick()

    const confirm = byTestId('graph-pattern-case-confirm') as HTMLButtonElement

    expect(confirm.disabled).toBe(true)

    const field = byTestId('graph-pattern-case-id') as HTMLInputElement

    field.value = '9044'
    field.dispatchEvent(new Event('input'))
    await nextTick()

    expect(confirm.disabled).toBe(false)
    confirm.click()
    await nextTick()

    expect(rail.emitted.decide).toEqual([[pattern, 4, 9044]])
  })

  it('filters patterns by state through the select', async () => {
    const rail = await mount()
    const select = byTestId('graph-pattern-state') as HTMLSelectElement

    select.value = '3'
    select.dispatchEvent(new Event('change'))
    select.value = ''
    select.dispatchEvent(new Event('change'))

    expect(rail.emitted['update:patternState']).toEqual([[3], [null]])
  })

  it('lists communities with the rate ratio badge and opens one', async () => {
    const rail = await mount()
    const row = byTestId('graph-community-812')!

    expect(row.textContent).toContain('Community 812')
    expect(row.textContent).toContain('37 members')
    expect(byTestId('graph-community-ratio-812')?.textContent?.trim()).toBe('2.1×')
    expect(byTestId('graph-community-ratio-812')?.classList.contains('badge-error')).toBe(true)

    row.click()

    expect(rail.emitted.openCommunity).toEqual([[community]])
  })

  it('confirms a hub override before emitting it', async () => {
    const rail = await mount({canResolve: true})
    const select = byTestId('graph-hub-override-9') as HTMLSelectElement

    expect(byTestId('graph-hub-9')?.textContent).toContain('480 neighbours')
    expect(select.value).toBe('2')

    select.value = '1'
    select.dispatchEvent(new Event('change'))
    await nextTick()

    expect(rail.emitted.hubOverride).toBeUndefined()
    expect(byTestId('graph-hub-override-dialog')?.textContent).toContain('AA:BB')

    ;(byTestId('graph-hub-override-cancel') as HTMLButtonElement).click()
    await nextTick()

    expect(rail.emitted.hubOverride).toBeUndefined()
    expect((byTestId('graph-hub-override-9') as HTMLSelectElement).value).toBe('2')

    const again = byTestId('graph-hub-override-9') as HTMLSelectElement

    again.value = '1'
    again.dispatchEvent(new Event('change'))
    await nextTick()
    ;(byTestId('graph-hub-override-confirm') as HTMLButtonElement).click()

    expect(rail.emitted.hubOverride).toEqual([[hub, 1]])
  })

  it('shows the empty states when nothing is detected', async () => {
    await mount({patterns: [], communities: [], hubs: []})

    expect(byTestId('graph-patterns-empty')).not.toBeNull()
    expect(byTestId('graph-communities-empty')).not.toBeNull()
    expect(byTestId('graph-hubs-empty')).not.toBeNull()
  })
})
