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

import { expect, test } from '@playwright/test'

test.describe('api and cache', () => {
  test('a successful read is shown', async ({ page }) => {
    await page.route('**/api/AntiforgeryToken', (route) =>
      route.fulfill({ json: { token: 'x'.repeat(32) } }),
    )
    await page.goto('/scaffold/api-demo')

    await expect(page.getByTestId('api-success')).toContainText('32 characters')
  })

  test('a 401 redirects to sign-in and keeps the return address', async ({ page }) => {
    await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ status: 401 }))
    await page.goto('/scaffold/api-demo')

    await expect(page).toHaveURL(/\/\?redirect=\/scaffold\/api-demo/)
    await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  })

  test('a 403 shows not permitted and offers a retry without redirecting', async ({ page }) => {
    await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ status: 403 }))
    await page.goto('/scaffold/api-demo')

    await expect(page.getByTestId('api-error')).toHaveText('Not permitted.')
    await expect(page).toHaveURL(/\/scaffold\/api-demo$/)
  })

  test('a server error is retried for reads and then recovers on demand', async ({ page }) => {
    let calls = 0
    await page.route('**/api/AntiforgeryToken', (route) => {
      calls += 1
      return calls <= 4
        ? route.fulfill({ status: 500 })
        : route.fulfill({ json: { token: 'y'.repeat(10) } })
    })
    await page.goto('/scaffold/api-demo')

    await expect(page.getByTestId('api-error')).toHaveText(
      'The server could not complete the request.',
      {
        timeout: 15000,
      },
    )
    expect(calls).toBe(4)

    await page.getByRole('button', { name: 'Retry' }).click()
    await expect(page.getByTestId('api-success')).toContainText('10 characters')
  })
})
