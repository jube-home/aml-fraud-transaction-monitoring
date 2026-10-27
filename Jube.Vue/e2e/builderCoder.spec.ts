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
import { settleAnimations } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

const operators = [
  {
    type: 'equal',
    label: 'is equal to',
    group: 'Comparison',
    arguments: [{ name: 'Value', kind: 'value', many: false }],
    applyTo: ['string', 'integer', 'double', 'datetime', 'boolean'],
    template: ' = ?',
  },
  {
    type: 'greater',
    label: 'is greater than',
    group: 'Comparison',
    arguments: [{ name: 'Value', kind: 'value', many: false }],
    applyTo: ['integer', 'double'],
    template: ' > ?',
  },
]

const completions = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: 'Name:string',
    score: 1000,
    dataType: 'string',
    group: 'Payload',
  },
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: 'Amount:double',
    score: 1000,
    dataType: 'double',
    group: 'Payload',
  },
]

const vocabulary = [
  {
    name: 'StartsWith',
    kind: 'Function',
    overloads: [
      {
        signature: '<String>.StartsWith(prefix As String) As Boolean',
        receiverType: 'String',
        returnType: 'Boolean',
        parameters: [{ name: 'prefix', type: 'String', optional: false, defaultValue: null }],
      },
    ],
  },
  {
    name: 'Trim',
    kind: 'Function',
    overloads: [
      {
        signature: '<String>.Trim() As String',
        receiverType: 'String',
        returnType: 'String',
        parameters: [],
      },
    ],
  },
  {
    name: 'Between',
    kind: 'Function',
    overloads: [
      {
        signature: '<Double>.Between(low As Double, high As Double) As Boolean',
        receiverType: 'Double',
        returnType: 'Boolean',
        parameters: [
          { name: 'low', type: 'Double', optional: false, defaultValue: null },
          { name: 'high', type: 'Double', optional: false, defaultValue: null },
        ],
      },
    ],
  },
  { name: 'Then', kind: 'Keyword', overloads: [] },
]

async function stub(page: Page, parse: (text: string) => object = () => ({ message: 'Compiled' })) {
  await page.route('**/api/Me', (route) => route.fulfill({ json: me }))
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AntiforgeryToken', (route) => route.fulfill({ json: { token: 'test' } }))
  await page.route('**/api/RuleVocabulary', (route) => route.fulfill({ json: vocabulary }))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({ json: operators }))
  await page.route('**/api/Completions/ByEntityAnalysisModelIdParseTypeId**', (route) =>
    route.fulfill({ json: completions }),
  )
  await page.route('**/api/Parser', (route) => {
    const body = route.request().postDataJSON() as { ruleText: string }

    return route.fulfill({ json: parse(body.ruleText) })
  })
}

