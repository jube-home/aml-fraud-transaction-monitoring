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
import {expect, type Page} from '@playwright/test'
import {test} from './fixtures'
import {openEditorHost, stubEditorHost} from './editorHost'
import {setLanguage, settleAnimations} from './support'

const operators = [
  {
    type: 'equal',
    label: 'is equal to',
    group: 'Comparison',
    arguments: [{name: 'Value', kind: 'value', many: false}],
    applyTo: ['string', 'integer', 'double', 'datetime', 'boolean'],
    template: ' = ?',
  },
  {
    type: 'greater',
    label: 'is greater than',
    group: 'Comparison',
    arguments: [{name: 'Value', kind: 'value', many: false}],
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
        parameters: [{name: 'prefix', type: 'String', optional: false, defaultValue: null}],
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
          {name: 'low', type: 'Double', optional: false, defaultValue: null},
          {name: 'high', type: 'Double', optional: false, defaultValue: null},
        ],
      },
    ],
  },
  {name: 'Then', kind: 'Keyword', overloads: []},
]

const cursorMember = (name: string, returnType: string, description: string) => ({
  name,
  kind: 'Keyword',
  description,
  overloads: [
    {
      signature: `Cursor.${name} As ${returnType}`,
      receiverType: 'AbstractionCursor',
      returnType,
      parameters: [],
    },
  ],
})

const abstractionVocabulary = [
  ...vocabulary,
  ...[
    'If',
    'End If',
    'Return',
    'Matched',
    'True',
    'False',
    'Payload',
    'Dim',
    'Static',
    'As',
    'Integer',
    'Double',
  ].map((name) => ({
    name,
    kind: 'Keyword',
    overloads: [],
  })),
  {
    name: 'Cursor',
    kind: 'Keyword',
    description: 'Where the rule is in its walk through the history.',
    overloads: [
      {
        signature: 'Cursor As AbstractionCursor',
        receiverType: '',
        returnType: 'AbstractionCursor',
        parameters: [],
      },
    ],
  },
  cursorMember('Position', 'Integer', 'One-based position of the transaction being tested.'),
  cursorMember('Matches', 'Integer', 'How many transactions this rule has accepted so far.'),
  cursorMember('IsCurrent', 'Boolean', 'True for the transaction being processed now.'),
  cursorMember('Result', 'Double', 'A number the rule can set and read.'),
  {
    name: 'Exit For',
    kind: 'Keyword',
    overloads: [],
    description: 'Finishes with this transaction and ends the walk.',
  },
  {
    name: 'Continue For',
    kind: 'Keyword',
    overloads: [],
    description: 'Finishes with this transaction and moves to the next.',
  },
]

async function stubAbstraction(
  page: Page,
  parse: (text: string) => object = () => ({message: 'Compiled'}),
) {
  await stub(page, parse, 3)
  await page.route('**/api/RuleVocabulary*', (route) =>
    route.fulfill({json: abstractionVocabulary}),
  )
}

async function write(page: Page, text: string) {
  const content = page.getByTestId('rule-coder').locator('.cm-content')

  await content.click()
  await page.keyboard.press('Control+a')
  await page.keyboard.insertText(text)
  await expect(content).toContainText(text.split('\n').at(-1)!.trim())
}

async function stub(
  page: Page,
  parse: (text: string) => object = () => ({message: 'Compiled'}),
  parseType = 5,
) {
  const sent = await stubEditorHost(page, {editor: 'rule', parseType})

  await page.route('**/api/RuleVocabulary*', (route) => route.fulfill({json: vocabulary}))
  await page.route('**/api/QueryBuilder/Operators', (route) => route.fulfill({json: operators}))
  await page.route('**/api/Completions/ByEntityAnalysisModelIdParseTypeId**', (route) =>
    route.fulfill({json: completions}),
  )
  await page.route('**/api/Parser', (route) => {
    const request = route.request().postDataJSON() as { ruleText: string }

    return route.fulfill({json: parse(request.ruleText)})
  })

  return sent
}

async function open(page: Page) {
  await openEditorHost(page)
  await expect(page.getByTestId('rule-coder')).toBeVisible()
}

