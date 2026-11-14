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

import {nextTick, type Plugin} from 'vue'
import {createPinia, type Pinia} from 'pinia'
import {QueryClient, VueQueryPlugin} from '@tanstack/vue-query'
import {createI18nInstance, loadLocale} from '../../../i18n'
import {mountVapor, type MountedVapor} from '../../../test-support/mountVapor'
import {itemState, slaState, type ChecklistItem, type ChecklistView} from '../types'

export function item(patch: Partial<ChecklistItem> = {}): ChecklistItem {
  return {
    id: 1,
    caseWorkflowChecklistItemGuid: '00000000-0000-0000-0000-000000000001',
    name: 'Record an investigation note',
    priority: 2,
    stateId: itemState.shown,
    firstShownDate: '2026-11-04T09:00:00Z',
    shownDate: '2026-11-04T09:00:00Z',
    dueDate: null,
    slaBreachedDate: null,
    completedDate: null,
    completedUser: null,
    slaStateId: slaState.none,
    minutesRemaining: null,
    ...patch,
  }
}

export function view(items: ChecklistItem[], patch: Partial<ChecklistView> = {}): ChecklistView {
  const done = items.filter((candidate) => candidate.stateId === itemState.done).length
  const open = items.length - done

  return {
    caseId: 9041,
    checklists: [
      {
        caseWorkflowChecklistGuid: '11111111-1111-1111-1111-111111111111',
        name: 'Initial Triage',
        items,
      },
    ],
    required: items.length,
    done,
    open,
    breached: items.filter((candidate) => candidate.slaStateId === slaState.breached).length,
    reconciledDate: '2026-11-04T09:00:00Z',
    override: null,
    complete: open === 0,
    ...patch,
  }
}

export interface Harness {
  mounted: MountedVapor
  client: QueryClient
  pinia: Pinia
  root: HTMLElement
  byTestId: (id: string) => HTMLElement | null
  settle: () => Promise<void>
  unmount: () => void
}

export async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 20))
  await nextTick()
}

export async function mountWith(
  component: object,
  props: Record<string, unknown>,
  events: string[] = [],
  extraPlugins: Plugin[] = [],
): Promise<Harness> {
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  const client = new QueryClient({
    defaultOptions: {queries: {retry: false}, mutations: {retry: false}},
  })

  const pinia = createPinia()

  const mounted = mountVapor(component, {
    props,
    events,
    plugins: [pinia, i18n, [VueQueryPlugin, {queryClient: client}], ...extraPlugins],
  })

  await settle()

  return {
    mounted,
    client,
    pinia,
    root: mounted.root,
    byTestId: (id) => mounted.root.querySelector(`[data-testid="${id}"]`),
    settle,
    unmount: () => mounted.unmount(),
  }
}

export function installDialogStubs(): void {
  HTMLDialogElement.prototype.showModal = function showModal() {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function close() {
    this.removeAttribute('open')
  }
}

export interface StubbedCall {
  method: string
  path: string
  body: unknown
}

export type StubRoute = [
  method: string,
  pattern: RegExp,
  reply: (call: StubbedCall) => { status: number; body?: unknown },
]

export function stubApi(routes: StubRoute[]): { calls: StubbedCall[]; restore: () => void } {
  const calls: StubbedCall[] = []

  const original = globalThis.fetch

  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const method = (init?.method ?? 'GET').toUpperCase()
    const path = String(input)
    const body = typeof init?.body === 'string' ? (JSON.parse(init.body) as unknown) : undefined
    const call = {method, path, body}

    if (path !== '/api/AntiforgeryToken') calls.push(call)

    if (path === '/api/AntiforgeryToken') {
      return new Response(JSON.stringify({token: 'token'}), {status: 200})
    }

    const route = routes.find(([m, pattern]) => m === method && pattern.test(path))
    const result = route ? route[2](call) : {status: 200, body: []}

    return new Response(result.body === undefined ? null : JSON.stringify(result.body), {
      status: result.status,
    })
  }) as typeof fetch

  return {
    calls,
    restore: () => {
      globalThis.fetch = original
    },
  }
}
