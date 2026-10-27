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

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { createPinia } from 'pinia'
import { createMemoryHistory } from 'vue-router'
import { VueQueryPlugin } from '@tanstack/vue-query'
import App from '../App.vue'
import { createAppRouter } from '../router'
import { createI18nInstance, loadLocale } from '../i18n'
import { createQueryClient } from '../api/queryClient'
import { resetAntiforgeryToken, setApiHandlers } from '../api/useApi'
import { forgetSessionExpiry, noteSessionExpiry } from '../auth/sessionExpiry'
import { mountVapor, type MountedVapor } from '../test-support/mountVapor'

vi.setConfig({ testTimeout: 30_000 })

const fetchMock = vi.fn<typeof fetch>()

let mounted: MountedVapor | undefined

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [1, 6, 29],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 30))
}

function reply(status: number, body?: unknown): void {
  fetchMock.mockResolvedValueOnce(
    new Response(body === undefined ? null : JSON.stringify(body), { status }),
  )
}

async function mountApp(path: string) {
  const client = createQueryClient()
  const router = createAppRouter(createMemoryHistory(), client)
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  setApiHandlers({})
  await router.push(path)
  await router.isReady()

  mounted = mountVapor(App, {
    plugins: [createPinia(), router, i18n, [VueQueryPlugin, { queryClient: client }]],
  })
  await settle()

  return { router, root: mounted.root }
}

function menuLabels(root: HTMLElement): string[] {
  return Array.from(root.querySelectorAll('nav[aria-label] a'), (a) => a.textContent!.trim())
}

beforeEach(() => {
  localStorage.clear()
  forgetSessionExpiry()
  vi.stubGlobal('fetch', fetchMock)
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockReturnValue({
      matches: false,
      addEventListener: vi.fn<() => void>(),
      removeEventListener: vi.fn<() => void>(),
    }),
  )
  resetAntiforgeryToken()
})

afterEach(() => {
  mounted?.unmount()
  mounted = undefined
  fetchMock.mockReset()
  vi.unstubAllGlobals()
})

describe('the signed-in shell', () => {
  it('renders the shell with a menu limited to the permissions held', async () => {
    reply(200, me)
    const { root } = await mountApp('/tools')

    expect(root.querySelector('h1')?.textContent).toBe('Tools workbench')
    expect(menuLabels(root)).toEqual(['Ask Jooby', 'Cases', 'Models', 'Sanctions', 'TEMP: Tools'])
    expect(root.querySelector('[data-testid="account-button"]')?.textContent).toContain('alex')
  })

  it('sends an anonymous visitor to sign-in with the return address', async () => {
    reply(401)
    const { router, root } = await mountApp('/tools')

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/tools')
    expect(root.querySelector('h1')?.textContent).toBe('Sign in')
  })

  it('links menu entries to their pages', async () => {
    reply(200, me)
    const { root } = await mountApp('/tools')

    const hrefs = Array.from(root.querySelectorAll('nav[aria-label] a'), (a) =>
      a.getAttribute('href'),
    )

    expect(hrefs[1]).toBe('/cases')
  })

  it('signs out, clears the session and returns to sign-in', async () => {
    reply(200, me)
    const { router, root } = await mountApp('/tools')

    reply(200)
    const signOut = Array.from(root.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Sign out'),
    )!
    signOut.click()

    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('login'))
    expect(fetchMock.mock.calls.map((call) => call[0])).toContain('/api/Authentication/Logout')
  })

  it('stays put and says so when sign-out cannot be completed', async () => {
    reply(200, me)
    const { router, root } = await mountApp('/tools')

    reply(500)
    Array.from(root.querySelectorAll('button'))
      .find((b) => b.textContent?.includes('Sign out'))!
      .click()
    await settle()

    expect(router.currentRoute.value.name).toBe('tools')
    expect(root.querySelector('[data-testid="toasts"]')?.textContent).toContain('Sign-out failed')
  })

  it('returns to sign-in with a notice when the session expires', async () => {
    reply(200, me)
    const { router, root } = await mountApp('/tools')

    noteSessionExpiry(new Date(Date.now() + 40).toISOString())

    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('login'))
    expect(router.currentRoute.value.query.redirect).toBe('/tools')
    expect(root.querySelector('[data-testid="toasts"]')?.textContent).toContain(
      'session has expired',
    )
  })
})