test.describe('BuilderCoder', () => {
  test.use({viewport: {width: 1440, height: 1000}})

  test('writes the code live as the rule is built, and locks the builder once the code is edited', async ({
                                                                                                            page,
                                                                                                          }) => {
    await stub(page)
    await open(page)

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

  test('asks for the vocabulary of the rule type being edited', async ({page}) => {
    const asked: string[] = []

    await stub(page, undefined, 3)
    await page.route('**/api/RuleVocabulary*', (route) => {
      asked.push(new URL(route.request().url()).searchParams.get('ruleParseTypeId') ?? '')

      return route.fulfill({
        json: [...vocabulary, {name: 'Exit For', kind: 'Keyword', overloads: []}],
      })
    })
    await open(page)

    await expect.poll(() => asked).toEqual(['3'])

    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.press('Control+End')
    await page.keyboard.press('Enter')
    await page.keyboard.type('Ex')

    await expect(page.locator('.cm-tooltip-autocomplete')).toContainText('Exit For')
  })

  test('undoing the edit hands control back to the builder', async ({page}) => {
    await stub(page)
    await open(page)

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

  test('squiggles builder-made code in the coder where the compiler objects', async ({page}) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{start: 0, length: 5, message: 'Oops', line: 0, column: 4, endColumn: 18}],
    }))
    await open(page)

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
    await open(page)

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await expect(builder.locator('.group-conditions input[value="OR"]')).toBeDisabled()

    await builder.getByRole('button', {name: 'Add rule'}).first().click()
    await expect(builder.locator('.rule-container')).toHaveCount(2)

    const rules = builder.locator('.rule-container')

    await rules.nth(0).locator('.rule-filter-container select').selectOption('Payload.Amount')
    await rules.nth(0).locator('.rule-operator-container select').selectOption('greater')
    await rules.nth(0).locator('.rule-value-container input').fill('1')
    await rules.nth(1).locator('.rule-filter-container select').selectOption('Payload.Name')
    await rules.nth(1).locator('.rule-value-container input').fill('x')

    await builder.locator('.group-conditions label').filter({hasText: 'OR'}).click()
    await builder.locator('.group-conditions [data-not]').click()
    await expect(builder.locator('.group-conditions [data-not]')).toHaveAttribute(
      'aria-pressed',
      'true',
    )

    await expect(page.getByTestId('rule-coder')).toContainText(
      'If (NOT (Payload.Amount > 1 OR Payload.Name = "x")) Then',
    )
  })

  test('beautifies the code in the coder', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return     True')
    await page.getByTestId('coder-beautify').click()

    await expect(page.getByTestId('rule-coder').locator('.cm-line')).toHaveText('Return True')
  })

  test('offers extension methods for the type of the field, below the fields', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Name.')

    const options = page.locator('.cm-tooltip-autocomplete li[role="option"]')

    await expect(options.filter({hasText: 'StartsWith'})).toBeVisible()
    await expect(options.filter({hasText: 'Trim'})).toBeVisible()
    await expect(options.filter({hasText: 'Between'})).toHaveCount(0)

    await page.keyboard.type('Sta')
    await expect(options.filter({hasText: 'Trim'})).toHaveCount(0)
    await page.keyboard.press('Enter')

    await expect(page.getByTestId('rule-coder').locator('.cm-line').first()).toHaveText(
      'Payload.Name.StartsWith(prefix)',
    )
  })

  test('chains through the return type of a method', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Name.Trim().')

    await expect(
      page.locator('.cm-tooltip-autocomplete li[role="option"]').filter({hasText: 'StartsWith'}),
    ).toBeVisible()
  })

  test('shows the parameters of a call while typing its arguments', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Payload.Amount.Between(1, ')

    const help = page.locator('.cm-rule-signature')

    await expect(help).toContainText('Between(low As Double, high As Double) As Boolean')
    await expect(help.locator('strong')).toHaveText('high As Double')
  })

  test('describes a field and a method on hover', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)
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

        return {x: box.x + box.width / 2, y: box.y + box.height / 2}
      })

    await page.mouse.move(rect.x, rect.y)
    await expect(page.locator('.cm-rule-hover')).toContainText('double (Payload)')
  })

  test('puts the squiggle on the exact span the compiler reports', async ({page}) => {
    await stub(
      page,
      () => ({
        message: 'Error',
        errorSpans: [{start: 0, length: 5, message: 'Oops', line: 0, column: 7, endColumn: 12}],
      }),
      1,
    )
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return Tru')

    const squiggle = page.getByTestId('rule-coder').locator('.cm-lintRange-error')

    await expect(squiggle).toHaveText('Tru')
  })

  test('indents after Then and dedents as End If is typed', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)

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

  test('compiles the rule text the builder writes and shows the result', async ({page}) => {
    const compiled: string[] = []

    await stub(page, (text) => {
      compiled.push(text)

      return {message: 'Compiled'}
    })
    await open(page)

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('compile-status')).toContainText('Compiled')
    expect(compiled.at(-1)).toBe('If (Payload.Amount > 100) Then\n  Return True\nEnd If')
  })

  test('shows compiler errors for builder rules as messages', async ({page}) => {
    await stub(page, () => ({
      message: 'Error',
      errorSpans: [{start: 0, length: 3, message: 'Line 1: Nope is not valid', line: 0}],
    }))
    await open(page)

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
    await expect(page.getByTestId('compile-messages')).toContainText('Nope is not valid')
  })

  test('a blank builder says Incomplete and has nothing to compile', async ({page}) => {
    const compiled: string[] = []

    await stub(page, (text) => {
      compiled.push(text)

      return {message: 'Compiled'}
    })
    await open(page)
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
        errorSpans: [{start: 0, length: 1, message: 'Line 1: Expression expected.', line: 0}],
      }
    })
    await open(page)

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
    ['fails', {status: 500, body: ''}],
    [
      'returns a page instead of data',
      {status: 200, contentType: 'text/html', body: '<html></html>'},
    ],
    ['is missing', {status: 404, body: ''}],
  ] as const) {
    test(`the builder and the coder still render when the vocabulary ${name}`, async ({page}) => {
      await stub(page)
      await page.route('**/api/RuleVocabulary*', (route) =>
        fulfil ? route.fulfill(fulfil) : undefined,
      )
      await open(page)

      await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1, {
        timeout: 4000,
      })
      await expect(page.getByTestId('rule-coder').locator('.cm-content')).toBeVisible()
    })
  }

  test('fills the editor with the coder', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)

    const coder = await page.getByTestId('rule-coder').boundingBox()
    const card = await page.getByTestId('editor-ruleBuilder').boundingBox()

    expect(coder!.height).toBeGreaterThan(card!.height * 0.5)
  })

  test('shows a compiler error against the offending line', async ({page}) => {
    await stub(
      page,
      () => ({
        message: 'Error',
        errorSpans: [{start: 0, length: 3, message: 'Oops', line: 0}],
      }),
      1,
    )
    await open(page)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('Return Tru')

    await expect(page.getByTestId('compile-status')).toContainText('Errors')
  })

  test('coder-only rule types show no builder', async ({page}) => {
    await stub(page, undefined, 1)
    await open(page)

    await expect(page.getByTestId('rule-coder')).toBeVisible()
    await expect(page.getByTestId('builder-section')).toHaveCount(0)
    await expect(page.getByTestId('compile-status')).toContainText('Return False')
  })

  test('keeps the coder left-to-right in a right-to-left page', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByTestId('rule-coder')).toBeVisible()
    await page.getByTestId('account-button').click()
    await setLanguage(page, 'ar')
    await page.keyboard.press('Escape')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

    const coder = page.getByTestId('rule-coder')

    await expect(coder).toHaveAttribute('dir', 'ltr')
    await expect(coder.locator('.cm-content')).toHaveCSS('direction', 'ltr')

    await coder.locator('.cm-content').click()
    await page.keyboard.type('Payload.Amount > 100')
    await expect(coder.locator('.cm-line').first()).toHaveText('Payload.Amount > 100')
  })

  test('stacks the builder above a full-width coder that wraps long lines', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)

    const builder = await page.getByTestId('builder-section').boundingBox()
    const coder = await page.getByTestId('rule-coder').boundingBox()
    const card = await page.getByTestId('editor-ruleBuilder').boundingBox()

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

  test('saves the code and the builder rule it wrote as soon as the rule is built', async ({
                                                                                             page,
                                                                                           }) => {
    const sent = await stub(page)
    await open(page)

    const builder = page.getByTestId('rule-builder')

    await expect(builder.locator('.rule-container')).toHaveCount(1)
    await builder.locator('.rule-filter-container select').selectOption('Payload.Amount')
    await builder.locator('.rule-operator-container select').selectOption('greater')
    await builder.locator('.rule-value-container input').fill('100')
    await expect(page.getByTestId('compile-status')).toContainText('Compiled')

    await page.getByTestId('crud-save').click()
    await expect.poll(() => sent.length).toBe(1)

    expect(String(sent[0]!.body.functionScript)).toContain('Payload.Amount > 100')
    expect(String(sent[0]!.body.builderText)).toContain('Payload.Amount > 100')
    expect(String(sent[0]!.body.builderJson)).toContain('Payload.Amount')
  })

  test('keeps the compile pill in the flow, below the coder', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByTestId('compile-status')).toBeVisible()

    const pill = await page.getByTestId('compile-status').boundingBox()
    const coder = await page.getByTestId('rule-coder').boundingBox()

    expect(pill!.y).toBeGreaterThanOrEqual(coder!.y + coder!.height)
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
    await open(page)

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

  test('a builder problem takes you to the value that is missing', async ({page}) => {
    await stub(page)
    await open(page)

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

  test('colours the rule by the role of each word', async ({page}) => {
    await stubAbstraction(page)
    await open(page)
    await write(
      page,
      [
        "' the first three large payments",
        'If Cursor.Matches >= 3 Then Exit For',
        'Matched = Payload.Amount.Between(1000, 5000)',
      ].join('\n'),
    )

    const coder = page.getByTestId('rule-coder')
    const word = (text: string) => coder.locator('.cm-line span').getByText(text, {exact: true})

    await expect(word('If')).toHaveCSS('color', 'rgb(0, 95, 127)')
    await expect(word('If')).toHaveCSS('font-weight', '600')
    await expect(word('Cursor')).toHaveCSS('color', 'rgb(26, 107, 97)')
    await expect(word('Amount')).toHaveCSS('color', 'rgb(26, 107, 97)')
    await expect(word('Between')).toHaveCSS('color', 'rgb(107, 63, 160)')
    await expect(word('1000')).toHaveCSS('color', 'rgb(138, 75, 8)')
    await expect(word("' the first three large payments")).toHaveCSS('color', 'rgb(95, 92, 84)')
    await expect(word("' the first three large payments")).toHaveCSS('font-style', 'italic')
    await expect(word('Matched')).toHaveCSS('background-color', 'rgb(230, 239, 242)')
    await expect(word('Exit For')).toHaveCSS('text-decoration-line', 'underline')
    await expect(word('Exit For')).toHaveCSS('text-decoration-style', 'dotted')
    await expect(coder.locator('.cm-lintRange')).toHaveCount(0)
  })

  test('marks in the gutter the lines that answer, skip a record or end the scan', async ({
                                                                                            page,
                                                                                          }) => {
    await stubAbstraction(page)
    await open(page)
    await write(
      page,
      [
        'If Cursor.IsCurrent Then Continue For',
        'If Cursor.Matches >= 3 Then Exit For',
        'Matched = Payload.Amount > 1000',
      ].join('\n'),
    )

    const gutter = page.getByTestId('rule-coder').locator('.cm-scan-gutter')
    const mark = (title: string) => gutter.locator('.cm-scan-mark').and(page.getByTitle(title))

    await expect(gutter.locator('.cm-scan-mark')).toHaveCount(3)
    await expect(mark('Skips to the next record')).toHaveAttribute('data-mark', 'skip')
    await expect(mark('Ends the scan')).toHaveAttribute('data-mark', 'stop')
    await expect(mark('Sets the answer')).toHaveAttribute('data-mark', 'answer')

    const skip = await mark('Skips to the next record').boundingBox()
    const stop = await mark('Ends the scan').boundingBox()
    const answer = await mark('Sets the answer').boundingBox()

    expect(skip!.y).toBeLessThan(stop!.y)
    expect(stop!.y).toBeLessThan(answer!.y)
  })

  test('warns about a misspelt field before the compiler answers and fixes it from the suggestion', async ({
                                                                                                             page,
                                                                                                           }) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'Return Payload.Amont > 100')

    const coder = page.getByTestId('rule-coder')
    const warning = coder.locator('.cm-lintRange-warning')

    await expect(warning).toHaveText('Payload.Amont')
    await warning.hover()

    const tip = page.locator('.cm-tooltip-lint')

    await expect(tip).toContainText('Payload.Amont is not a field of this model.')
    await expect(tip).toContainText('Did you mean Payload.Amount?')
    await tip.getByRole('button', {name: 'Use Payload.Amount'}).click()

    await expect(coder.locator('.cm-line').first()).toHaveText('Return Payload.Amount > 100')
    await expect(coder.locator('.cm-lintRange')).toHaveCount(0)
  })

  test('warns about a word the rule type does not allow and a member the cursor does not have', async ({
                                                                                                         page,
                                                                                                       }) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'If Cursor.Positon = 3 Then Return True\nWhile True')

    const warnings = page.getByTestId('rule-coder').locator('.cm-lintRange-warning')

    await expect(warnings).toHaveText(['Positon', 'While'])
    await warnings.first().hover()
    await expect(page.locator('.cm-tooltip-lint')).toContainText(
      'Cursor has no member called Positon.',
    )
    await page.mouse.move(0, 0)
    await warnings.last().hover()
    await expect(page.locator('.cm-tooltip-lint')).toContainText(
      'While is not allowed in this kind of rule.',
    )
  })

  test('offers to assign a discarded result to Matched', async ({page}) => {
    await stubAbstraction(page, (text) =>
      text.includes('Matched')
        ? {message: 'Compiled'}
        : {
          message: 'Error',
          errorSpans: [
            {
              code: 'ResultDiscarded',
              message: 'Line 1: the result is not used.',
              start: 0,
              length: 21,
              line: 0,
              column: 0,
              endColumn: 21,
            },
          ],
        },
    )
    await open(page)
    await write(page, 'Payload.Amount > 1000')

    const coder = page.getByTestId('rule-coder')

    await expect(coder.locator('.cm-lintRange-error')).toHaveText('Payload.Amount > 1000')
    await coder.locator('.cm-lintRange-error').hover()
    await page
      .locator('.cm-tooltip-lint')
      .getByRole('button', {name: 'Assign to Matched'})
      .click()

    await expect(coder.locator('.cm-line').first()).toHaveText('Matched = Payload.Amount > 1000')
    await expect(page.getByTestId('compile-status')).toContainText('Compiled')
    await expect(coder.locator('.cm-lintRange')).toHaveCount(0)
  })

  test('closes an If block when Enter is pressed after Then', async ({page}) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'If Payload.Amount > 1000 Then')
    await page.keyboard.press('Enter')
    await page.keyboard.type('Return True')

    const lines = page.getByTestId('rule-coder').locator('.cm-line')

    await expect(lines).toHaveText(['If Payload.Amount > 1000 Then', '  Return True', 'End If'])

    await page.keyboard.press('Control+Home')
    await page.keyboard.press('End')
    await page.keyboard.press('Enter')

    await expect(lines).toHaveCount(4)
    await expect(lines.filter({hasText: 'End If'})).toHaveCount(1)
  })

  test('offers the cursor members with what each one means, and the cursor patterns', async ({
                                                                                               page,
                                                                                             }) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'Matched = Cursor.')

    const list = page.locator('.cm-tooltip-autocomplete')

    await page.keyboard.press('Control+Space')
    await expect(list).toContainText('Members')
    await expect(list.getByRole('option')).toContainText(['IsCurrent', 'Matches', 'Position'])
    await page.keyboard.type('Pos')
    await expect(list.getByRole('option')).toHaveCount(1)
    await expect(page.locator('.cm-completionInfo')).toContainText(
      'One-based position of the transaction being tested.',
    )
    await page.keyboard.press('Enter')
    await expect(page.getByTestId('rule-coder').locator('.cm-line').first()).toHaveText(
      'Matched = Cursor.Position',
    )

    await write(page, 'The')
    await expect(list).toContainText('Patterns')
    await list.getByRole('option', {name: /The Nth record/}).click()

    await expect(page.getByTestId('rule-coder').locator('.cm-line')).toHaveText([
      'Matched = Cursor.Position = 3',
      'If Cursor.Position >= 3 Then Exit For',
    ])
  })

  test('explains a cursor member and a scan word on hover', async ({page}) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'If Cursor.Matches >= 3 Then Exit For')

    const coder = page.getByTestId('rule-coder')
    const hover = page.locator('.cm-rule-hover')

    await coder.locator('.cm-line span').getByText('Matches', {exact: true}).hover()
    await expect(hover).toContainText('Cursor.Matches As Integer')
    await expect(hover).toContainText('How many transactions this rule has accepted so far.')

    await page.mouse.move(0, 0)
    await coder.locator('.cm-line span').getByText('Exit For', {exact: true}).hover()
    await expect(hover).toContainText('Finishes with this transaction and ends the walk.')
  })

  test('accepts a declared variable, offers it with the methods of its type and says how long it lives', async ({
                                                                                                                  page,
                                                                                                                }) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'Static declines As Integer\ndeclines += 1\nMatched = declines = 3')

    const coder = page.getByTestId('rule-coder')
    const word = (text: string) => coder.locator('.cm-line span').getByText(text, {exact: true})

    await expect(coder.locator('.cm-lintRange')).toHaveCount(0)
    await expect(word('Static')).toHaveCSS('color', 'rgb(0, 95, 127)')
    await expect(word('Integer')).toHaveCSS('color', 'rgb(0, 95, 127)')

    await coder
      .locator('.cm-line')
      .nth(1)
      .hover({position: {x: 12, y: 8}})
    await expect(page.locator('.cm-rule-hover')).toContainText('declines As Integer')
    await expect(page.locator('.cm-rule-hover')).toContainText('Kept for the whole scan')
    await page.mouse.move(0, 0)

    const list = page.locator('.cm-tooltip-autocomplete')

    await page.keyboard.press('Control+End')
    await page.keyboard.press('Enter')
    await page.keyboard.type('Matched = decl')
    await expect(list).toContainText('Variables')
    await list.getByRole('option', {name: /declines/}).click()
    await expect(coder.locator('.cm-line').last()).toHaveText('Matched = declines')
    await expect(list).toHaveCount(0)
    await page.keyboard.type('.')
    await page.keyboard.press('Control+Space')
    await expect(list.getByRole('option', {name: /Between/})).toHaveCount(1)
    await page.keyboard.press('Escape')

    await expect(coder.locator('.cm-line').last()).toHaveText('Matched = declines.')
  })

  test('warns that a variable is not allowed where the rule type has no variables', async ({
                                                                                             page,
                                                                                           }) => {
    await stub(page, undefined, 1)
    await page.route('**/api/RuleVocabulary*', (route) =>
      route.fulfill({
        json: abstractionVocabulary.filter(
          (entry) => !['Dim', 'Static', 'As', 'Cursor'].includes(entry.name),
        ),
      }),
    )
    await open(page)
    await write(page, 'Static declines As Integer\nReturn True')

    await expect(page.getByTestId('rule-coder').locator('.cm-lintRange-warning')).toHaveText([
      'Static',
      'declines',
      'As',
    ])
  })

  test('has no accessibility violations with the scan gutter and a warning showing', async ({
                                                                                              page,
                                                                                            }) => {
    await stubAbstraction(page)
    await open(page)
    await write(page, 'If Cursor.Matches >= 3 Then Exit For\nMatched = Payload.Amont > 1000')
    await expect(page.getByTestId('rule-coder').locator('.cm-lintRange-warning')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({page}).analyze()

    expect(results.violations).toEqual([])
  })

  test('has no accessibility violations while the builder is locked', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)
    await page.getByTestId('rule-coder').locator('.cm-content').click()
    await page.keyboard.type('x')
    await expect(page.getByTestId('builder-locked')).toBeVisible()
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
      .include('[data-testid="builder-coder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })

  test('has no accessibility violations', async ({page}) => {
    await stub(page)
    await open(page)
    await expect(page.getByTestId('rule-builder').locator('.rule-container')).toHaveCount(1)
    await settleAnimations(page)

    const results = await new AxeBuilder({page})
      .include('[data-testid="builder-coder"]')
      .analyze()

    expect(
      results.violations.map(
        (v) => `${v.id}: ${v.nodes.map((n) => n.html.slice(0, 90)).join(' | ')}`,
      ),
    ).toEqual([])
  })
})
