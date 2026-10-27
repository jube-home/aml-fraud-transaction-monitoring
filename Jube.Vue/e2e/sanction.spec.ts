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
import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { me } from './hierarchyStub'
import { settleAnimations } from './support'

const aggregate = {
  sum: 6.5,
  average: 2.1667,
  count: 3,
  max: 3,
  min: 1,
  first: 1,
  last: 2,
  confidence: 0.8125,
}

const response = {
  entries: [
    {
      id: 11,
      distance: 1,
      value: 'IVAN PETROV',
      sanctionEntrySourceId: 4,
      source: 'OFAC',
      reference: 'SDN-1',
    },
    {
      id: 12,
      distance: 2,
      value: 'IVAN PETROVICH',
      sanctionEntrySourceId: 5,
      source: 'UN',
      reference: 'UN-9',
    },
  ],
  aggregations: {
    total: aggregate,
    bySource: [
      { sourceId: 4, sourceName: 'OFAC', ...aggregate },
      { sourceId: 5, sourceName: 'UN', ...aggregate },
    ],
  },
}

async function stub(
  page: Page,
  requests: URLSearchParams[] = [],
  status = 200,
  body: unknown = response,
) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('**/api/Invoke/Sanction**', (route) => {
    requests.push(new URL(route.request().url()).searchParams)

    return route.fulfill({ status, json: body })
  })
}

test.describe('Sanctions search', () => {
  test.use({ viewport: { width: 1400, height: 1200 } })

  test('asks nothing until there is text, then sends the four parameters and draws all three grids', async ({
    page,
  }) => {
    const requests: URLSearchParams[] = []

    await stub(page, requests)
    await page.goto('/tools?tool=sanction-search')

    await expect(page.getByTestId('sanction-check')).toBeDisabled()
    await expect(page.getByTestId('sanction-prompt')).toBeVisible()
    expect(requests).toHaveLength(0)

    await page.getByTestId('sanction-text').fill('  Ivan Petrov ')
    await page.getByTestId('sanction-distance').fill('3')
    await page.getByTestId('sanction-maxDistanceRatio').fill('0.5')
    await expect(page.getByTestId('sanction-distance-value')).toHaveText('3')
    await page.getByTestId('sanction-check').click()

    await expect(page.getByTestId('sanction-count')).toHaveText('2 matches')
    expect(requests).toHaveLength(1)
    expect(Object.fromEntries(requests[0]!)).toEqual({
      multiPartString: 'Ivan Petrov',
      distance: '3',
      maxDistanceRatio: '0.5',
      maxCoverageRatio: '2',
    })

    const results = page.getByTestId('sanction-results')

    await expect(results.locator('[aria-label="Matches"] .tabulator-row')).toHaveCount(2)
    await expect(results.locator('[aria-label="Total aggregation"] .tabulator-row')).toHaveCount(1)
    await expect(
      results.locator('[aria-label="Aggregation by source"] .tabulator-row'),
    ).toHaveCount(2)
    await expect(results).toContainText('IVAN PETROV')
    await expect(results).toContainText('0.8125')
  })

  test('shows the server refusal reasons rather than a bare failure', async ({ page }) => {
    await stub(page, [], 400, {
      errors: [{ propertyName: 'MultiPartString', errorMessage: 'Multipart string is too long.' }],
    })
    await page.goto('/tools?tool=sanction-search')
    await page.getByTestId('sanction-text').fill('x')
    await page.getByTestId('sanction-check').click()

    await expect(page.getByTestId('sanction-error')).toContainText('The search was refused:')
    await expect(page.getByTestId('sanction-error')).toContainText('Multipart string is too long.')
  })

  test('says so plainly when the search itself fails', async ({ page }) => {
    await stub(page, [], 500, {})
    await page.goto('/tools?tool=sanction-search')
    await page.getByTestId('sanction-text').fill('x')
    await page.getByTestId('sanction-check').click()

    await expect(page.getByTestId('sanction-error')).toContainText('could not be completed')
  })

  test('embedded without its form, the host supplies the parameters and the results draw as on the page', async ({
    page,
  }) => {
    const requests: URLSearchParams[] = []

    await stub(page, requests)
    await page.goto('/tools?tool=sanction-search')
    await page.getByTestId('sanction-embedded').check()

    await expect(page.getByTestId('sanction-check')).toHaveCount(0)
    expect(requests).toHaveLength(0)

    await page.getByTestId('sanction-host-text').fill('Ivan')
    await page.getByTestId('sanction-host-apply').click()

    await expect(page.getByTestId('sanction-count')).toHaveText('2 matches')
    expect(requests).toHaveLength(1)
    expect(requests[0]!.get('multiPartString')).toBe('Ivan')
    expect(requests[0]!.get('distance')).toBe('2')
  })

  test('has no accessibility violations with results showing', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=sanction-search')
    await page.getByTestId('sanction-text').fill('Ivan')
    await page.getByTestId('sanction-check').click()
    await expect(page.getByTestId('sanction-count')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).analyze()

    expect(results.violations).toEqual([])
  })
})
