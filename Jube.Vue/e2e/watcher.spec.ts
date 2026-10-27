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

const activation = (overrides: Record<string, unknown> = {}) => ({
  id: 1,
  key: 'AccountId',
  keyValue: 'ACC-1',
  latitude: 51.5,
  longitude: -0.12,
  activationRuleSummary: 'High value',
  responseElevation: 5,
  responseElevationContent: 'Review',
  backColor: '#cc6e38',
  foreColor: '#ffffff',
  createdDate: '2026-10-21T10:00:00Z',
  ...overrides,
})

const london = activation()
const sydney = activation({
  id: 2,
  latitude: -33.87,
  longitude: 151.21,
  createdDate: '2026-10-21T10:30:00Z',
  activationRuleSummary: 'Velocity',
})
const other = activation({
  id: 3,
  keyValue: 'ACC-2',
  latitude: 40.7,
  longitude: -74,
  createdDate: '2026-10-21T10:10:00Z',
})

const style = {
  version: 8,
  sources: {},
  layers: [{ id: 'background', type: 'background', paint: { 'background-color': '#dde3e8' } }],
}

interface Seen {
  replays: URLSearchParams[]
  summaries: Record<string, unknown>[]
}

async function stub(
  page: Page,
  streamed: unknown[] = [london, sydney, other],
  replayed: unknown[] = [],
) {
  const seen: Seen = { replays: [], summaries: [] }

  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 't' } }))
  await page.route('https://tiles.openfreemap.org/**', (route) => route.fulfill({ json: style }))
  await page.route('**/api/Watcher/Stream', (route) =>
    route.fulfill({
      contentType: 'text/event-stream',
      body:
        'retry: 600000\n\n' +
        streamed.map((item) => `event: activation\ndata: ${JSON.stringify(item)}\n\n`).join(''),
    }),
  )
  await page.route('**/api/ActivationWatcher/Replay**', (route) => {
    seen.replays.push(new URL(route.request().url()).searchParams)

    return route.fulfill({ json: replayed })
  })
  await page.route('**/api/Chart/Watcher/ActivationSummary', (route) => {
    seen.summaries.push(route.request().postDataJSON() as Record<string, unknown>)

    return route.fulfill({
      contentType: 'image/svg+xml',
      body: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"><rect width="10" height="10"/></svg>',
    })
  })

  return seen
}

const map = (page: Page) => page.getByTestId('activation-map')

