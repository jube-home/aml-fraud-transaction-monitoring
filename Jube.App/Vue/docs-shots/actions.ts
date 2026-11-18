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

import type { Locator, Page } from '@playwright/test'
import type { Shot } from './shots'

type Action = (page: Page, argument: string) => Promise<void>
type Frame = (page: Page, argument: string) => Locator

const model = process.env.JUBE_DOCS_MODEL ?? ''
const demoCase = process.env.JUBE_DOCS_CASE ?? ''
const writes =
  /^(save|create|add|delete|remove|approve|reject|submit|upload|import|restore|requeue|retract|withdraw|release|refuse|take|close|reopen|send|generate|issue|schedule|accept|override|confirm|propose|erase|record|request|start|stop|tag|lift|place|validate|assign|unassign|discard|promote|train|run\s+\S+)\b|\bnow$/i

const reads = [
  /^\/api\/CaseSearch\/Run$/,
  /^\/api\/CaseAnalytics\/(Summary|Chart\/\w+)$/,
  /^\/api\/CaseKeyJournal\/(Rows|Analytics|Activations|Around|Track|Chart\/\w+)$/,
  /^\/api\/CaseKeyJournal\/Transaction(\/Activations|\/Chart\/\w+)?$/,
  /^\/api\/CaseWorkflowDisplayExecution(\/Dispatches|\/FormEntries|\/Form)?$/,
  /^\/api\/CaseSupervision\/Workload$/,
  /^\/api\/VisualisationRegistryDatasource\/(\d+\/Card|Introspect|CheckSql|Preview)$/,
  /^\/api\/Graph\/(Neighbourhood|Explode|Render|TimeStrip\/Svg|Paths|Flow|Patterns\/Search|Node)$/,
  /^\/api\/EntityApproval\/ValueStatus$/,
  /^\/api\/Erasure\/Preview$/,
  /^\/api\/Chart\//,
  /^\/api\/Parser$/,
]

const anchors = new WeakMap<Page, Locator>()

const still = `
  main footer.sticky { position: static !important; }
  [data-testid="toasts"], [data-testid="widget-launcher"] { visibility: hidden !important; }
  *, *::before, *::after { caret-color: transparent !important; }
`

function rows(scope: Locator): Locator {
  return scope.locator('.tabulator-row')
}

async function surface(page: Page): Promise<Locator> {
  const dialogs = page.locator('dialog[open]')

  return (await dialogs.count()) > 0 ? dialogs.last() : page.locator('body')
}

function phrase(text: string): string {
  return text
    .split(/\s+/)
    .map((word) => word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))
    .join('\\s+')
}

function titleOf(title: string): RegExp {
  return new RegExp(`^\\s*(\\d+\\s+)?${phrase(title)}`, 'i')
}

function headed(scope: Locator, title: string): Locator {
  return scope
    .locator(':is(h1, h2, h3, h4, h5, summary, legend):visible')
    .filter({ hasText: titleOf(title) })
}

function pairOf(argument: string): [string, string] {
  const separator = argument.indexOf('=')

  if (separator < 0) throw new Error(`"${argument}" must be written as label=value`)

  return [argument.slice(0, separator).trim(), argument.slice(separator + 1).trim()]
}

async function firstOf(...candidates: Locator[]): Promise<Locator> {
  const started = Date.now()

  while (Date.now() - started < 4000 + 1000 * candidates.length) {
    for (const [rank, candidate] of candidates.entries())
      if (Date.now() - started >= rank * 1500 && (await candidate.count()) > 0)
        return candidate.first()

    await candidates[0]!.page().waitForTimeout(150)
  }

  return candidates[0]!.first()
}

async function settle(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle', { timeout: 3000 }).catch(() => undefined)
  await page
    .waitForFunction(
      () =>
        ![...document.querySelectorAll('.skeleton, [role="progressbar"]')].some((element) => {
          const box = element.getBoundingClientRect()

          return box.height > 0 && box.bottom > 0 && box.top < innerHeight
        }),
      undefined,
      { timeout: 5000 },
    )
    .catch(() => undefined)
}

async function toTop(target: Locator): Promise<void> {
  await target.evaluate((element) => {
    window.dispatchEvent(new Event('wheel'))
    element.scrollIntoView({ block: 'start' })

    let scroller: Element | undefined
    let top = 0

    for (let parent = element.parentElement; parent; parent = parent.parentElement) {
      const overflow = getComputedStyle(parent).overflowY

      if (overflow !== 'visible') top = Math.max(top, parent.getBoundingClientRect().top)
      if (!scroller && /auto|scroll/.test(overflow) && parent.scrollHeight > parent.clientHeight)
        scroller = parent
    }

    const covered = [...document.querySelectorAll('.sticky')]
      .filter((band) => !band.contains(element) && getComputedStyle(band).top === '0px')
      .map((band) => band.getBoundingClientRect())
      .filter((band) => band.height > 0 && band.top <= top + 32 && band.bottom > top)
      .reduce((lowest, band) => Math.max(lowest, band.bottom - top), 0)

    scroller?.scrollBy(0, element.getBoundingClientRect().top - top - covered - 16)
  })
}

