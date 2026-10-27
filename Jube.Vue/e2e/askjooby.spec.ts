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
import { settleAnimations, setTheme, stubRuleBuilder } from './support'

const me = {
  userName: 'alex',
  landlord: false,
  permissions: [65],
  tenantRegistryId: 1,
  canChangePassword: true,
  askJoobyInference: true,
  sessionExpiry: null,
}

function frames(...items: object[]): string {
  return [
    ...items.map((item) => `event: item\ndata: ${JSON.stringify(item)}\n\n`),
    'event: complete\ndata: {}\n\n',
  ].join('')
}

interface Stubs {
  sent: unknown[]
  guidance: unknown[]
  started: number
}

async function stubJooby(
  page: Page,
  options: { reply?: object[]; greeting?: object[] } = {},
): Promise<Stubs> {
  const stubs: Stubs = { sent: [], guidance: [], started: 0 }

  await stubRuleBuilder(page)
  await page.route('**/api/Me', (route) => route.fulfill({ json: { ...me, ...options.me } }))
  await page.route(/\/api\/AskJoobyConversation(\?.*)?$/, (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({
        json: { id: 1, title: null, createdDate: new Date().toISOString(), updatedDate: null },
      })
    }

    return route.fulfill({
      json: [
        {
          id: 1,
          title: 'Velocity rules',
          createdDate: new Date().toISOString(),
          updatedDate: null,
        },
      ],
    })
  })
  await page.route('**/api/AskJoobyConversation/1/Messages', (route) => route.fulfill({ json: [] }))
  await page.route('**/api/AskJoobyConversation/1/Start', (route) => {
    stubs.started += 1

    return route.fulfill({
      contentType: 'text/event-stream',
      body: frames(
        ...(options.greeting ?? [
          { kind: 'Text', text: 'Hello, I am Jooby.', messageId: 10 },
          { kind: 'Stats', inputTokens: 10, outputTokens: 5, durationMs: 1200 },
        ]),
      ),
    })
  })
  await page.route('**/api/AskJoobyConversation/1/SendMessage', async (route) => {
    stubs.sent.push(route.request().postDataJSON())

    if (options.delayMs) await new Promise((resolve) => setTimeout(resolve, options.delayMs))

    return route.fulfill({
      contentType: 'text/event-stream',
      body: frames(
        ...((typeof options.reply === 'function'
          ? options.reply(stubs.sent.length)
          : options.reply) ?? [
          { kind: 'Step', text: 'Searching the pasture', detail: 'query: velocity', iconId: 3 },
          { kind: 'Text', text: 'Three ' },
          { kind: 'Text', text: 'rules found.', messageId: 11 },
        ]),
      ),
    })
  })
  await page.route('**/api/AskJoobyConversation/1/Guidance', (route) => {
    stubs.guidance.push(route.request().postDataJSON())

    return route.fulfill({ status: 204 })
  })
  await page.route('**/api/AskJoobyConversation/1/Messages/*/Feedback', (route) =>
    route.fulfill({ json: 'Noted, thank you.' }),
  )
  await page.route('**/api/ServiceChange/Stream', (route) =>
    route.fulfill({ contentType: 'text/event-stream', body: ': keep-alive\n\n' }),
  )
  await page.route('**/api/AskJoobyConversation/StartupStats', (route) =>
    route.fulfill({ json: { bites: 12, trails: 3, saltlicks: 4, joobriks: 5, yokes: 6 } }),
  )
  await page.route('**/api/AntiforgeryToken', (route) =>
    route.fulfill({ json: { token: 'a'.repeat(40) } }),
  )

  return stubs
}

async function type(page: Page, text: string): Promise<void> {
  const input = page.locator('deep-chat').locator('#text-input')

  await input.click()
  await page.keyboard.type(text)
  await page.keyboard.press('Enter')
}

