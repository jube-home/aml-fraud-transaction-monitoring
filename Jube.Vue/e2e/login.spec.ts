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

import { expect, test, type Page } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'
import { settleAnimations, setTheme } from './support'

interface Script {
  wirePasswordHash?: boolean
  methods?: { negotiate: boolean; oauth: boolean } | 'fails'
  negotiate?: { status: number }
  negotiateMfa?: { status: number }
  responses: { status: number; json?: unknown }[]
}

async function stubAuthentication(page: Page, script: Script): Promise<unknown[]> {
  const bodies: unknown[] = []
  let next = 0

  await page.route('**/api/Me', (route) =>
    route.fulfill({
      json: {
        userName: 'alex',
        landlord: false,
        permissions: [1],
        canChangePassword: true,
        sessionExpiry: null,
      },
    }),
  )
  await page.route(/\/api\/AskJoobyConversation(\?.*)?$/, (route) => route.fulfill({ json: [] }))
  await page.route('**/api/AskJoobyConversation/StartupStats', (route) =>
    route.fulfill({ json: { bites: 1, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 } }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/Authentication/Methods', (route) =>
    script.methods === 'fails'
      ? route.fulfill({ status: 500 })
      : route.fulfill({ json: script.methods ?? { negotiate: false, oauth: false } }),
  )
  await page.route('**/api/Authentication/ByNegotiate', (route) =>
    route.fulfill({ status: script.negotiate?.status ?? 401 }),
  )
  await page.route('**/api/Authentication/ByNegotiateMfa', (route) => {
    bodies.push(route.request().postDataJSON())

    return route.fulfill({ status: script.negotiateMfa?.status ?? 200 })
  })
  await page.route('**/api/Authentication/WirePasswordHash', (route) =>
    route.fulfill({ json: { wirePasswordHash: script.wirePasswordHash ?? true } }),
  )
  await page.route('**/api/Authentication/ByUserNamePassword', (route) => {
    bodies.push(route.request().postDataJSON())
    const response = script.responses[Math.min(next, script.responses.length - 1)]!
    next += 1

    return route.fulfill({ status: response.status, json: response.json })
  })

  return bodies
}

const mfaRequired = {
  status: 400,
  json: { errors: [{ propertyName: 'Mfa', errorMessage: "'Mfa' must not be empty." }] },
}

async function enterCredentials(page: Page): Promise<void> {
  await page.getByLabel('User name').fill('alex')
  await page.getByLabel('Password', { exact: true }).fill('a-long-secret')
  await page.getByRole('button', { name: 'Sign in' }).click()
}

test.describe('sign in', () => {
  test('is the default page and carries no scaffolding navigation', async ({ page }) => {
    await page.goto('/')

    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
    await expect(page.getByRole('navigation')).toHaveCount(0)
    await expect(page.getByLabel('User name')).toBeVisible()
  })

  test('signs in and returns to the address that was asked for', async ({ page }) => {
    await stubAuthentication(page, { responses: [{ status: 200 }] })
    await page.goto('/?redirect=/scaffold/grid')
    await enterCredentials(page)

    await expect(page).toHaveURL(/\/scaffold\/grid$/)
  })

  test('sends a user with Ask Jooby access straight to it after sign-in', async ({ page }) => {
    await stubAuthentication(page, { responses: [{ status: 200 }] })
    await page.route('**/api/Me', (route) =>
      route.fulfill({
        json: {
          userName: 'alex',
          landlord: false,
          permissions: [65],
          tenantRegistryId: 1,
          canChangePassword: true,
          askJoobyInference: true,
          sessionExpiry: null,
        },
      }),
    )
    await page.route(/\/api\/AskJoobyConversation(\?.*)?$/, (route) => route.fulfill({ json: [] }))
    await page.route('**/api/AskJoobyConversation/StartupStats', (route) =>
      route.fulfill({ json: { bites: 1, trails: 2, saltlicks: 3, joobriks: 4, yokes: 5 } }),
    )
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
    )
    await page.goto('/')
    await enterCredentials(page)

    await expect(page).toHaveURL(/\/ask-jooby$/)
    await expect(page.getByTestId('empty-welcome')).toContainText('What brings you in?')
  })

  test('hands over to the identity provider when OAuth is on', async ({ page }) => {
    await stubAuthentication(page, { methods: { negotiate: false, oauth: true }, responses: [] })

    const challenge = page.waitForRequest(
      /\/api\/Authentication\/OAuth\?redirectUrl=%2Fpreservation%2Fmove$/,
    )

    await page.route('**/api/Authentication/OAuth**', (route) =>
      route.fulfill({ contentType: 'text/html', body: '<p>identity provider</p>' }),
    )
    await page.goto('/?redirect=/preservation/move')

    await challenge
    await expect(page.getByText('identity provider')).toBeVisible()
  })

  test('signs a Windows user straight in when Negotiate succeeds', async ({ page }) => {
    await stubAuthentication(page, {
      methods: { negotiate: true, oauth: false },
      negotiate: { status: 200 },
      responses: [],
    })
    await page.goto('/')

    await expect(page).toHaveURL(/\/ask-jooby$/)
  })

  test('asks only for the verification code when Negotiate needs one', async ({ page }) => {
    const bodies = await stubAuthentication(page, {
      methods: { negotiate: true, oauth: false },
      negotiate: { status: 202 },
      responses: [],
    })
    await page.goto('/')

    await expect(page.getByLabel('Verification code')).toBeVisible()
    await expect(page.getByLabel('User name')).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Back' })).toHaveCount(0)

    await page.getByLabel('Verification code').fill('123456')
    await page.getByRole('button', { name: 'Verify' }).click()

    await expect(page).toHaveURL(/\/ask-jooby$/)
    expect(bodies).toEqual([{ mfa: '123456' }])
  })

  test('says so when the Negotiate verification code is wrong', async ({ page }) => {
    await stubAuthentication(page, {
      methods: { negotiate: true, oauth: false },
      negotiate: { status: 202 },
      negotiateMfa: { status: 401 },
      responses: [],
    })
    await page.goto('/')
    await page.getByLabel('Verification code').fill('000000')
    await page.getByRole('button', { name: 'Verify' }).click()

    await expect(page.getByTestId('login-problem')).toBeVisible()
    await expect(page.getByLabel('Verification code')).toHaveValue('')
  })

  test('explains when Negotiate does not sign the user in, as a password would not work', async ({
    page,
  }) => {
    await stubAuthentication(page, {
      methods: { negotiate: true, oauth: false },
      negotiate: { status: 401 },
      responses: [],
    })
    await page.goto('/')

    await expect(page.getByTestId('login-blocked')).toContainText('does not accept a password')
    await expect(page.getByLabel('User name')).toHaveCount(0)
    await expect(page.getByTestId('login-retry')).toBeVisible()
  })

  test('shows the form when the sign-in methods cannot be read', async ({ page }) => {
    await stubAuthentication(page, { methods: 'fails', responses: [] })
    await page.goto('/')

    await expect(page.getByLabel('User name')).toBeVisible()
  })

  test('ignores an unsafe return address', async ({ page }) => {
    await stubAuthentication(page, { responses: [{ status: 200 }] })
    await page.goto('/?redirect=//evil.example')
    await enterCredentials(page)

    await expect(page).toHaveURL(/\/ask-jooby$/)
  })

  test('hashes the password and never sends it in the clear', async ({ page }) => {
    const bodies = await stubAuthentication(page, { responses: [{ status: 200 }] })
    await page.goto('/')
    await enterCredentials(page)
    await expect(page).toHaveURL(/\/ask-jooby$/)

    const body = bodies[0] as { password: string }

    expect(body.password).toMatch(/^[0-9a-f]{64}$/)
  })

  test('shows a generic message for a wrong password and keeps the user name', async ({ page }) => {
    await stubAuthentication(page, { responses: [{ status: 401 }] })
    await page.goto('/')
    await enterCredentials(page)

    await expect(page.getByRole('alert')).toContainText('The details were not accepted')
    await expect(page.getByLabel('User name')).toHaveValue('alex')
    await expect(page.getByLabel('Password', { exact: true })).toHaveValue('')
    await expect(page.getByLabel('Password', { exact: true })).toBeFocused()
  })

  test('reveals the password on request and hides it again', async ({ page }) => {
    await page.goto('/')
    const password = page.getByLabel('Password', { exact: true })

    await password.fill('visible-secret')
    await expect(password).toHaveAttribute('type', 'password')

    await page.getByRole('button', { name: 'Show password' }).click()
    await expect(password).toHaveAttribute('type', 'text')

    await page.getByRole('button', { name: 'Hide password' }).click()
    await expect(password).toHaveAttribute('type', 'password')
  })

  test('asks for the verification code only when the server needs one', async ({ page }) => {
    const bodies = await stubAuthentication(page, {
      responses: [mfaRequired, { status: 401 }, { status: 200 }],
    })
    await page.goto('/')
    await enterCredentials(page)

    await expect(page.getByRole('heading', { level: 1, name: 'Verify it is you' })).toBeVisible()
    await expect(page.getByLabel('Verification code')).toBeFocused()

    await page.getByLabel('Verification code').fill('000000')
    await page.getByRole('button', { name: 'Verify and continue' }).click()
    await expect(page.getByRole('alert')).toContainText('The details were not accepted')
    await expect(page.getByLabel('Verification code')).toHaveValue('')

    await page.getByLabel('Verification code').fill('123456')
    await page.getByRole('button', { name: 'Verify and continue' }).click()
    await expect(page).toHaveURL(/\/ask-jooby$/)
    expect((bodies.at(-1) as { mfa: string }).mfa).toBe('123456')
  })

  test('takes the user through a forced password change after verification', async ({ page }) => {
    const bodies = await stubAuthentication(page, {
      responses: [mfaRequired, { status: 403 }, { status: 200 }],
    })
    await page.goto('/')
    await enterCredentials(page)
    await page.getByLabel('Verification code').fill('123456')
    await page.getByRole('button', { name: 'Verify and continue' }).click()

    await expect(
      page.getByRole('heading', { level: 1, name: 'Choose a new password' }),
    ).toBeVisible()
    await expect(page.getByRole('listitem').filter({ hasText: 'Verification' })).toBeVisible()

    const submit = page.getByRole('button', { name: 'Change password and sign in' })
    await page.getByLabel('New password', { exact: true }).fill('short')
    await page.getByLabel('Confirm new password').fill('short')
    await submit.click()
    await expect(page.getByRole('alert')).toContainText('does not meet every requirement')

    await page.getByLabel('New password', { exact: true }).fill('Correct-Horse-Battery-9')
    await page.getByLabel('Confirm new password').fill('Different-Horse-Battery-9')
    await submit.click()
    await expect(page.getByRole('alert')).toContainText('do not match')

    await page.getByLabel('Confirm new password').fill('Correct-Horse-Battery-9')
    await submit.click()
    await expect(page).toHaveURL(/\/ask-jooby$/)

    const last = bodies.at(-1) as { passwordChangeState: boolean; newPassword: string; mfa: string }

    expect(last).toMatchObject({ passwordChangeState: true, mfa: '123456' })
    expect(last.newPassword).toMatch(/^[0-9a-f]{64}$/)
  })

  test('the legacy /login address still opens the wizard', async ({ page }) => {
    await page.goto('/login?redirect=/scaffold/theme')

    await expect(page).toHaveURL(/\/\?redirect=\/scaffold\/theme$/)
    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`no axe violations on any step in ${theme}`, async ({ page }) => {
      await stubAuthentication(page, { responses: [mfaRequired, { status: 403 }, { status: 403 }] })
      await page.goto('/')
      await setTheme(page, theme)

      const scan = async (): Promise<void> => {
        await settleAnimations(page)
        expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
      }

      await scan()

      await enterCredentials(page)
      await expect(page.getByLabel('Verification code')).toBeVisible()
      await scan()

      await page.getByLabel('Verification code').fill('123456')
      await page.getByRole('button', { name: 'Verify and continue' }).click()
      await expect(page.getByLabel('Confirm new password')).toBeVisible()
      await page.getByLabel('New password', { exact: true }).fill('Abc')
      await scan()
    })
  }
})
