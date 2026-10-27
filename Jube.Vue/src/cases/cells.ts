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

import { durationText, relativeText, type Translate } from './duration'
import {
  attentionKeys,
  caseLabel,
  diaryTypeKeys,
  impactKeys,
  laneKeys,
  laneTone,
  likelihoodKeys,
  slaDimensionKeys,
  slaStateKeys,
  slaTone,
  toneClass,
  type Tone,
} from './labels'
import type { CaseRow } from './types'

export interface CellContext {
  t: Translate
  percent: (value: number) => string
  number: (value: number) => string
  nowMs: number
}

function element(tag: string, className = '', text?: string): HTMLElement {
  const created = document.createElement(tag)

  if (className) created.className = className
  if (text !== undefined) created.textContent = text

  return created
}

function pill(text: string, tone: Tone, extra = ''): HTMLElement {
  return element('span', `${toneClass[tone]} whitespace-nowrap ${extra}`.trim(), text)
}

function stack(...children: HTMLElement[]): HTMLElement {
  const holder = element('div', 'flex flex-col gap-0.5 py-0.5 leading-tight')

  holder.append(...children)

  return holder
}

function inline(...children: HTMLElement[]): HTMLElement {
  const holder = element('div', 'flex flex-wrap items-center gap-1.5 py-0.5')

  holder.append(...children)

  return holder
}

export function riskText(row: CaseRow, context: CellContext): string {
  const { risk } = row

  if (!risk.assessed) return context.t('cases.risk.notAssessed')

  return context.t('cases.risk.summary', {
    band: context.t(likelihoodKeys[risk.likelihoodBand] ?? 'cases.likelihood.possible'),
    likelihood: context.percent(risk.likelihood),
    impact: context.t(impactKeys[risk.impact] ?? 'cases.impact.moderate'),
    priority: context.number(risk.priority),
  })
}

export function riskCell(row: CaseRow, context: CellContext): HTMLElement {
  const { risk } = row

  const lane = pill(context.t(laneKeys[risk.lane] ?? 'cases.lane.standard'), laneTone(risk.lane))

  if (risk.laneReason) lane.title = risk.laneReason

  const summary = element(
    'span',
    'whitespace-normal font-mono text-xs tabular-nums ' +
      (risk.assessed ? '' : 'text-base-content/70'),
    riskText(row, context),
  )

  if (risk.assessed && risk.likelihoodBand >= 3) {
    summary.className += ' font-semibold'
  }

  summary.dataset.band = String(risk.likelihoodBand)

  return stack(lane, summary)
}

export function caseCell(row: CaseRow, context: CellContext): HTMLElement {
  const identifier = element('span', 'font-mono text-sm font-semibold', caseLabel(row.caseId))
  const key = element(
    'span',
    'font-mono text-xs text-base-content/70',
    [row.caseKey, row.caseKeyValue].filter((part) => part).join(' · '),
  )
  const age = element(
    'span',
    'font-mono text-xs tabular-nums text-base-content/70',
    context.t('cases.age', { age: durationText(context.t, row.ageMinutes, context.number) }),
  )

  return stack(inline(identifier, age), key)
}

export function whyCell(row: CaseRow, context: CellContext): HTMLElement {
  const driver = row.risk.topDriver ?? row.risk.laneReason ?? context.t('cases.why.none')
  const main = element('span', 'text-sm', driver)
  const children: HTMLElement[] = [main]

  if (row.attention.needed) {
    const reasons = row.attention.reasons
      .map(
        (reason) =>
          reason.text ?? context.t(attentionKeys[reason.code] ?? 'cases.attention.newActivity'),
      )
      .join('; ')

    const flag = pill(context.t('cases.attention.badge'), 'info')

    flag.title = reasons
    children.push(inline(flag, element('span', 'text-xs', reasons)))
  }

  if (row.qaSample) {
    const flag = pill(context.t('cases.qa.badge'), 'neutral')

    flag.title = context.t('cases.qa.hint')
    children.push(inline(flag, element('span', 'text-xs', context.t('cases.qa.hint'))))
  }

  return stack(...children)
}

export function valueText(row: CaseRow, context: CellContext): string {
  return row.value === null ? context.t('cases.value.none') : context.number(row.value)
}

export function dueCell(row: CaseRow, context: CellContext): HTMLElement {
  const worst = row.serviceLevel.worst
  const children: HTMLElement[] = []

  if (worst && row.serviceLevel.state >= 2) {
    const label = context.t(slaStateKeys[worst.state] ?? 'cases.sla.onTrack')
    const dimension = context.t(slaDimensionKeys[worst.dimension] ?? 'cases.sla.resolution')

    children.push(
      pill(label, slaTone(worst.state)),
      element(
        'span',
        'font-mono text-xs tabular-nums',
        `${dimension} · ${relativeText(context.t, worst.dueDate, context.nowMs, context.number)}`,
      ),
    )
  } else if (row.risk.nextDeadline) {
    if (row.diary && row.diaryTypeId > 1) {
      children.push(
        pill(
          context.t(diaryTypeKeys[row.diaryTypeId] ?? 'cases.diary.type.review'),
          row.diaryTypeId >= 3 ? 'error' : 'neutral',
        ),
      )
    }

    children.push(
      element(
        'span',
        'font-mono text-xs tabular-nums',
        relativeText(context.t, row.risk.nextDeadline, context.nowMs, context.number),
      ),
    )
  } else {
    children.push(element('span', 'text-xs text-base-content/70', context.t('cases.sla.noneDue')))
  }

  return stack(...children)
}

export function lockCell(row: CaseRow, context: CellContext): HTMLElement {
  const status = pill(row.statusName ?? '', 'neutral')

  if (row.backColor) status.title = row.statusName ?? ''

  const holder = row.mine
    ? element('span', 'text-xs font-semibold', context.t('cases.lock.you'))
    : row.locked
      ? element('span', 'text-xs', context.t('cases.lock.heldBy', { user: row.lockedUser ?? '' }))
      : element('span', 'text-xs text-base-content/70', context.t('cases.lock.free'))

  return stack(status, holder)
}
