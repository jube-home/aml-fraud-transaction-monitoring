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
import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {me} from './hierarchyStub'

const guid = '11111111-1111-1111-1111-111111111111'

interface Put {
  path: string
  body: Record<string, unknown>
}

async function stub(page: Page, puts: Put[] = [], putStatus = 200) {
  const models = [
    {
      name: 'Payments',
      entityAnalysisModelGuid: guid,
      hasOverride: false,
      overrideKind: 0,
      deleteExpiryDate: null,
    },
  ]

  const rules = [
    {
      name: 'High value',
      hasOverride: true,
      entityAnalysisModelActivationRuleOverrideId: 7,
      entityAnalysisModelGuid: guid,
      enableForce: false,
      overrideKind: 0,
      deleteExpiryDate: null,
    },
    {
      name: 'Blacklist',
      hasOverride: true,
      entityAnalysisModelActivationRuleOverrideId: 8,
      entityAnalysisModelGuid: guid,
      enableForce: true,
      overrideKind: 0,
      deleteExpiryDate: null,
    },
  ]

  await page.route('**/api/Me', (route) => route.fulfill({json: {...me, permissions: [2, 62]}}))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/TenantRegistry/1', (route) =>
    route.fulfill({json: {id: 1, name: 'Tenant one'}}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/EntityAnalysisModelRequestXPath/ByOverrideKey', (route) =>
    route.fulfill({json: [{name: 'AccountId'}]}),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery/Keys', (route) =>
    route.fulfill({
      json: [
        {
          overrideKey: 'AccountId',
          enabledOnModels: 1,
          values: 2,
          overrides: 3,
          forced: 1,
          activationRules: 2,
          allActivationRules: 4,
          nextExpiryDate: null,
          lastCreatedDate: '2026-10-20T10:00:00Z',
        },
      ],
    }),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery/Values**', (route) =>
    route.fulfill({
      json: [
        {
          overrideKeyValue: 'ACC-1',
          name: 'Payments',
          entityAnalysisModelGuid: guid,
          overrideKind: 1,
          createdUser: 'Administrator',
          createdDate: '2026-10-20T10:00:00Z',
          deleteExpiryDate: null,
        },
      ],
    }),
  )
  await page.route('**/api/GetEntityAnalysisModelOverrideQuery?**', (route) =>
    route.fulfill({json: models}),
  )
  await page.route('**/api/GetEntityAnalysisModelActivationRuleOverrideQuery**', (route) =>
    route.fulfill({json: rules}),
  )
  await page.route('**/api/EntityAnalysisModelOverride**', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    const body = route.request().postDataJSON() as Record<string, unknown>

    puts.push({path: new URL(route.request().url()).pathname, body})

    if (putStatus === 200 && typeof body.active === 'boolean') {
      models[0]!.hasOverride = body.active
    }

    return route.fulfill({
      status: putStatus,
      json:
        putStatus === 200 ? {} : {errors: [{propertyName: 'x', errorMessage: 'Not allowed.'}]},
    })
  })
  await page.route('**/api/EntityAnalysisModelActivationRuleOverride**', (route) => {
    if (route.request().method() !== 'PUT') return route.fallback()

    puts.push({
      path: new URL(route.request().url()).pathname,
      body: route.request().postDataJSON() as Record<string, unknown>,
    })

    return route.fulfill({status: putStatus, json: putStatus === 200 ? {} : {}})
  })
}

async function toModels(page: Page) {
  await page.goto('/override?key=AccountId&value=ACC-1')
  await expect(page.getByText('Payments').first()).toBeVisible()
}

test.describe('Overrides', () => {
  test.use({viewport: {width: 1400, height: 1100}})

  test('switching a model on sends the change', async ({page}) => {
    const puts: Put[] = []

    await stub(page, puts)
    await toModels(page)

    const toggle = page.locator('[aria-label="Models"] .tabulator-row input[type=checkbox]').first()

    await toggle.check()

    await expect.poll(() => puts.length).toBe(1)
    expect(puts[0]).toEqual({
      path: '/api/EntityAnalysisModelOverride',
      body: {
        overrideKey: 'AccountId',
        overrideKeyValue: 'ACC-1',
        entityAnalysisModelGuid: guid,
        active: true,
      },
    })
  })

  test('an expiry in the past is refused in the browser and never sent', async ({page}) => {
    const puts: Put[] = []

    await stub(page, puts)
    await toModels(page)

    const row = page.locator('[aria-label="Models"] .tabulator-row').first()

    await row.locator('input[type=checkbox]').check()
    await expect.poll(() => puts.length).toBe(1)

    const expiry = row.locator('input[type=datetime-local]')

    await expect(expiry).toBeEnabled()
    await expiry.fill('2020-01-01T10:00')

    await expect(page.getByTestId('override-problems')).toContainText('must be later than')
    expect(puts).toHaveLength(1)

    await expiry.fill('2999-01-01T10:00')

    await expect.poll(() => puts.length).toBe(2)
    expect(puts[1]!.path).toBe('/api/EntityAnalysisModelOverride/DeleteExpiryDate')
    expect(String(puts[1]!.body.deleteExpiryDate)).toMatch(/^2999-/)
  })

  test('a model opens onto its activation rules, offering Force only where the rule allows it', async ({
                                                                                                         page,
                                                                                                       }) => {
    const puts: Put[] = []

    await stub(page, puts)
    await toModels(page)
    await page.getByTestId('grid-expand').click()

    const rules = page.getByTestId(`override-rules-${guid}`)

    await expect(rules.locator('.tabulator-row')).toHaveCount(2)

    const kinds = rules.locator('select')

    await expect(kinds.nth(0).locator('option')).toHaveText(['Suppress'])
    await expect(kinds.nth(1).locator('option')).toHaveText(['Suppress', 'Force'])

    await kinds.nth(1).selectOption('1')

    await expect.poll(() => puts.length).toBe(1)
    expect(puts[0]).toMatchObject({
      path: '/api/EntityAnalysisModelActivationRuleOverride/OverrideKind',
      body: {entityAnalysisModelActivationRuleName: 'Blacklist', overrideKind: 1},
    })
  })
})