test.describe('Watcher', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('plots the streamed activations, one pin per entity, with the feed and counts to match', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')

    await expect(page.getByTestId('watcher-counts')).toContainText(
      'Plotted 2 · Without a location 0',
    )
    await expect(page.getByTestId('watcher-counts')).toContainText('Received 3')
    await expect(map(page)).toHaveAttribute('data-points', '2')
    await expect(page.locator('[aria-label="Latest activations"] .tabulator-row')).toHaveCount(3)
    await expect(page.getByTestId('watcher-status')).not.toContainText('Paused')
    await expect(map(page)).not.toHaveAttribute('data-error', /orker/)
  })

  test('an activation with no location stays in the feed but is not pinned or joined', async ({
    page,
  }) => {
    await stub(page, [
      london,
      activation({ id: 9, latitude: 0, longitude: 0, createdDate: '2026-10-21T10:05:00Z' }),
      other,
    ])
    await page.goto('/tools?tool=watcher')

    await expect(page.getByTestId('watcher-counts')).toContainText(
      'Plotted 2 · Without a location 1',
    )
    await expect(map(page)).toHaveAttribute('data-points', '2')
    await expect(map(page)).toHaveAttribute('data-trails', '0')
    await expect(page.locator('[aria-label="Latest activations"] .tabulator-row')).toHaveCount(3)
  })

  test('flags a journey no aeroplane could make, and every activation can be shown instead of the latest', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')

    await expect(page.getByTestId('watcher-impossible')).toHaveText('1 impossible journey')
    await expect(map(page)).toHaveAttribute('data-impossible', '1')
    await expect(map(page)).toHaveAttribute('data-trails', '1')

    await page.getByTestId('watcher-mode-all').check()

    await expect(map(page)).toHaveAttribute('data-points', '3')

    await page.getByTestId('watcher-trails').uncheck()

    await expect(map(page)).toHaveAttribute('data-trails', '0')
    await expect(page.getByTestId('watcher-impossible')).toHaveCount(0)
  })

  test('locating a row selects it, and following the entity narrows the map to its journey', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')
    await expect(map(page)).toHaveAttribute('data-points', '2')

    await page.getByTestId('grid-action-locate').first().click()

    await expect(page.getByTestId('watcher-selected')).toBeVisible()

    const entity = (await page
      .getByTestId('watcher-selected')
      .locator('span')
      .first()
      .textContent())!

    await page.getByTestId('watcher-follow').click()

    await expect(page.getByTestId('watcher-followed')).toContainText(entity)
    await expect(map(page)).toHaveAttribute('data-points', entity === 'AccountId:ACC-1' ? '2' : '1')

    await page.getByTestId('watcher-release').click()

    await expect(map(page)).toHaveAttribute('data-points', '2')
  })

  test('replays a window from history and asks for exactly that window', async ({ page }) => {
    const seen = await stub(page, [], [other])

    await page.goto('/tools?tool=watcher')
    await page.getByTestId('watcher-from').fill('2026-10-21T10:00')
    await page.getByTestId('watcher-to').fill('2026-10-21T11:00')
    await page.getByTestId('watcher-replay').click()

    await expect(map(page)).toHaveAttribute('data-points', '1')
    expect(seen.replays).toHaveLength(1)
    expect(seen.replays[0]!.get('dateFrom')).toBe(new Date('2026-10-21T10:00').toISOString())
    expect(seen.replays[0]!.get('dateTo')).toBe(new Date('2026-10-21T11:00').toISOString())
  })

  test('switching realtime off stops listening, and clearing empties the map', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')
    await expect(map(page)).toHaveAttribute('data-points', '2')

    await page.getByTestId('watcher-live').uncheck()

    await expect(page.getByTestId('watcher-status')).toContainText('Paused')

    await page.getByTestId('watcher-clear').click()

    await expect(map(page)).toHaveAttribute('data-points', '0')
    await expect(page.getByTestId('watcher-counts')).toContainText('Plotted 0')
  })

  test('sends the rule summary for the chart, and the embedded form holds to one entity', async ({
    page,
  }) => {
    const seen = await stub(page)

    await page.goto('/tools?tool=watcher')

    await expect.poll(() => seen.summaries.length, { timeout: 8000 }).toBeGreaterThan(0)
    expect(seen.summaries.at(-1)).toMatchObject({
      columnColor: expect.stringMatching(/^#[0-9a-f]{6}$/),
    })
    expect(
      (seen.summaries.at(-1)!.rules as { name: string }[]).map((rule) => rule.name).sort(),
    ).toEqual(['High value', 'Velocity'])

    await page.getByTestId('watcher-embedded').check()
    await page.getByTestId('watcher-host-key').fill('AccountId')
    await page.getByTestId('watcher-host-value').fill('ACC-1')
    await page.getByTestId('watcher-host-apply').click()

    await expect(page.getByTestId('watcher-counts')).toContainText('Received 3')
    await expect(map(page)).toHaveAttribute('data-points', '2')
    await expect(page.getByTestId('watcher-live')).toHaveCount(0)
  })

  test('the embedded form replays the whole history of its one entity as soon as it opens', async ({
    page,
  }) => {
    const history = [
      activation({ id: 5, createdDate: '2026-10-20T09:00:00Z' }),
      activation({
        id: 6,
        latitude: 48.86,
        longitude: 2.35,
        createdDate: '2026-10-20T09:40:00Z',
      }),
    ]
    const seen = await stub(page, [], history)

    await page.goto('/tools?tool=watcher')
    await page.getByTestId('watcher-embedded').check()
    await page.getByTestId('watcher-host-key').fill('AccountId')
    await page.getByTestId('watcher-host-value').fill('ACC-1')
    await page.getByTestId('watcher-host-apply').click()

    await expect(page.getByTestId('activation-map')).toHaveAttribute('data-trails', '1')
    expect(seen.replays.at(-1)!.get('key')).toBe('AccountId')
    expect(seen.replays.at(-1)!.get('keyValue')).toBe('ACC-1')
    expect(seen.replays.at(-1)!.get('dateFrom')).toBeNull()
    expect(seen.replays.at(-1)!.get('dateTo')).toBeNull()
  })

  test('replaying a selected entity journey asks for exactly that key and value and follows it', async ({
    page,
  }) => {
    const seen = await stub(page, [london, other], [london, sydney])

    await page.goto('/tools?tool=watcher')
    await expect(map(page)).toHaveAttribute('data-points', '2')
    await page
      .locator('[aria-label="Latest activations"] .tabulator-row', { hasText: 'ACC-1' })
      .getByTestId('grid-action-locate')
      .click()
    await page.getByTestId('watcher-journey').click()

    await expect.poll(() => seen.replays.length).toBe(1)
    expect(seen.replays[0]!.get('key')).toBe('AccountId')
    expect(seen.replays[0]!.get('keyValue')).toBe('ACC-1')
    await expect(page.getByTestId('watcher-followed')).toContainText('AccountId:ACC-1')
    await expect(page.getByTestId('watcher-impossible')).toHaveText('1 impossible journey')
  })

  test('older pins and arcs fade, and the fade can be switched off or lengthened', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')
    await expect(map(page)).toHaveAttribute('data-points', '2')

    await expect(page.getByTestId('watcher-fade')).toHaveValue('15')

    await page.getByTestId('watcher-fade').selectOption('0')

    await expect(page.getByTestId('watcher-fade')).toHaveValue('0')
    await expect(map(page)).toHaveAttribute('data-points', '2')
  })

  test('draws the journey the engine recorded even when the earlier activation is not on the map', async ({
    page,
  }) => {
    await stub(page, [
      activation({
        id: 20,
        latitude: -33.87,
        longitude: 151.21,
        createdDate: '2026-10-21T10:30:00Z',
        previousLatitude: 51.5,
        previousLongitude: -0.12,
        previousCreatedDate: '2026-10-21T10:00:00Z',
      }),
    ])
    await page.goto('/tools?tool=watcher')

    await expect(map(page)).toHaveAttribute('data-points', '1')
    await expect(map(page)).toHaveAttribute('data-trails', '1')
    await expect(page.getByTestId('watcher-impossible')).toHaveText('1 impossible journey')
  })

  test('has no accessibility violations with activations showing', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=watcher')
    await expect(map(page)).toHaveAttribute('data-points', '2')
    await settleAnimations(page)

    const results = await new AxeBuilder({ page }).exclude('.maplibregl-map').analyze()

    console.log(
      'AXE',
      JSON.stringify(
        results.violations.map((v) => ({
          id: v.id,
          nodes: v.nodes.map((n) => n.target.join(' ') + ' :: ' + n.html.slice(0, 120)),
        })),
      ),
    )
    console.log(
      'AXE',
      JSON.stringify(
        results.violations.map((v) => ({
          id: v.id,
          nodes: v.nodes.map((n) => n.target.join(' ') + ' :: ' + n.html.slice(0, 140)),
        })),
      ),
    )
    expect(results.violations).toEqual([])
  })
})
