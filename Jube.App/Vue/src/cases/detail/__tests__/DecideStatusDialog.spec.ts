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
import {itemState, type ChecklistOverride} from '../../checklist/types'
import DecideStatusDialog from '../DecideStatusDialog.vue'
import type {CaseDetail} from '../types'

const statuses = [
  {guid: 'a', name: 'First Line Review', priority: 5},
  {guid: 'b', name: 'Supervisor Review', priority: 4},
]

const record = {
  id: 9041,
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
  item({id: 2, name: 'Check related cases'}),
])

const doneView = view([item({id: 1, stateId: itemState.done})])

let harness: Harness | undefined
let stub: ReturnType<typeof stubApi> | undefined

beforeEach(() => installDialogStubs())

afterEach(() => {
  harness?.unmount()
  stub?.restore()
})

function routes(checklist: unknown, put: StubRoute[2] = () => ({status: 200, body: record})) {
  return [
    ['GET', /^\/api\/CaseChecklist\/9041$/, () => ({status: 200, body: checklist})],
    ['GET', /^\/api\/Case\/9041$/, () => ({status: 200, body: record})],
    ['PUT', /^\/api\/Case$/, put],
  ] satisfies StubRoute[]
}

async function mount(
  checklist: unknown,
  options: { userName?: string; permitted?: boolean; put?: StubRoute[2] } = {},
): Promise<Harness> {
  stub = stubApi(routes(checklist, options.put))

  return mountWith(
    DecideStatusDialog,
    {
      open: true,
      detail: {...record} as unknown as CaseDetail,
      statuses,
      editable: true,
      userName: options.userName ?? 'alex',
      canOverrideChecklist: options.permitted ?? false,
    },
    ['close', 'openChecklist', 'overrideChecklist'],
  )
}

function choose(guid: string): void {
  ;(harness!.byTestId(`case-status-option-${guid}`) as HTMLInputElement).click()
}

function move(): HTMLButtonElement {
  return harness!.byTestId('case-status-move') as HTMLButtonElement
}

describe('DecideStatusDialog checklist gate', () => {
  it('warns up front, names the open items and disables the move', async () => {
    harness = await mount(openView)

    choose('b')
    await settle()

    expect(harness.byTestId('case-status-gate-count')!.textContent).toContain(
      '2 checklist items still open',
    )
    expect(harness.byTestId('case-status-gate-names')!.textContent).toContain(
      'Contact the customer',
    )
    expect(move().disabled).toBe(true)
    expect((harness.byTestId('case-status-escalate') as HTMLButtonElement).disabled).toBe(true)
  })

  it('opens the checklist from the banner', async () => {
    harness = await mount(openView)

    harness.byTestId('case-status-gate-open')!.click()

    expect(harness.mounted.emitted.openChecklist).toHaveLength(1)
  })

  it('lets the move through when everything required is done', async () => {
    harness = await mount(doneView)

    choose('b')
    await settle()

    expect(harness.byTestId('case-status-gate')).toBeNull()
    expect(move().disabled).toBe(false)

    move().click()
    await settle()

    const put = stub!.calls.find((call) => call.method === 'PUT')

    expect((put!.body as { caseWorkflowStatusGuid: string }).caseWorkflowStatusGuid).toBe('b')
    expect(harness.mounted.emitted.close).toHaveLength(1)
  })

  it('lets the move through while an override is in place and says so', async () => {
    harness = await mount({...openView, override})

    choose('b')
    await settle()

    expect(harness.byTestId('case-status-gate-override')!.textContent).toContain('sam')
    expect(move().disabled).toBe(false)
  })

  it('offers the override to a supervisor who does not hold the case', async () => {
    harness = await mount(openView, {userName: 'sam', permitted: true})

    harness.byTestId('case-status-gate-override-action')!.click()

    expect(harness.mounted.emitted.overrideChecklist).toHaveLength(1)
  })

  it('never offers the override to the holder, even with the permission', async () => {
    harness = await mount(openView, {userName: 'alex', permitted: true})

    expect(harness.byTestId('case-status-gate')).not.toBeNull()
    expect(harness.byTestId('case-status-gate-override-action')).toBeNull()
  })

  it('never offers the override without the permission', async () => {
    harness = await mount(openView, {userName: 'sam', permitted: false})

    expect(harness.byTestId('case-status-gate-override-action')).toBeNull()
  })

  it('shows the server list when it refuses a move the page thought was allowed', async () => {
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

    choose('b')
    await settle()

    const before = stub!.calls.filter((call) => call.path === '/api/CaseChecklist/9041').length

    move().click()
    await settle()

    const toasts = useToastStore(harness.pinia).items

    expect(toasts.map((toast) => toast.text)).toContain(
      'Complete the checklist first: Contact the customer.',
    )
    expect(harness.mounted.emitted.close).toBeUndefined()
    expect(
      stub!.calls.filter((call) => call.path === '/api/CaseChecklist/9041').length,
    ).toBeGreaterThan(before)
  })

  it('reports any other failure with the standard message', async () => {
    harness = await mount(doneView, {put: () => ({status: 500})})

    choose('b')
    await settle()
    move().click()
    await settle()

    expect(useToastStore(harness.pinia).items.map((toast) => toast.kind)).toEqual(['error'])
    expect(harness.mounted.emitted.close).toBeUndefined()
  })
})
