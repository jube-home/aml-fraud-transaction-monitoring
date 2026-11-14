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
import {modelOf, stubFormModels} from './formModels'
import {me} from './hierarchyStub'

async function stub(page: Page, landlord: boolean): Promise<string[]> {
  const telemetryCalls: string[] = []
  await page.route('**/api/Me', (route) => route.fulfill({json: {...me, landlord}}))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({contentType: 'text/event-stream', body: ': keep-alive\n\n'}),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({json: {token: 't'}}))
  await page.route('**/api/OpenTelemetry*', (route) => {
    telemetryCalls.push(route.request().url())
    return route.fulfill({status: landlord ? 200 : 403, json: []})
  })
  await stubFormModels(page, [
    modelOf({area: 'OpenTelemetryLogCounter', fields: [{name: 'name', type: 'string'}]}),
    modelOf({area: 'OpenTelemetryExclude', fields: [{name: 'name', type: 'string'}]}),
  ])

  return telemetryCalls
}

test.describe('Telemetry settings landlord gate', () => {
  test.use({viewport: {width: 1440, height: 900}})

  test('shows the tabs to the landlord and no denial', async ({page}) => {
    await stub(page, true)
    await page.goto('/administration/telemetry')
    await expect(page.getByTestId('tab-OpenTelemetryLogCounter')).toBeVisible()
    await expect(page.getByTestId('adminTelemetry-denied')).toHaveCount(0)
  })

  test('denies a tenant user who deep-links and never calls the telemetry API', async ({
                                                                                         page,
                                                                                       }) => {
    const calls = await stub(page, false)
    await page.goto('/administration/telemetry?area=OpenTelemetryExclude')
    await expect(page.getByTestId('adminTelemetry-denied')).toBeVisible()
    await expect(page.getByTestId('tab-OpenTelemetryExclude')).toHaveCount(0)
    expect(calls).toEqual([])
  })
})
