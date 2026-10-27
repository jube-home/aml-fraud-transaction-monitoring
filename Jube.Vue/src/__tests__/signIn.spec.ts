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

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [1],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

let mounted: MountedVapor | undefined

async function settle(): Promise<void> {
  await nextTick()
  await new Promise((resolve) => setTimeout(resolve, 30))
}

function reply(status: number, body?: unknown): void {
  fetchMock.mockResolvedValueOnce(
    new Response(body === undefined ? null : JSON.stringify(body), { status }),
  )
}

async function mountSignIn(path = '/') {
  const router = createAppRouter(createMemoryHistory())
  const i18n = createI18nInstance()
  await loadLocale(i18n.global, 'en')

  setApiHandlers({})
  await router.push(path)
  await router.isReady()

  mounted = mountVapor(App, {
    plugins: [createPinia(), router, i18n, [VueQueryPlugin, { queryClient: createQueryClient() }]],
  })
  await settle()

  return { router, root: mounted.root }
}

function type(root: HTMLElement, selector: string, value: string): void {
  const input = root.querySelector(selector) as HTMLInputElement

  input.value = value
  input.dispatchEvent(new Event('input', { bubbles: true }))
}

async function submit(root: HTMLElement): Promise<void> {
  ;(root.querySelector('form') as HTMLFormElement).dispatchEvent(
    new Event('submit', { bubbles: true, cancelable: true }),
  )
  await vi.waitFor(() => expect(root.querySelector('button[type="submit"][disabled]')).toBeNull())
  await settle()
}

function stepNames(root: HTMLElement): string[] {
  return Array.from(root.querySelectorAll('.steps .step'), (step) => step.textContent!.trim())
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

describe('the sign-in wizard', () => {
  it('signs straight in when no further step is needed and goes to the return address', async () => {
    const { root, router } = await mountSignIn('/?redirect=/scaffold/grid')

    reply(200, { wirePasswordHash: false })
    reply(200)
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'secret')
    await submit(root)

    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/scaffold/grid'))
  })

  it('walks credentials, verification and the forced password change', async () => {
    const { root, router } = await mountSignIn('/?redirect=/tools')

    expect(stepNames(root)).toEqual(['Credentials'])

    reply(200, { wirePasswordHash: true })
    reply(400, { errors: [{ propertyName: 'Mfa', errorMessage: 'required' }] })
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'secret')
    await submit(root)

    expect(stepNames(root)).toEqual(['Credentials', 'Verification'])
    expect(root.querySelector('h1')?.textContent).toBe('Verify it is you')

    reply(403)
    type(root, '#login-mfa', '123456')
    await submit(root)

    expect(stepNames(root)).toEqual(['Credentials', 'Verification', 'New password'])
    expect(root.querySelector('[data-testid="rules"]')).not.toBeNull()

    reply(200)
    reply(200, me)
    type(root, '#login-new-password', 'Correct-Horse-Battery-9')
    type(root, '#login-repeat-password', 'Correct-Horse-Battery-9')
    await submit(root)

    const signInCall = fetchMock.mock.calls.findLast(
      ([url]) => url === '/api/Authentication/ByUserNamePassword',
    )!
    const body = JSON.parse(signInCall[1]!.body as string) as Record<string, unknown>

    expect(body).toMatchObject({ mfa: '123456', passwordChangeState: true })
    expect(body.newPassword).toHaveLength(64)
    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/tools'), {
      timeout: 10_000,
    })
  })

  it('stays on the password step and explains a mismatch without calling the server', async () => {
    const { root } = await mountSignIn()

    reply(200, { wirePasswordHash: false })
    reply(403)
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'secret')
    await submit(root)

    const calls = fetchMock.mock.calls.length

    type(root, '#login-new-password', 'one')
    type(root, '#login-repeat-password', 'two')
    await submit(root)

    expect(root.querySelector('[data-testid="login-problem"]')?.textContent).toContain(
      'do not match',
    )
    expect(fetchMock.mock.calls.length).toBe(calls)
    expect(root.querySelector('[data-testid="rules"]')).toBeNull()
  })

  it('shows a generic message on invalid credentials and clears the password', async () => {
    const { root } = await mountSignIn()

    reply(200, { wirePasswordHash: false })
    reply(401)
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'wrong')
    await submit(root)

    expect(root.querySelector('[data-testid="login-problem"]')?.textContent).toContain(
      'not accepted',
    )
    expect((root.querySelector('#login-password') as HTMLInputElement).value).toBe('')
    expect(root.querySelector('h1')?.textContent).toBe('Sign in')
  })

  it('asks the server for the wire password scheme again on every credentials attempt', async () => {
    const { root } = await mountSignIn()

    reply(200, { wirePasswordHash: false })
    reply(401)
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'wrong')
    await submit(root)

    reply(200, { wirePasswordHash: true })
    reply(401)
    type(root, '#login-user-name', 'sam')
    type(root, '#login-password', 'right')
    await submit(root)

    const urls = fetchMock.mock.calls.map(([url]) => url)

    expect(urls.filter((url) => url === '/api/Authentication/WirePasswordHash')).toHaveLength(2)

    const second = fetchMock.mock.calls.findLast(
      ([url]) => url === '/api/Authentication/ByUserNamePassword',
    )!
    const body = JSON.parse(second[1]!.body as string) as { password: string }

    expect(body.password).toMatch(/^[0-9a-f]{64}$/)
  })

  it('starts again from the verification step', async () => {
    const { root } = await mountSignIn()

    reply(200, { wirePasswordHash: false })
    reply(400, { errors: [{ propertyName: 'Mfa', errorMessage: 'required' }] })
    type(root, '#login-user-name', 'alex')
    type(root, '#login-password', 'secret')
    await submit(root)
    Array.from(root.querySelectorAll('button'))
      .find((button) => button.textContent?.includes('Start again'))!
      .click()
    await settle()

    expect(stepNames(root)).toEqual(['Credentials'])
  })
})
