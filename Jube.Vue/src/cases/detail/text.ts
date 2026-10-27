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

import type { CaseFieldEntry } from './types'

export function plainText(html: string | null): string {
  if (!html) return ''

  const parsed = new DOMParser().parseFromString(html, 'text/html')

  parsed.body.querySelectorAll('script, style, template').forEach((node) => node.remove())

  return (parsed.body.textContent ?? '').replace(/\s+/g, ' ').trim()
}

export function matchesFormat(entry: CaseFieldEntry): boolean {
  return entry.conditionalRegularExpressionFormatting ? entry.existsMatch : true
}