test.describe('Ask Jooby page', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('starts a conversation with the static welcome and no model call, then streams a reply', async ({
    page,
  }) => {
    const stubs = await stubJooby(page)
    await page.goto('/ask-jooby')

    await expect(page.getByTestId('empty-welcome')).toContainText(
      '12 Bites, 3 Trails, 4 Saltlicks, 5 Joobriks on call, 6 Yokes ready, and counting. What brings you in?',
    )
    expect(stubs.started).toBe(0)

    await type(page, 'show velocity rules')
    await expect(page.locator('deep-chat')).toContainText('show velocity rules')
    await expect(page.locator('deep-chat')).toContainText('Three rules found.')
    await expect(page.locator('deep-chat')).toContainText('Searching the pasture')
    await expect(page.getByTestId('empty-welcome')).toHaveCount(0)

    expect(stubs.sent[0]).toMatchObject({ text: 'show velocity rules', attachmentFileIds: [] })
    expect(stubs.started).toBe(0)
  })

  test('draws agent boundaries as bright one-line pills', async ({ page }, testInfo) => {
    await stubJooby(page, {
      reply: [
        { kind: 'Step', text: 'Searching the pasture', detail: 'query: velocity', iconId: 3 },
        { kind: 'Delegation', text: 'Calling in Zubr to read the rules', detail: 'task: rules' },
        { kind: 'Handoff', text: 'Handing the turn to Kozioł' },
        { kind: 'Validation', text: 'Jadek is checking the draft' },
        { kind: 'Revision', text: 'Jadek asked for a revision' },
        { kind: 'Guard', text: 'Guard refused this message' },
        { kind: 'Text', text: 'Done.', messageId: 11 },
      ],
    })
    await page.goto('/ask-jooby')
    await type(page, 'launch agents')
    await expect(page.locator('deep-chat')).toContainText('Done.')

    for (const [kind, text] of [
      ['delegation', 'Calling in Zubr to read the rules'],
      ['handoff', 'Handing the turn to Kozioł'],
      ['validation', 'Jadek is checking the draft'],
      ['revision', 'Jadek asked for a revision'],
      ['guard', 'Guard refused this message'],
    ] as const) {
      const pill = page
        .locator('deep-chat')
        .locator(`.jooby-activity-${kind}`)
        .filter({ hasText: text })
        .first()
      const look = await pill.evaluate((element) => {
        const target = element.tagName === 'DETAILS' ? element.querySelector('summary')! : element
        const style = getComputedStyle(target)

        return { image: style.backgroundImage, height: style.height, radius: style.borderRadius }
      })

      expect(look.image).toContain('linear-gradient')
      expect(look.height).toBe('24px')
      expect(parseFloat(look.radius)).toBeGreaterThan(12)
    }

    await page.screenshot({ path: testInfo.outputPath('banners.png') })
  })

  test('lists conversations, opens one, and deletes with confirmation', async ({ page }) => {
    await stubJooby(page)
    let deleted = false
    await page.route('**/api/AskJoobyConversation/1', (route) => {
      deleted = route.request().method() === 'DELETE'

      return route.fulfill({ status: 204 })
    })
    await page.goto('/ask-jooby')

    const list = page.getByTestId('conversations')

    await expect(list).toContainText('Velocity rules')
    await list.getByRole('button', { name: /Velocity rules/ }).click()
    await expect(page.getByTestId('empty-welcome')).toContainText('What brings you in?')

    await list.getByRole('button', { name: 'Delete conversation' }).click()
    await page.getByTestId('confirm-delete').click()
    await expect.poll(() => deleted).toBe(true)
  })

  test('saves feedback on a reply and locks the buttons', async ({ page }) => {
    await stubJooby(page)
    await page.goto('/ask-jooby')
    await type(page, 'hello')
    await expect(page.locator('deep-chat')).toContainText('Three rules found.')

    const thumbs = page.locator('deep-chat').locator('button.jooby-feedback')

    await thumbs.first().click()
    await expect(page.locator('deep-chat')).toContainText('Noted, thank you.')
    await expect(thumbs.first()).toBeDisabled()
    await expect(thumbs.last()).toBeDisabled()
  })

  test('offers choices from a clarification and sends the ticked ones', async ({ page }) => {
    const stubs = await stubJooby(page, {
      reply: (call) =>
        call === 1
          ? [
              { kind: 'Text', text: 'Which one?', messageId: 12 },
              { kind: 'Clarification', options: ['Velocity', 'Geography'], allowFreeText: true },
            ]
          : [{ kind: 'Text', text: 'Done.', messageId: 13 }],
    })
    await page.goto('/ask-jooby')
    await type(page, 'which rules')

    const options = page.getByTestId('options')

    await expect(options).toBeVisible()
    await expect(options.getByRole('button', { name: 'Go' })).toBeDisabled()
    await options.getByLabel('Velocity').check()
    await options.getByLabel('Geography').check()
    await options.getByRole('button', { name: 'Go' }).click()

    await expect.poll(() => stubs.sent.length).toBe(2)
    expect(stubs.sent[1]).toMatchObject({ text: 'Velocity and Geography' })
    await expect(options).toBeHidden()
  })

  test('sends a second message as guidance while a reply is running', async ({ page }) => {
    const stubs = await stubJooby(page, { delayMs: 1500 })
    await page.goto('/ask-jooby')
    await type(page, 'first')
    await expect(page.getByTestId('stop')).toBeVisible()
    await type(page, 'second thought')

    await expect.poll(() => stubs.guidance.length).toBe(1)
    expect(stubs.guidance[0]).toEqual({ guidance: 'second thought' })
    expect(stubs.sent).toHaveLength(1)
  })

  test('reports a conversation that could not be started instead of failing silently', async ({
    page,
  }) => {
    await stubJooby(page)
    await page.route(/\/api\/AskJoobyConversation(\?.*)?$/, (route) =>
      route.request().method() === 'POST'
        ? route.fulfill({ status: 500 })
        : route.fulfill({ json: [] }),
    )
    await page.goto('/ask-jooby')
    await type(page, 'hello')

    await expect(page.locator('deep-chat')).toContainText('Could not start a conversation')
  })

  test('keeps New conversation for a conversation with content, not an empty one', async ({
    page,
  }) => {
    await stubJooby(page)
    await page.goto('/ask-jooby')

    const create = page.getByTestId('new-conversation')

    await expect(create).toBeDisabled()
    await expect(page.getByTestId('empty-welcome')).toBeVisible()

    await type(page, 'show velocity rules')
    await expect(page.locator('deep-chat')).toContainText('Three rules found.')
    await expect(create).toBeEnabled()

    await create.click()
    await expect(page.getByTestId('empty-welcome')).toBeVisible()
    await expect(page.locator('deep-chat')).not.toContainText('Three rules found.')
    await expect(create).toBeDisabled()
  })

  test('stops a running reply', async ({ page }) => {
    await stubJooby(page, { delayMs: 8000 })
    await page.goto('/ask-jooby')
    await type(page, 'slow one')

    await expect(page.getByTestId('thinking')).toBeVisible()
    await page.getByTestId('stop').click()

    await expect(page.locator('deep-chat')).toContainText('Stopped.')
    await expect(page.getByTestId('stop')).toBeHidden()
    await expect(page.getByTestId('thinking')).toBeHidden()
  })

  test('explains itself when no inference backend is configured', async ({ page }) => {
    await stubJooby(page, { me: { askJoobyInference: false } })
    await page.goto('/ask-jooby')

    await expect(page.getByTestId('offline')).toBeVisible()
    await expect(page.getByTestId('empty-welcome')).toContainText('Jooby')
    await expect(page.getByTestId('empty-welcome')).toContainText('DigitalOcean')
  })

  for (const theme of ['light', 'dark'] as const) {
    test(`no axe violations on the page in ${theme}`, async ({ page }) => {
      await stubJooby(page)
      await page.goto('/ask-jooby')
      await page.getByTestId('account-button').click()
      await setTheme(page, theme)
      await page.keyboard.press('Escape')
      await expect(page.getByTestId('empty-welcome')).toContainText('What brings you in?')
      await settleAnimations(page)

      const { default: AxeBuilder } = await import('@axe-core/playwright')

      expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
    })
  }
})