async function steady(page: Page, anchor: Locator): Promise<void> {
  let stable = 0

  for (let pass = 0; pass < 8 && stable < 2; pass += 1) {
    await toTop(anchor)

    const before = (await anchor.boundingBox())?.y

    await settle(page)
    await page.waitForTimeout(500)

    stable = (await anchor.boundingBox())?.y === before ? stable + 1 : 0
  }
}

async function open(row: Locator): Promise<void> {
  await row.waitFor()

  const button = row.getByTestId('grid-action-open')

  await ((await button.count()) > 0 ? button.first() : row).click()
}

async function below(page: Page, candidates: Locator): Promise<Locator> {
  await settle(page)
  await candidates.first().waitFor()

  const floor = (await anchors.get(page)?.boundingBox())?.y ?? 80

  for (const candidate of await candidates.all()) {
    const box = await candidate.boundingBox()

    if (box && box.y + box.height > floor && box.y < 880) return candidate
  }

  return candidates.first()
}

async function grid(page: Page): Promise<Locator> {
  await settle(page)

  const lists = page.locator('[data-testid="crud-list"]:visible')

  return (await lists.count()) > 0
    ? lists.last()
    : below(page, page.locator('main .tabulator:visible'))
}

async function unfold(scope: Locator, title: string): Promise<void> {
  const details = (
    await firstOf(scope.locator('summary').filter({ hasText: titleOf(title) }))
  ).locator('xpath=..')

  if ((await details.getAttribute('open')) === null)
    await details.locator('summary').first().click()

  await details.evaluate((element) => element.scrollIntoView({ block: 'start' }))
}

async function field(page: Page, label: string): Promise<Locator> {
  const scope = await surface(page)
  const controls = scope.locator('input, textarea, select, [contenteditable="true"]')

  return firstOf(
    scope.getByLabel(label, { exact: true }).and(controls),
    scope.getByPlaceholder(label, { exact: true }),
    scope.getByLabel(label).and(controls),
    scope.getByPlaceholder(label),
    scope
      .getByText(label, { exact: true })
      .first()
      .locator(
        'xpath=following::*[self::input or self::textarea or self::select or @contenteditable="true"][1]',
      ),
  )
}

export const actions: Record<string, Action> = {
  none: async () => {},

  'sign-out': async (page) => {
    await page.context().clearCookies()
    await page.reload()
    await page.getByLabel('User name').waitFor()
  },

  'open-default-model': async (page) => {
    const table = page.getByTestId('models-table')
    const row = model ? rows(table).filter({ hasText: model }).first() : rows(table).first()

    await open(row)
    await page.getByTestId('record-beat').first().waitFor()
  },

  'open-demo-case': async (page) => {
    if (demoCase) await page.goto(`/cases/${demoCase}`)
    else await open(rows(page.getByTestId('cases-list')).first())

    await page.getByTestId('case-page').waitFor()
  },

  'open-hub': async (page, label) => {
    const hubs = page.locator('[data-testid^="hub-open-"]')

    await (
      await firstOf(
        hubs.filter({ has: page.getByText(label, { exact: true }) }).last(),
        hubs.filter({ hasText: label }).last(),
      )
    ).click()
  },

  'open-row': async (page, name) => {
    const scope = rows(await grid(page))

    await open(
      await firstOf(
        scope.filter({ has: page.getByText(name, { exact: true }) }),
        scope.filter({ hasText: name }),
      ),
    )
  },

  'open-first-row': async (page) => {
    await open(rows(await grid(page)).first())
  },

  'expand-first-row': async (page) => {
    await (await below(page, page.locator('main [data-testid="grid-expand"]:visible'))).click()
  },

  new: async (page) => {
    await page.getByTestId('crud-new').last().click()
    await page.getByTestId('crud-form').last().waitFor()
  },

  'open-tab': async (page, name) => {
    const scope = await surface(page)

    await (
      await firstOf(
        scope.getByRole('tab', { name, exact: true }),
        scope.getByRole('tab', { name }),
        scope.getByRole('radio', { name }),
      )
    ).click()
  },

  'open-section': async (page, title) => {
    await unfold(page.locator('main'), title)
  },

  'open-menu': async (page, name) => {
    const menu = page.getByRole('navigation').locator('summary').filter({ hasText: name })

    if ((await menu.count()) > 0) await unfold(page.getByRole('navigation'), name)
    else await page.locator('header').getByRole('button', { name }).first().click()
  },

  'open-decide-tile': async (page, name) => {
    const tiles = page.getByTestId('case-decide').getByRole('button')

    await (
      await firstOf(
        tiles.filter({ has: page.getByText(name, { exact: true }) }),
        tiles.filter({ hasText: name }),
      )
    ).click()
    await page.locator('dialog[open]').last().waitFor()
  },

  'scroll-to': async (page, text) => {
    const scope = await surface(page)

    const target = await firstOf(
      headed(scope, text),
      scope.getByText(text, { exact: true }),
      scope.getByText(text),
    )

    anchors.set(page, target)
    await steady(page, target)
  },

  click: async (page, name) => {
    if (writes.test(name)) throw new Error(`click:${name} could change data and is refused`)

    const scope = await surface(page)

    await (
      await firstOf(
        scope.getByRole('button', { name, exact: true }),
        scope.getByRole('button', { name }),
        scope.getByRole('link', { name }),
      )
    ).click()
  },

  fill: async (page, argument) => {
    const [label, value] = pairOf(argument)

    await (await field(page, label)).fill(value)
    await page.waitForTimeout(1200)
  },

  select: async (page, argument) => {
    const [label, value] = pairOf(argument)

    const control = await field(page, label)
    const option = await firstOf(
      control.locator('option').filter({ hasText: new RegExp(`^\\s*${value}\\s*$`) }),
      control.locator('option').filter({ hasText: value }),
    )

    await control.selectOption({ label: (await option.textContent()) ?? value })
  },

  toggle: async (page, argument) => {
    const [label, wanted] = argument.includes('=') ? pairOf(argument) : [argument, '']
    const control = await field(page, label)
    const scope = await surface(page)
    const off = scope.getByLabel(label).and(scope.locator('input:not(:checked):visible'))

    if (wanted) await control.setChecked(wanted === 'on')
    else await ((await off.count()) > 0 ? off.first() : control).click()
  },

  hover: async (page, text) => {
    const scope = await surface(page)

    await (
      await firstOf(
        scope.getByText(text, { exact: true }),
        scope.getByLabel(text),
        scope.getByText(text),
      )
    ).hover()
  },

  'wait-for': async (page, text) => {
    await page.waitForFunction(
      (source) => new RegExp(source, 'i').test(document.body.innerText),
      phrase(text),
    )
  },
}

