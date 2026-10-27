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
import { mountVapor, type MountedVapor } from '../test-support/mountVapor'

const fetchMock = vi.fn<typeof fetch>()

let mounted: MountedVapor | undefined

async function until(condition: () => boolean, limit = 5000): Promise<void> {
  const deadline = Date.now() + limit

  while (!condition() && Date.now() < deadline) {
    await new Promise((resolve) => setTimeout(resolve, 20))
  }
}

async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 30))
}

async function mountApp(path: string) {
  const router = createAppRouter(createMemoryHistory())
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  setApiHandlers({
    onUnauthorized: () => {
      void router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
    },
  })

  await router.push(path)
  await router.isReady()

  mounted = mountVapor(App, {
    plugins: [createPinia(), router, i18n, [VueQueryPlugin, { queryClient: createQueryClient() }]],
  })
  await settle()

  return { router, i18n, root: mounted.root }
}

beforeEach(() => {
  localStorage.clear()
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

describe('the app under Vapor with every runtime dependency', () => {
  it('renders the home page through router, pinia and i18n', async () => {
    const { root } = await mountApp('/scaffold')

    expect(root.querySelector('h1')?.textContent).toBe('Jube Vue scaffolding')
    expect(root.querySelector('[data-testid="card-intl"]')?.textContent).toMatch(/GBP\s12,500\.50/)
    expect(root.querySelector('[data-testid="card-intl"]')?.textContent).toContain('3 cases')
    expect(root.querySelector('[data-testid="card-pinia"]')?.textContent).toContain('system')
  })

  it('navigates between pages by router link', async () => {
    const { root, router } = await mountApp('/scaffold')

    ;(root.querySelector('nav a[href="/scaffold/grid"]') as HTMLAnchorElement).click()

    await vi.waitFor(() => expect(router.currentRoute.value.name).toBe('grid'))
    await vi.waitFor(() => expect(root.querySelector('h1')?.textContent).toBe('Data grid'))
  })

  it('opens on the sign-in page by default', async () => {
    const { root, router } = await mountApp('/')

    expect(router.currentRoute.value.name).toBe('login')
    expect(root.querySelector('h1')?.textContent).toBe('Sign in')
    expect(root.querySelector('nav')).toBeNull()
  })

  it('keeps the old /login address working', async () => {
    const { router } = await mountApp('/login?redirect=/scaffold/grid')

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/scaffold/grid')
  })

  it('shows the not-found page for an unknown path', async () => {
    const { root } = await mountApp('/nowhere/at/all')

    expect(root.querySelector('h1')?.textContent).toBe('Page not found')
  })

  it('switches theme through the store', async () => {
    const { root } = await mountApp('/scaffold')
    root.querySelector<HTMLButtonElement>('[data-testid="theme-dark"]')!.click()
    await settle()

    expect(document.documentElement.dataset.theme).toBe('jube-dark')
    expect(localStorage.getItem('jube.theme')).toBe('dark')
  })

  it('switches language and re-renders translated text', async () => {
    const { root } = await mountApp('/scaffold')
    root.querySelector<HTMLButtonElement>('[data-testid="locale-pl"]')!.click()
    await until(() => root.querySelector('h1')?.textContent === 'Szkielet Jube Vue')

    expect(root.querySelector('h1')?.textContent).toBe('Szkielet Jube Vue')
    expect(document.documentElement.lang).toBe('pl')
  })

  it('reads through vue-query and useApi', async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ token: 'z'.repeat(12) }), { status: 200 }),
    )

    const { root } = await mountApp('/scaffold/api-demo')

    expect(root.querySelector('[data-testid="api-success"]')?.textContent).toContain(
      '12 characters',
    )
  })

  it('redirects to the login route with the return address on a 401', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 401 }))

    const { root, router } = await mountApp('/scaffold/api-demo')
    await settle()

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/scaffold/api-demo')
    expect(root.querySelector('h1')?.textContent).toBe('Sign in')
  })

  it('shows not permitted on a 403 without leaving the page', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 403 }))

    const { root, router } = await mountApp('/scaffold/api-demo')

    expect(router.currentRoute.value.name).toBe('api')
    expect(root.querySelector('[data-testid="api-error"]')?.textContent).toBe('Not permitted.')
  })
})
