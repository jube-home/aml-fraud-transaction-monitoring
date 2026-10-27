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

export const laneLetters = ['A', 'B', 'C', 'D'] as const

const laneClasses: Record<string, string> = {
  A: 'border-[oklch(0.62_0.07_250)] bg-[oklch(0.95_0.02_250)] text-[oklch(0.42_0.07_250)]',
  B: 'border-[oklch(0.62_0.07_160)] bg-[oklch(0.95_0.02_160)] text-[oklch(0.42_0.07_160)]',
  C: 'border-[oklch(0.62_0.07_70)] bg-[oklch(0.95_0.02_70)] text-[oklch(0.42_0.07_70)]',
  D: 'border-[oklch(0.62_0.07_320)] bg-[oklch(0.95_0.02_320)] text-[oklch(0.42_0.07_320)]',
}

export function laneClass(letter: string): string {
  return laneClasses[letter] ?? laneClasses.A!
}

export function lettersOf(value: unknown): string[] {
  return typeof value === 'string'
    ? [...value].filter((letter) => laneLetters.includes(letter as 'A'))
    : []
}
