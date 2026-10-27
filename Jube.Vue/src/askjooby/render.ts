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

import { escapeHtml, formatStatParts, formatTimestamp } from './format'
import { toolIconSvg } from './toolIcons'
import type { ActivityKind, Attachment, MessageStats } from './types'

const glyph = (body: string): string =>
  `<svg class="jooby-banner-glyph" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">${body}</svg>`

const bannerGlyphs: Partial<Record<ActivityKind, string>> = {
  Delegation:
    '<img class="jooby-banner-glyph jooby-banner-avatar" src="/images/askjooby-avatar.png" alt="" width="16" height="16">',
  Handoff: glyph(
    '<path d="m11 17 2 2a1 1 0 1 0 3-3"/><path d="m14 14 2.5 2.5a1 1 0 1 0 3-3l-3.88-3.88a3 3 0 0 0-4.24 0l-.88.88a1 1 0 1 1-3-3l2.81-2.81a5.79 5.79 0 0 1 7.06-.87l.47.28a2 2 0 0 0 1.42.25L21 4"/><path d="m21 3 1 11h-2"/><path d="M3 3 2 14l6.5 6.5a1 1 0 1 0 3-3"/><path d="M3 4h8"/>',
  ),
  Validation: glyph(
    '<circle cx="6" cy="15" r="4"/><circle cx="18" cy="15" r="4"/><path d="M14 15a2 2 0 0 0-2-2 2 2 0 0 0-2 2"/><path d="M2.5 13 5 7c.7-1.3 1.4-2 3-2"/><path d="M21.5 13 19 7c-.7-1.3-1.5-2-3-2"/>',
  ),
  Guard: glyph(
    '<path d="M2.586 16.726A2 2 0 0 1 2 15.312V8.688a2 2 0 0 1 .586-1.414l4.688-4.688A2 2 0 0 1 8.688 2h6.624a2 2 0 0 1 1.414.586l4.688 4.688A2 2 0 0 1 22 8.688v6.624a2 2 0 0 1-.586 1.414l-4.688 4.688a2 2 0 0 1-1.414.586H8.688a2 2 0 0 1-1.414-.586z"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/>',
  ),
}

bannerGlyphs.Revision = bannerGlyphs.Validation

const thumbsUp =
  '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" width="14" height="14"><path d="M7 10v12"/><path d="M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2a3.13 3.13 0 0 1 3 3.88Z"/></svg>'

const thumbsDown =
  '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" width="14" height="14"><path d="M17 14V2"/><path d="M9 18.12 10 14H4.17a2 2 0 0 1-1.92-2.56l2.33-8A2 2 0 0 1 6.5 2H20a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-2.76a2 2 0 0 0-1.79 1.11L12 22a3.13 3.13 0 0 1-3-3.88Z"/></svg>'

const paperclip =
  '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" width="14" height="14"><path d="m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l8.57-8.57A4 4 0 1 1 18 8.84l-8.59 8.57a2 2 0 0 1-2.83-2.83l8.49-8.48"/></svg>'

export interface FeedbackLabels {
  good: string
  bad: string
}

export const defaultFeedbackLabels: FeedbackLabels = {
  good: 'Good response, worth remembering',
  bad: 'Bad response, worth remembering as an example to avoid',
}

export const activityRoles: Record<ActivityKind, string> = {
  Step: 'activity',
  Thought: 'thought',
  Thinking: 'thinking',
  Delegation: 'banner',
  Handoff: 'banner',
  Validation: 'banner',
  Revision: 'banner',
  Guard: 'guard',
  Error: 'failure',
}

export function activityHtml(
  kind: ActivityKind,
  text: string,
  detail: string | null | undefined,
  iconId: number | null | undefined,
  timestamp: string | Date | null | undefined,
): string {
  const stamp = formatTimestamp(timestamp)
  const summary =
    `${bannerGlyphs[kind] ?? toolIconSvg(iconId)}<span class="jooby-activity-text">${escapeHtml(text)}</span>` +
    (stamp ? `<time class="jooby-pill jooby-stamp">${escapeHtml(stamp)}</time>` : '')
  const className = `jooby-activity jooby-activity-${kind.toLowerCase()}`

  return detail
    ? `<details class="${className}"><summary>${summary}</summary><pre class="jooby-detail">${escapeHtml(detail)}</pre></details>`
    : `<div class="${className}">${summary}</div>`
}

export function metaHtml(options: {
  timestamp?: string | Date | null
  stats?: Partial<MessageStats>
  messageId?: number | null
  labels?: FeedbackLabels
  chosen?: boolean | null
}): string {
  const parts: string[] = []
  const time = formatTimestamp(options.timestamp)

  if (time) parts.push(time)
  if (options.stats) parts.push(...formatStatParts(options.stats))

  const labels = options.labels ?? defaultFeedbackLabels
  const feedback = options.messageId
    ? [
        [true, thumbsUp, labels.good],
        [false, thumbsDown, labels.bad],
      ]
        .map(([positive, icon, label]) => {
          const chosen = options.chosen === positive
          const disabled =
            options.chosen === null || options.chosen === undefined ? '' : ' disabled'

          return `<button type="button" class="jooby-feedback${chosen ? ' jooby-feedback-chosen' : ''}" data-message-id="${options.messageId}" data-positive="${String(positive)}" aria-label="${escapeHtml(String(label))}"${disabled}>${String(icon)}</button>`
        })
        .join('')
    : ''

  const pills = parts.map((part) => `<span class="jooby-pill">${escapeHtml(part)}</span>`).join('')

  return `<div class="jooby-meta">${pills}${feedback}</div>`
}

export function attachmentsHtml(
  attachments: Attachment[],
  fileUrl: (id: number) => string,
): string {
  return attachments
    .map(
      (attachment) =>
        `<a class="jooby-chip" href="${escapeHtml(fileUrl(attachment.id))}" target="_blank" rel="noopener">${paperclip}${escapeHtml(attachment.name)}</a>`,
    )
    .join('')
}

export function svgDiagramHtml(name: string, svgSource: string): string | undefined {
  const trimmed = svgSource.trim()

  if (!/^<svg[\s>]/i.test(trimmed) || !/<\/svg>\s*$/i.test(trimmed)) return undefined

  const source = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(trimmed)}`

  return `<img class="jooby-diagram" alt="${escapeHtml(name)}" src="${source}">`
}

export function thinkingLabel(agentLabel: string | undefined): string | undefined {
  return agentLabel ? agentLabel : undefined
}