test.describe('Ask Jooby widget', () => {
  test.use({ viewport: { width: 1440, height: 900 } })

  test('opens from the launcher, greets with the model, remembers the conversation and closes on Escape', async ({
    page,
  }) => {
    const stubs = await stubJooby(page)
    await page.goto('/tools')

    const panel = page.getByTestId('widget-panel')
    const launcher = page.getByTestId('widget-launcher')

    await expect(panel).toBeHidden()
    await launcher.click()
    await expect(panel).toBeVisible()
    await expect(panel.locator('deep-chat')).toContainText('Hello, I am Jooby.')
    expect(stubs.started).toBe(1)
    await expect(launcher).toHaveAttribute('aria-expanded', 'true')

    await page.keyboard.press('Escape')
    await expect(panel).toBeHidden()
    await expect(launcher).toBeFocused()

    expect(await page.evaluate(() => Object.values(localStorage))).toContain('1')
  })

  test('sends the page context with each message', async ({ page }) => {
    const stubs = await stubJooby(page)
    await page.goto('/tools')
    await page.getByTestId('widget-launcher').click()
    await expect(page.getByTestId('widget-panel').locator('deep-chat')).toContainText('Hello')

    const input = page.getByTestId('widget-panel').locator('deep-chat').locator('#text-input')

    await input.click()
    await page.keyboard.type('what is this page')
    await page.keyboard.press('Enter')

    await expect.poll(() => stubs.sent.length).toBe(1)
    expect((stubs.sent[0] as { pageContext: string }).pageContext).toContain('"pageName": "tools"')
  })

  test('is not offered on the Ask Jooby page itself', async ({ page }) => {
    await stubJooby(page)
    await page.goto('/ask-jooby')

    await expect(page.getByTestId('widget-launcher')).toHaveCount(0)
  })

  test('no axe violations with the panel open', async ({ page }) => {
    await stubJooby(page)
    await page.goto('/tools')
    await page.getByTestId('widget-launcher').click()
    await expect(page.getByTestId('widget-panel').locator('deep-chat')).toContainText('Hello')
    await settleAnimations(page)

    const { default: AxeBuilder } = await import('@axe-core/playwright')

    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([])
  })
})
