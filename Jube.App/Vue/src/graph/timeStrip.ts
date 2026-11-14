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

export interface TimeStripBin {
  index: number
  from: number
  to: number
}

export function parseTimeStripBins(svg: string): TimeStripBin[] {
  if (typeof DOMParser === 'undefined') return []

  const parsed = new DOMParser().parseFromString(svg, 'image/svg+xml')

  if (parsed.querySelector('parsererror')) return []

  const bins: TimeStripBin[] = []

  for (const element of parsed.querySelectorAll('[data-bin]')) {
    const index = Number(element.getAttribute('data-bin'))
    const from = Number(element.getAttribute('data-from'))
    const to = Number(element.getAttribute('data-to'))

    if (Number.isFinite(index) && Number.isFinite(from) && Number.isFinite(to))
      bins.push({index, from, to})
  }

  return bins.sort((a, b) => a.index - b.index)
}
