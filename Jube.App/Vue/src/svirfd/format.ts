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

export type Tone = 'neutral' | 'info' | 'success' | 'warning' | 'error'

export function kmBound(rttMs: number): number {
  return Math.max(0, rttMs) * 100
}

export function confidenceTone(confidence: string | null | undefined): Tone {
  switch (confidence) {
    case 'high':
      return 'success'
    case 'medium':
      return 'info'
    case 'low':
      return 'warning'
    default:
      return 'neutral'
  }
}

export function rpkiTone(state: string | null | undefined): Tone {
  switch (state) {
    case 'valid':
      return 'success'
    case 'invalid':
      return 'error'
    default:
      return 'neutral'
  }
}

export function claimKey(value: boolean | null | undefined): 'yes' | 'no' | 'notClaimed' {
  if (value === true) return 'yes'
  if (value === false) return 'no'

  return 'notClaimed'
}

export function toneClass(tone: Tone): string {
  switch (tone) {
    case 'success':
      return 'badge-success'
    case 'info':
      return 'badge-info badge-soft'
    case 'warning':
      return 'badge-warning'
    case 'error':
      return 'badge-error badge-soft'
    default:
      return 'badge-ghost'
  }
}
