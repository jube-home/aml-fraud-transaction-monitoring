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
import { readFile } from 'node:fs/promises'
import ExcelJS from 'exceljs'
import { expect, test } from '@playwright/test'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

test.describe('Grid CSV export', () => {
  test.use({ viewport: { width: 1600, height: 1000 } })

  test('downloads a spreadsheet-safe CSV of the data columns only', async ({ page }) => {
    await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
    )
    await page.goto('/tools?tool=grid-csv-export')
    await expect(page.locator('.tabulator-row')).toHaveCount(5)

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('export-csv').click(),
    ])

    expect(download.suggestedFilename()).toBe('evidence.csv')

    const csv = await readFile((await download.path())!, 'utf8')
    const lines = csv.replace(/^\uFEFF/, '').split(/\r?\n/)

    expect(csv.startsWith('\uFEFF')).toBe(true)
    expect(lines[0]).toBe('"Id","Source","Detail","Hits","Seen"')
    expect(csv).toContain(`"'=HYPERLINK(""http://x.test"",""click"")"`)
    expect(csv).toContain(`"'@SUM(1+1)"`)
    expect(csv).toContain('"comma, ""quote"" and\nnewline"')
    expect(csv).toContain('"-5"')
    expect(csv).not.toContain("'-5")
    expect(csv).not.toContain('Row action')
    expect(csv).not.toContain('Actions')
  })

  test('downloads a styled Excel workbook beside the CSV, with real numbers and dates', async ({
    page,
  }) => {
    await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
    await page.route('**/api/ServiceChange/Stream', (route) =>
      route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
    )
    await page.goto('/tools?tool=grid-csv-export')
    await expect(page.locator('.tabulator-row')).toHaveCount(5)

    const buttons = page.getByTestId('grid-export').getByRole('button')

    await expect(buttons).toHaveText(['Excel', 'CSV'])

    const [download] = await Promise.all([
      page.waitForEvent('download'),
      page.getByTestId('export-excel').click(),
    ])

    expect(download.suggestedFilename()).toBe('evidence.xlsx')

    const workbook = new ExcelJS.Workbook()

    await workbook.xlsx.readFile((await download.path())!)

    const sheet = workbook.worksheets[0]!

    expect(sheet.getRow(1).values).toEqual([undefined, 'Id', 'Source', 'Detail', 'Hits', 'Seen'])
    expect(sheet.getRow(1).font?.bold).toBe(true)
    expect(sheet.views[0]).toMatchObject({ state: 'frozen', ySplit: 1 })
    expect(sheet.rowCount).toBe(6)
    expect(sheet.getCell('A2').value).toBe(1)
    expect(sheet.getCell('D4').value).toBe(-4)
    expect(sheet.getCell('C3').value).toBe('=HYPERLINK("http://x.test","click")')
    expect(sheet.getCell('C3').type).toBe(3)
    expect(sheet.getCell('E2').value).toEqual(new Date('2026-10-20T10:30:00Z'))
    expect(sheet.getCell('E4').value).toBeNull()
    expect(sheet.getRow(1).values).not.toContain('Actions')
  })
})