test.describe('BuilderCoder', () => {
  test.use({ viewport: { width: 1440, height: 1000 } })

  test('writes the code live as the rule is built, and locks the builder once the code is edited', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('rule-coder')).toContainText('If (Payload.Amount > 100) Then')
    await expect(page.getByTestId('compile-status')).toContainText('Compiled')
    await expect(page.getByTestId('builder-locked')).toHaveCount(0)

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type('x')

    await expect(page.getByTestId('builder-locked')).toBeVisible()
    await expect(page.getByTestId('builder-pane')).toHaveAttribute('inert', '')

    await page.getByTestId('builder-reset').click()

    await expect(page.getByTestId('builder-locked')).toHaveCount(0)
    await expect(page.getByTestId('builder-pane')).not.toHaveAttribute('inert')
    await expect(page.getByTestId('rule-coder')).not.toContainText('x')
    await expect(page.getByTestId('compile-status')).toContainText('Compiled')
  })

  test('undoing the edit hands control back to the builder', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')
    await expect(page.getByTestId('rule-coder')).toContainText('If (Payload.Amount > 100) Then')

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.type('x')
    await expect(page.getByTestId('builder-locked')).toBeVisible()

    await page.keyboard.press('Backspace')
    await expect(page.getByTestId('builder-locked')).toHaveCount(0)
  })

  test('squiggles builder-made code in the coder where the compiler objects', async ({ page }) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{ start: 0, length: 5, message: 'Oops', line: 0, column: 4, endColumn: 18 }],
    }))
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('rule-coder').locator('.cm-lintRange-error')).toHaveText(
      'Payload.Amount',
    )
  })

  test('combines rules with OR, negates the group with NOT and shows both states', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await expect(builder.locator('.group-conditions input[value="OR"]')).toBeDisabled()

    await builder.getByRole('button', { name: 'Add rule' }).first().click()
    await expect(builder.locator('.rule-container')).toHaveCount(2)

    const rules = builder.locator('.rule-container')

    await rules.nth(0).locator('.rule-filter-container select').selectOption('Payload.Amount')
    await rules.nth(0).locator('.rule-operator-container select').selectOption('greater')
    await rules.nth(0).locator('.rule-value-container input').fill('1')
    await rules.nth(1).locator('.rule-filter-container select').selectOption('Payload.Name')
    await rules.nth(1).locator('.rule-value-container input').fill('x')

    await builder.locator('.group-conditions label').filter({ hasText: 'OR' }).click()
    await builder.locator('.group-conditions [data-not]').click()
    await expect(builder.locator('.group-conditions [data-not]')).toHaveAttribute(
      'aria-pressed',
      'true',
    )

    await expect(page.getByTestId('rule-coder')).toContainText(
      'If (NOT (Payload.Amount > 1 OR Payload.Name = "x")) Then',
    )
  })

  test('beautifies the code in the coder', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return     True')
    await page.getByTestId('coder-beautify').click()

    await expect(page.getByTestId('rule-coder').locator('.cm-line')).toHaveText('Return True')
  })

  test('offers extension methods for the type of the field, below the fields', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Name.')

    const options = page.locator('.cm-tooltip-autocomplete li[role="option"]')

    await expect(options.filter({ hasText: 'StartsWith' })).toBeVisible()
    await expect(options.filter({ hasText: 'Trim' })).toBeVisible()
    await expect(options.filter({ hasText: 'Between' })).toHaveCount(0)

    await page.keyboard.type('Sta')
    await expect(options.filter({ hasText: 'Trim' })).toHaveCount(0)
    await page.keyboard.press('Enter')

    await expect(page.getByTestId('rule-coder').locator('.cm-line').first()).toHaveText(
      'Payload.Name.StartsWith(prefix)',
    )
  })

  test('chains through the return type of a method', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Name.Trim().')

    await expect(
      page.locator('.cm-tooltip-autocomplete li[role="option"]').filter({ hasText: 'StartsWith' }),
    ).toBeVisible()
  })

  test('shows the parameters of a call while typing its arguments', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Amount.Between(1, ')

    const help = page.locator('.cm-rule-signature')

    await expect(help).toContainText('Between(low As Double, high As Double) As Boolean')
    await expect(help.locator('strong')).toHaveText('high As Double')
  })

  test('describes a field and a method on hover', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Amount.Between(1, 2)')
    await page.keyboard.press('Escape')

    const rect = await page
      .getByTestId('rule-coder')
      .locator('.cm-line')
      .first()
      .evaluate((line) => {
        const walker = document.createTreeWalker(line, NodeFilter.SHOW_TEXT)
        const range = document.createRange()
        let seen = 0

        for (let node = walker.nextNode(); node; node = walker.nextNode()) {
          const length = node.textContent!.length

          if (seen + length > 10) {
            range.setStart(node, 10 - seen)
            range.setEnd(node, 11 - seen)

            break
          }

          seen += length
        }

        const box = range.getBoundingClientRect()

        return { x: box.x + box.width / 2, y: box.y + box.height / 2 }
      })

    await page.mouse.move(rect.x, rect.y)
    await expect(page.locator('.cm-rule-hover')).toContainText('double (Payload)')
  })

  test('puts the squiggle on the exact span the compiler reports', async ({ page }) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{ start: 0, length: 5, message: 'Oops', line: 0, column: 7, endColumn: 12 }],
    }))
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return Tru')

    const squiggle = page.getByTestId('rule-coder').locator('.cm-lintRange-error')

    await expect(squiggle).toHaveText('Tru')
  })

  test('indents after Then and dedents as End If is typed', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')

    const coder = page.getByTestId('rule-coder')

    await coder.locator('.cm-content').click()
    await page.keyboard.type('If x Then')
    await page.keyboard.press('Enter')
    await page.keyboard.type('Return True')
    await page.keyboard.press('Enter')
    await page.keyboard.type('End If')

    await expect(coder.locator('.cm-line').nth(1)).toHaveText('  Return True')
    await expect(coder.locator('.cm-line').nth(2)).toHaveText('End If')
  })

  test('compiles the rule text the builder writes and shows the result', async ({ page }) => {
    const compiled: string[] = []

    await stub(page, (text) => {
      compiled.push(text)

      return { message: 'Compiled' }
    })
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('compile-status')).toContainText('Compiled')
    expect(compiled.at(-1)).toBe('If (Payload.Amount > 100) Then\n  Return True\nEnd If')
  })

  test('shows compiler errors for builder rules as messages', async ({ page }) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{ start: 0, length: 3, message: 'Line 1: Nope is not valid', line: 0 }],
    }))
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
    await expect(page.getByTestId('compile-messages')).toContainText('Nope is not valid')
  })

  test('a blank builder says Incomplete and has nothing to compile', async ({ page }) => {
    const compiled: string[] = []

    await stub(page, (text) => {
      compiled.push(text)

      return { message: 'Compiled' }
    })
    await page.goto('/tools?tool=builder-coder')
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)

    await expect(page.getByTestId('compile-status')).toContainText('Incomplete')
    expect(compiled).toHaveLength(0)
  })

  test('a half-finished builder rule is compiled as far as it goes and fails honestly', async ({
    page,
  }) => {
    const compiled: string[] = []

    await stub(page, (text) => {
      compiled.push(text)

      return {
        message: 'Error',
        errorSpans: [{ start: 0, length: 1, message: 'Line 1: Expression expected.', line: 0 }],
      }
    })
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
    await expect(page.getByTestId('compile-messages')).toContainText('Expression expected')
    await expect(page.getByTestId('compile-messages')).toContainText('a value is needed')
    expect(compiled.at(-1)).toContain('Payload.Amount >')

    await expect(page.getByTestId('rule-coder')).toContainText('Payload.Amount >')
    await expect(page.getByTestId('rule-coder')).not.toContainText('Return False')
  })

  for (const [name, fulfil] of [
    ['never answers', null],
    ['fails', { status: 500, body: '' }],
    [
      'returns a page instead of data',
      { status: 200, contentType: 'text/html', body: '<html></html>' },
    ],
    ['is missing', { status: 404, body: '' }],
  ] as const) {
    test(`the builder and the coder still render when the vocabulary ${name}`, async ({ page }) => {
      await stub(page)
      await page.route('**/api/RuleVocabulary', (route) =>
        fulfil ? route.fulfill(fulfil) : undefined,
      )
      await page.goto('/tools?tool=builder-coder')

      await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1, {
        timeout: 4000,
      })
      await expect(page.getByTestId('rule-coder').locator('.cm-content')).toBeVisible()
    })
  }

  test('fills the card with the coder', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')

    const coder = await page.getByTestId('rule-coder').boundingBox()
    const card = await page.locator('article').boundingBox()

    expect(coder!.height).toBeGreaterThan(card!.height * 0.5)
  })

  test('shows a compiler error against the offending line', async ({ page }) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{ start: 0, length: 3, message: 'Oops', line: 0 }],
    }))
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return Tru')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
  })

  test('coder-only rule types show no builder', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await page.getByTestId('bench-parse-type').selectOption('1')

    await expect(page.getByTestId('rule-coder')).toBeVisible()
    await expect(page.getByTestId('builder-section')).toHaveCount(0)
    await expect(page.getByTestId('compile-status')).toContainText('Return False')
  })

  test('stacks the builder above a full-width coder that wraps long lines', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)

    const builder = await page.getByTestId('builder-section').boundingBox()
    const coder = await page.getByTestId('rule-coder').boundingBox()
    const card = await page.locator('article').boundingBox()

    expect(builder!.y + builder!.height).toBeLessThanOrEqual(coder!.y)
    expect(coder!.width).toBeGreaterThan(card!.width * 0.9)

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type(`Payload.Name.${'Trim().'.repeat(60)}`)

    await expect(page.getByTestId('rule-coder').locator('.cm-content')).toHaveClass(
      /cm-lineWrapping/,
    )

    const content = await page
      .getByTestId('rule-coder')
      .locator('.cm-scroller')
      .evaluate((e) => ({
        scroll: e.scrollWidth,
        client: e.clientWidth,
      }))

    expect(content.scroll).toBeLessThanOrEqual(content.client + 1)
  })

  test('shows the value the component would save as soon as it loads and as it changes', async ({
    page,
  }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')

    const card = page.getByTestId('bench-value-card')

    await expect(card).toHaveAttribute('open', '')
    await expect(page.getByTestId('bench-value')).toContainText('"ruleType": 1')
    await expect(page.getByTestId('bench-value')).toContainText('"ruleTextBuilder": "Return False"')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('bench-value')).toContainText('Payload.Amount > 100')
  })

  test('keeps the compile pill in the flow, above the value card', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await expect(page.getByTestId('compile-status')).toBeVisible()

    const pill = await page.getByTestId('compile-status').boundingBox()
    const card = await page.getByTestId('bench-value-card').boundingBox()
    const coder = await page.getByTestId('rule-coder').boundingBox()

    expect(pill!.y).toBeGreaterThanOrEqual(coder!.y + coder!.height)
    expect(pill!.y + pill!.height).toBeLessThanOrEqual(card!.y)
  })

  test('presents each problem with its source and line, and a click goes to it in the coder', async ({
    page,
  }) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [
        {
          start: 0,
          length: 5,
          message: 'Line 1: expression expected.',
          line: 0,
          column: 4,
          endColumn: 18,
        },
      ],
    }))
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    const problem = page.getByTestId('problem-compiler')

    await expect(problem).toContainText('Compiler')
    await expect(problem).toContainText(/Expression expected\. \(Line 1\)/)
    await expect(problem).not.toContainText('Line 1:')

    await problem.click()

    await expect(page.getByTestId('rule-coder').locator('.cm-content')).toBeFocused()
    expect(await page.evaluate(() => window.getSelection()?.toString())).toBe('Payload.Amount')
  })

  test('a builder problem takes you to the value that is missing', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')

    const problem = page.getByTestId('problem-builder')

    await expect(problem).toContainText('Builder')
    await expect(problem).toContainText('Value: a value is needed')

    await problem.click()

    await expect(builder.locator('.rule-value-container input')).toBeFocused()
  })

  test('has no accessibility violations while the builder is locked', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('x')
    await expect(page.getByTestId('builder-locked')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="builder-coder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })

  test('has no accessibility violations', async ({ page }) => {
    await stub(page)
    await page.goto('/tools?tool=builder-coder')
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({ page })
      .include('[data-testid="builder-coder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