export const frames: Record<string, Frame> = {
  page: (page) => page.locator('main').first(),
  viewport: (page) => page.locator('body'),
  record: (page) => page.getByTestId('record-beat').last(),
  dialog: (page) =>
    page.locator('dialog[open] > .modal-box, dialog[open]:not(:has(> .modal-box))').last(),
  drawer: (page) => page.locator('[data-testid$="-drawer"]').last(),
  'decide-pane': (page) => page.getByTestId('case-decide'),
  popover: (page) => page.locator('[popover]:popover-open').last(),
  section: (page, heading) =>
    headed(page.locator('main'), heading)
      .first()
      .locator(
        'xpath=ancestor::*[self::section or self::details or self::fieldset or self::article or contains(concat(" ", normalize-space(@class), " "), " card ")][1]',
      ),
}

function frameOf(name: string): [Frame | undefined, string] {
  const separator = name.indexOf(':')
  const key = separator < 0 ? name : name.slice(0, separator)

  return [frames[key], separator < 0 ? '' : name.slice(separator + 1).trim()]
}

export function unsupported(shot: Shot): string[] {
  const verbs = shot.steps.map((step) => step.verb).filter((verb) => !(verb in actions))

  return frameOf(shot.frame)[0] ? verbs : [...verbs, `frame ${shot.frame}`]
}

async function visible(
  target: Locator,
): Promise<{ x: number; y: number; width: number; height: number }> {
  return target.evaluate((element) => {
    let { left, top, right, bottom } = element.getBoundingClientRect()

    for (let parent = element.parentElement; parent; parent = parent.parentElement) {
      if (getComputedStyle(parent).overflowY === 'visible') continue

      const box = parent.getBoundingClientRect()

      left = Math.max(left, box.left)
      top = Math.max(top, box.top)
      right = Math.min(right, box.right)
      bottom = Math.min(bottom, box.bottom)
    }

    left = Math.max(left, 0)
    top = Math.max(top, 0)

    return {
      x: left,
      y: top,
      width: Math.min(right, innerWidth) - left,
      height: Math.min(bottom, innerHeight) - top,
    }
  })
}

export async function capture(page: Page, shot: Shot): Promise<void> {
  const [frame, argument] = frameOf(shot.frame)
  const refused: string[] = []

  if (!frame) throw new Error(`unknown frame "${shot.frame}"`)

  await page.route('**/api/**', (route) => {
    const request = route.request()
    const path = new URL(request.url()).pathname

    if (request.method() === 'GET' || reads.some((read) => read.test(path))) return route.fallback()

    refused.push(`${request.method()} ${path}`)

    return route.abort()
  })
  await page.goto(shot.route)

  for (const step of shot.steps) {
    await actions[step.verb]!(page, step.argument)
    await settle(page)
  }

  await page.evaluate(() => document.fonts.ready)
  await page.addStyleTag({ content: still })

  const target = frame(page, argument)
  const floating = ['viewport', 'dialog', 'drawer', 'popover'].includes(shot.frame)

  await target.waitFor()

  if (shot.steps.at(-1)?.verb !== 'hover') await page.mouse.move(0, 0)
  if (!floating) await steady(page, anchors.get(page) ?? target)
  if (refused.length > 0) throw new Error(`refused as not read-only: ${refused.join(', ')}`)

  if (floating) await target.screenshot({ path: shot.image, animations: 'disabled' })
  else
    await page.screenshot({ path: shot.image, clip: await visible(target), animations: 'disabled' })
}
