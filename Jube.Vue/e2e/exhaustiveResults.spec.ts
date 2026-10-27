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
import { stubExhaustive as stub } from './exhaustiveStubs'
import { settleAnimations } from './support'

async function open(page: Page, query = '') {
  await page.goto(`/tools?tool=exhaustive-results${query}`)
  await expect(page.getByTestId('exhaustive-band')).toBeVisible()
}

test.describe('Exhaustive results', () => {
  test.use({ viewport: { width: 1500, height: 1300 } })

  test('shows the band, the verdict and the six sections with an index', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('exhaustive-status')).toContainText('Finished')
    await expect(page.getByTestId('exhaustive-models')).toHaveText('142')
    await expect(page.getByTestId('exhaustive-score')).toHaveText('0.9123')
    await expect(page.getByTestId('narrative-holistic')).toContainText('separates fraud well')
    await expect(page.getByTestId('exhaustive-anomaly')).toContainText('Isolation forest')

    await expect(page.getByTestId('canvas-index').getByRole('button')).toHaveText([
      'Verdict',
      'What it learned',
      'Which model is live',
      'How well it detects',
      'What if…',
      'How it got here',
    ])
  })

  test('opens a closed section from the index, scrolls to it and records it in the address', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)

    expect(seen.charts.some((chart) => chart.path.includes('TrialDiagram'))).toBe(false)
    await expect(page.getByTestId('exhaustive-trials')).toHaveCount(0)

    await page.getByTestId('canvas-index-trials').click()

    await expect(page.getByTestId('exhaustive-trials')).toBeVisible()
    await expect(page).toHaveURL(/section=trials/)
    await expect(page.getByTestId('canvas-index-trials')).toHaveAttribute(
      'aria-current',
      'location',
    )
  })

  test('lands on the section named in the address', async ({ page }) => {
    await stub(page)
    await open(page, '&section=whatif')

    await expect(page.getByTestId('exhaustive-sensitivity')).toBeVisible()
  })

  test('does not draw charts in a collapsed section until it is opened', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    await expect(page.getByTestId('exhaustive-performance')).toBeVisible()

    const before = seen.charts.length

    await page.getByTestId('canvas-whatif').locator('summary').click()
    await expect(page.getByTestId('exhaustive-sensitivity')).toBeVisible()
    await expect.poll(() => seen.charts.length).toBeGreaterThan(before)
  })

  test('follows the page: the index marks the section being read', async ({ page }) => {
    await stub(page)
    await open(page)

    await expect(page.getByTestId('canvas-index').locator('[aria-current=location]')).toHaveCount(1)

    await page.mouse.move(900, 600)

    await expect(async () => {
      await page.mouse.wheel(0, 250)
      await expect(page.getByTestId('canvas-index-detects')).toHaveAttribute(
        'aria-current',
        'location',
        { timeout: 150 },
      )
    }).toPass({ timeout: 15000 })

    await expect(page.getByTestId('canvas-index-detects')).toHaveAttribute(
      'aria-current',
      'location',
    )
    await expect(page.getByTestId('canvas-index').locator('[aria-current=location]')).toHaveCount(1)
  })

  test('brings a far section into view from the index even as sections above it fill in', async ({
    page,
  }) => {
    await stub(page)
    await page.setViewportSize({ width: 1500, height: 700 })
    await open(page)

    await page.getByTestId('canvas-index-detects').click()

    await expect(page.getByTestId('canvas-detects').locator('summary')).toBeInViewport({
      timeout: 8000,
    })
    await expect(page.getByTestId('performance-measures')).toBeInViewport({ timeout: 8000 })
  })

  test('brings a section opened from the index to the top once its content has filled in', async ({
    page,
  }) => {
    await stub(page)
    await page.setViewportSize({ width: 1500, height: 1000 })
    await open(page)

    await page.getByTestId('canvas-index-whatif').click()

    await expect(page.getByTestId('exhaustive-sensitivity')).toBeVisible()
    await expect
      .poll(
        () =>
          page
            .getByTestId('canvas-whatif')
            .evaluate((element) => Math.round(element.getBoundingClientRect().top)),
        { timeout: 8000 },
      )
      .toBeLessThan(450)
  })

  for (const [theme, surface] of [
    ['light', '#fcfcfb'],
    ['dark', '#1a1a19'],
  ] as const) {
    test(`draws the charts with the chart stylesheet in the ${theme} theme of the app`, async ({
      page,
    }) => {
      await stub(page)
      await page.emulateMedia({ colorScheme: theme === 'light' ? 'dark' : 'light' })
      await page.addInitScript((stored) => localStorage.setItem('jube.theme', stored), theme)
      await open(page)

      const chart = page.getByTestId('exhaustive-learning').locator('svg.jc').first()

      await expect(chart).toHaveAttribute('data-theme', theme)
      await expect
        .poll(() =>
          chart.evaluate((element) =>
            getComputedStyle(element).getPropertyValue('--jc-surface').trim(),
          ),
        )
        .toBe(surface)
    })
  }

  test('draws a far section only once it nears the screen', async ({ page }) => {
    const seen = await stub(page)

    await page.setViewportSize({ width: 1500, height: 700 })
    await open(page)
    await expect(page.getByTestId('exhaustive-variables')).toBeVisible()

    expect(seen.charts.some((chart) => chart.path.includes('/Measures/'))).toBe(false)

    await page.getByTestId('canvas-detects').evaluate((element) => element.scrollIntoView())

    await expect
      .poll(() => seen.charts.some((chart) => chart.path.includes('/Measures/')))
      .toBe(true)
  })

  test('shows progress and no results while the training is still early', async ({ page }) => {
    await stub(page, { instanceStatus: 9 })
    await open(page)

    await expect(page.getByTestId('exhaustive-status')).toContainText('Shuffling data')
    await expect(page.getByTestId('exhaustive-progress')).toHaveAttribute('value', '9')
    await expect(page.getByTestId('exhaustive-waiting')).toBeVisible()
    await expect(page.getByTestId('canvas-index')).toHaveCount(0)
    await expect(page.getByTestId('exhaustive-stop')).toBeVisible()
  })

  test('keeps polling while it trains and stops when it finishes', async ({ page }) => {
    await page.clock.install()
    await stub(page, { statuses: [15, 15, 16] })
    await open(page)

    await expect(page.getByTestId('exhaustive-status')).toContainText('Supervised learning')

    await page.clock.fastForward(5500)
    await expect(page.getByTestId('exhaustive-status')).toContainText('Supervised learning')
    await page.clock.fastForward(5500)

    await expect(page.getByTestId('exhaustive-status')).toContainText('Finished')
    await expect(page.getByTestId('exhaustive-stop')).toHaveCount(0)
  })

  test('stops a running training', async ({ page }) => {
    const seen = await stub(page, { instanceStatus: 15 })
    await open(page)

    await page.getByTestId('exhaustive-stop').click()

    await expect.poll(() => seen.stops).toEqual(['g-1'])
    await expect(page.getByTestId('exhaustive-status')).toContainText('Stopping')
  })

  test('says so when the adaptation cannot be loaded', async ({ page }) => {
    await stub(page)
    await page.route('**/api/ExhaustiveSearchInstance/1', (route) =>
      route.fulfill({ status: 500, json: {} }),
    )
    await page.goto('/tools?tool=exhaustive-results')

    await expect(page.getByTestId('exhaustive-failed')).toBeVisible()
  })

  test('lists the variables, searches them and opens the charts of one', async ({ page }) => {
    const seen = await stub(page)
    await open(page)
    const panel = page.getByTestId('exhaustive-variables')

    await expect(panel).toContainText('Amount')
    await expect(panel).toContainText('Velocity')

    await page.getByTestId('exhaustive-variable-search').fill('veloc')
    await expect(panel).not.toContainText('Amount')
    await page.getByTestId('exhaustive-variable-search').fill('')

    await panel.getByRole('button', { name: 'Show the charts' }).first().click()
    await expect(page.getByTestId('variable-detail-1')).toBeVisible()
    await expect(page.getByTestId('variable-detail-1')).toContainText('Velocity')
    expect(seen.charts.map((chart) => chart.path)).toEqual(
      expect.arrayContaining([
        '/api/Chart/Exhaustive/VariableHistogram/1/1',
        '/api/Chart/ExhaustiveStatistics/VariableClasses/1/1?large=true&counts=false',
      ]),
    )

    await page.getByTestId('exhaustive-variable-counts').check()
    await expect
      .poll(() =>
        seen.charts.some((chart) =>
          chart.path.endsWith('VariableClasses/1/1?large=true&counts=true'),
        ),
      )
      .toBe(true)
  })

  test('changes which model is live and shows why a refusal happened', async ({ page }) => {
    const seen = await stub(page, { activateStatus: 400 })
    await open(page)

    await page.getByTestId('promoted-active-12').check()

    await expect(page.getByTestId('promoted-problems')).toContainText('One model must stay active.')
    expect(seen.activations).toEqual([{ id: 12, active: true }])
  })

  test('shows the prescription of a promoted model and its charts', async ({ page }) => {
    const seen = await stub(page)
    await open(page)

    await page
      .getByTestId('exhaustive-promoted')
      .getByRole('button', { name: 'Show the prescription' })
      .first()
      .click()
    await expect(page.getByTestId('prescriptions-11')).toContainText('Amount')

    await page
      .getByTestId('prescriptions-11')
      .getByRole('button', { name: 'Show the charts' })
      .click()
    await expect
      .poll(() =>
        seen.charts.some(
          (chart) => chart.path === '/api/Chart/Exhaustive/PrescriptionHistogram/11/901',
        ),
      )
      .toBe(true)
  })

  test('draws the performance charts and re-bases the value charts on a chosen variable', async ({
    page,
  }) => {
    const seen = await stub(page)
    await open(page)
    const panel = page.getByTestId('exhaustive-performance')

    await expect(panel.getByTestId('performance-gains')).toContainText('top decile')
    await expect(panel.getByTestId('performance-valueCaptured')).toHaveCount(0)

    await page.getByTestId('exhaustive-basis').selectOption('1')

    await expect(panel.getByTestId('performance-valueCaptured')).toBeVisible()
    await expect
      .poll(() =>
        seen.charts.some(
          (chart) => chart.path === '/api/Chart/Exhaustive/Ks/1?variableId=1&variableName=Amount',
        ),
      )
      .toBe(true)
    expect(
      seen.charts.some(
        (chart) =>
          chart.path === '/api/Chart/Exhaustive/Measures/1?variableId=1&variableName=Amount',
      ),
    ).toBe(false)
  })

  test('scores what-if inputs and redraws the network with their relevance', async ({ page }) => {
    const seen = await stub(page)
    await open(page, '&section=whatif')

    await expect(page.getByTestId('sensitivity-score')).toContainText('0.5500')
    expect(seen.recalls[0]).toEqual({ Amount: 50, Country: 'Other' })

    await page.getByTestId('sensitivity-control-0').fill('80')
    await page.getByTestId('sensitivity-control-1').selectOption('GB')

    await expect(page.getByTestId('sensitivity-score')).toContainText('0.5800')
    expect(seen.recalls.at(-1)).toEqual({ Amount: 80, Country: 'GB' })
    expect(
      seen.charts.some(
        (chart) =>
          chart.method === 'POST' &&
          chart.path === '/api/Chart/Exhaustive/Topology/1' &&
          JSON.stringify(chart.body) === JSON.stringify({ nodeRelevance: [[0.1, 0.2]] }),
      ),
    ).toBe(true)
  })

  test('scores a file in a batch and reports its warnings', async ({ page }) => {
    const seen = await stub(page)
    await open(page, '&section=whatif')

    await page
      .getByTestId('exhaustive-batch')
      .getByTestId('file-upload-input')
      .setInputFiles({
        name: 'rows.csv',
        mimeType: 'text/csv',
        buffer: Buffer.from('Amount\n1\n'),
      })
    await page.getByTestId('exhaustive-batch').getByTestId('file-upload-send').click()

    await expect(page.getByTestId('batch-summary')).toContainText('2 rows, 1 scored')
    await expect(page.getByTestId('exhaustive-batch')).toContainText('No values')
    await expect(page.getByTestId('exhaustive-batch')).toContainText('One row was empty.')
    expect(seen.batches).toBe(1)
  })

  test('shows why a batch file was refused', async ({ page }) => {
    await stub(page, {
      batch: { status: 400, json: { errors: ['No column matches a variable.'] } },
    })
    await open(page, '&section=whatif')

    await page
      .getByTestId('exhaustive-batch')
      .getByTestId('file-upload-input')
      .setInputFiles({
        name: 'rows.csv',
        mimeType: 'text/csv',
        buffer: Buffer.from('x\n1\n'),
      })
    await page.getByTestId('exhaustive-batch').getByTestId('file-upload-send').click()

    await expect(page.getByTestId('batch-problems')).toContainText('No column matches a variable.')
  })

  test('shows the diagram and evidence of a trial', async ({ page }) => {
    const seen = await stub(page)
    await open(page, '&section=trials')

    await page
      .getByTestId('exhaustive-trials')
      .getByRole('button', { name: 'Show evidence' })
      .first()
      .click()

    await expect(page.getByTestId('trial-evidence')).toContainText(
      'Neural Network (evolved topology) trial, score 0.9123',
    )
    await expect(page.getByTestId('evidence-tree')).toBeVisible()
    expect(seen.charts.some((chart) => chart.path === '/api/Chart/Exhaustive/TrialDiagram/5')).toBe(
      true,
    )
  })

  test('has no accessibility violations with every section open', async ({ page }) => {
    await stub(page)
    await open(page, '&section=trials')
    await page.getByTestId('canvas-whatif').locator('summary').click()
    await expect(page.getByTestId('exhaustive-sensitivity')).toBeVisible()
    await settleAnimations(page)

    const scan = await new AxeBuilder({ page }).analyze()

    expect(scan.violations).toEqual([])
  })
})
