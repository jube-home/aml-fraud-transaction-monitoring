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

import type { MessageStats } from './types'

export function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;')
}

export function relativeTime(value: string | null | undefined, now = Date.now()): string {
  if (!value) return ''

  const seconds = Math.max(0, Math.floor((now - new Date(value).getTime()) / 1000))

  if (seconds < 60) return 'just now'

  const minutes = Math.floor(seconds / 60)

  if (minutes < 60) return `${minutes}m ago`

  const hours = Math.floor(minutes / 60)

  if (hours < 24) return `${hours}h ago`

  return `${Math.floor(hours / 24)}d ago`
}

export function formatTimestamp(value: string | Date | null | undefined, now = new Date()): string {
  if (!value) return ''

  const date = value instanceof Date ? value : new Date(value)

  if (Number.isNaN(date.getTime())) return ''

  const time = date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  const sameDay =
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate()

  return sameDay
    ? time
    : `${date.toLocaleDateString([], { day: 'numeric', month: 'short' })}, ${time}`
}

const present = (value: number | null | undefined): value is number =>
  value !== null && value !== undefined

export function formatStatParts(stats: Partial<MessageStats>): string[] {
  const parts: string[] = []

  if (present(stats.inputTokens)) {
    let up = `↑${stats.inputTokens}`

    if (present(stats.cachedInputTokens) && stats.inputTokens > 0) {
      const share = Math.round((stats.cachedInputTokens / stats.inputTokens) * 100)

      up += ` / ${stats.cachedInputTokens} (${share}%)`
    }

    parts.push(up)
  }

  if (present(stats.outputTokens)) parts.push(`↓${stats.outputTokens}`)

  if (present(stats.durationMs)) {
    parts.push(
      stats.durationMs >= 1000
        ? `${(stats.durationMs / 1000).toFixed(1)}s`
        : `${Math.round(stats.durationMs)}ms`,
    )
  }

  if (present(stats.joobrikCount)) {
    let joobriks = `${stats.joobrikCount} Joobrik${stats.joobrikCount === 1 ? '' : 's'}`

    if (present(stats.distinctJoobrikCount) && stats.distinctJoobrikCount !== stats.joobrikCount) {
      joobriks += ` (${stats.distinctJoobrikCount} distinct)`
    }

    parts.push(joobriks)
  }

  if (present(stats.totalCost)) {
    parts.push(`$${stats.totalCost.toFixed(stats.totalCost < 1 ? 4 : 2)}`)
  } else if (present(stats.joobrikCount)) {
    parts.push('cost unknown')
  }

  return parts
}

export function formatStats(stats: Partial<MessageStats>): string {
  return formatStatParts(stats).join(' · ')
}

export function joinWithAnd(items: string[]): string {
  if (items.length <= 1) return items.join('')

  return `${items.slice(0, -1).join(', ')} and ${items[items.length - 1]}`
}
