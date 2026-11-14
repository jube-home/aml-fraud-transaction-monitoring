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

import {afterEach, beforeEach, describe, expect, it} from 'vitest'
import {createMemoryHistory, createRouter} from 'vue-router'
import {useToastStore} from '../../../stores/toasts'
import {
  installDialogStubs,
  item,
  mountWith,
  settle,
  stubApi,
  view,
  type Harness,
  type StubRoute,
} from '../../checklist/__tests__/support'
import {itemState, slaState, type ChecklistOverride} from '../../checklist/types'
import CaseDecide from '../CaseDecide.vue'
import type {CaseDetail} from '../types'

const record = {
  id: 9041,
  caseWorkflowGuid: 'workflow',
  caseWorkflowStatusGuid: 'a',
  locked: true,
  lockedUser: 'alex',
  closedStatusId: 0,
  rating: 0,
  diary: false,
  diaryDate: null,
}

const override: ChecklistOverride = {
  id: 5,
  caseId: 9041,
  reason: 'Verified by phone.',
  grantedUser: 'sam',
  grantedDate: '2026-11-04T10:00:00Z',
  openItems: [],
}

const openView = view([
  item({id: 1, name: 'Contact the customer'}),
  item({id: 2, name: 'Check related cases', stateId: itemState.done}),
])

const breachedView = view([
  item({
    id: 1,
    name: 'Contact the customer',
    slaStateId: slaState.breached,
    minutesRemaining: -5,
  }),
])

const doneView = view([item({id: 1, stateId: itemState.done})])

let harness: Harness | undefined
let stub: ReturnType<typeof stubApi> | undefined

beforeEach(() => installDialogStubs())

afterEach(() => {
  harness?.unmount()
  stub?.restore()
})

async function mount(
  checklist: unknown,
  options: {
    userName?: string
    permitted?: boolean
    detail?: Partial<CaseDetail>
    put?: StubRoute[2]
  } = {},
): Promise<Harness> {
  const routes = [
    ['GET', /^\/api\/CaseChecklist\/9041$/, () => ({status: 200, body: checklist})],
    ['GET', /^\/api\/Case\/9041$/, () => ({status: 200, body: record})],
    ['PUT', /^\/api\/Case$/, options.put ?? (() => ({status: 200, body: record}))],
  ] satisfies StubRoute[]

  stub = stubApi(routes)

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{path: '/', component: {template: '<div></div>'}}],
  })

  await router.push('/')
  await router.isReady()

  return mountWith(
    CaseDecide,
    {
      detail: {...record, ...options.detail} as unknown as CaseDetail,
      row: null,
      userName: options.userName ?? 'alex',
      canOverrideChecklist: options.permitted ?? false,
    },
    [],
    [router],
  )
}

function closeCase(): void {
  harness!.byTestId('case-close')!.click()
}

describe('CaseDecide checklist tile', () => {
  it('replaces the old placeholder boxes', async () => {
    harness = await mount(openView)

    expect(harness.byTestId('case-checklist')).toBeNull()
    expect(harness.root.textContent).not.toContain('Identity verified')
  })

  it('shows progress on a tile when the case has items', async () => {
    harness = await mount(openView)

    expect(harness.byTestId('case-decide-tab-checklist')!.textContent).toContain('1 of 2 done')
  })

  it('goes red when an item has breached its SLA', async () => {
    harness = await mount(breachedView)

    expect(harness.byTestId('case-decide-tab-checklist')!.className).toContain('text-error')
  })

  it('is absent when there is no checklist for the case', async () => {
    harness = await mount(view([]))

    expect(harness.byTestId('case-decide-tab-checklist')).toBeNull()
  })

  it('opens the checklist dialog', async () => {
    harness = await mount(openView)

    harness.byTestId('case-decide-tab-checklist')!.click()
    await settle()

    expect(harness.byTestId('checklist-dialog')!.hasAttribute('open')).toBe(true)
  })
})

describe('CaseDecide close gate', () => {
  it('lists the open items on the close confirmation and disables confirm', async () => {
    harness = await mount(openView)

    closeCase()
    await settle()

    expect(harness.byTestId('case-close-gate-count')!.textContent).toContain(
      '1 checklist item still open',
    )
    expect(harness.byTestId('case-close-gate-names')!.textContent).toContain('Contact the customer')
    expect((harness.byTestId('case-close-confirm') as HTMLButtonElement).disabled).toBe(true)
  })

  it('lets the case be closed when nothing is open', async () => {
    harness = await mount(doneView)

    closeCase()
    await settle()

    expect(harness.byTestId('case-close-gate')).toBeNull()

    const confirm = harness.byTestId('case-close-confirm') as HTMLButtonElement

    expect(confirm.disabled).toBe(false)

    confirm.click()
    await settle()

    const put = stub!.calls.find((call) => call.method === 'PUT')

    expect((put!.body as { closedStatusId: number }).closedStatusId).toBe(3)
  })

  it('lets the case be closed with an override in place', async () => {
    harness = await mount({...openView, override})

    closeCase()
    await settle()

    expect(harness.byTestId('case-close-gate-override')).not.toBeNull()
    expect((harness.byTestId('case-close-confirm') as HTMLButtonElement).disabled).toBe(false)
  })

  it('offers the override to a supervisor who is not the holder and opens its dialog', async () => {
    harness = await mount(openView, {userName: 'sam', permitted: true})

    closeCase()
    await settle()
    harness.byTestId('case-close-gate-override-action')!.click()
    await settle()

    expect(harness.byTestId('checklist-override-dialog')!.hasAttribute('open')).toBe(true)
  })

  it('never offers the override to the holder', async () => {
    harness = await mount(openView, {userName: 'alex', permitted: true})

    closeCase()
    await settle()

    expect(harness.byTestId('case-close-gate')).not.toBeNull()
    expect(harness.byTestId('case-close-gate-override-action')).toBeNull()
  })

  it('opens the checklist from the close confirmation', async () => {
    harness = await mount(openView)

    closeCase()
    await settle()
    harness.byTestId('case-close-gate-open')!.click()
    await settle()

    expect(harness.byTestId('checklist-dialog')!.hasAttribute('open')).toBe(true)
  })

  it('keeps the confirmation open with the server message when the server still refuses', async () => {
    harness = await mount(doneView, {
      put: () => ({
        status: 409,
        body: {
          code: 'ChecklistIncomplete',
          message: 'Complete the checklist first: Contact the customer.',
          openItems: ['Contact the customer'],
        },
      }),
    })

    closeCase()
    await settle()
    ;(harness.byTestId('case-close-confirm') as HTMLButtonElement).click()
    await settle()

    expect(harness.byTestId('case-close-dialog')!.hasAttribute('open')).toBe(true)
    expect(useToastStore(harness.pinia).items.map((toast) => toast.text)).toContain(
      'Complete the checklist first: Contact the customer.',
    )
  })
})
