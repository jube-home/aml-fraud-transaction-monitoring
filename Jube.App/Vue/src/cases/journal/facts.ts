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

import type {Adaptation, ContributionItem} from './types'

export const staleDays = 365

export function trainedAgeDays(trained: string | null | undefined, now: Date): number | null {
  if (!trained) return null

  const at = new Date(trained).getTime()

  if (!Number.isFinite(at)) return null

  return Math.max(0, Math.floor((now.getTime() - at) / 86_400_000))
}

export type ScoreKind = 'probability' | 'score'

export function scoreKind(adaptation: Adaptation): ScoreKind {
  const calibration = adaptation.calibration

  return calibration?.calibrated === true && calibration.space?.toLowerCase() === 'probability'
    ? 'probability'
    : 'score'
}

export type Direction = 'higher' | 'lower' | 'undeclared'

export function directionOf(item: ContributionItem): Direction {
  if (item.direction === null) return 'undeclared'
  if (item.direction > 0) return 'higher'
  if (item.direction < 0) return 'lower'

  return 'undeclared'
}

export function shownInputs(adaptation: Adaptation): ContributionItem[] {
  return (adaptation.contribution?.items ?? []).filter((item) => !item.hidden && !!item.name)
}

export function unlabelled(item: ContributionItem): boolean {
  return !item.humanLabel
}

export function sourceKey(adaptation: Adaptation): 'exhaustive' | 'advanced' | 'http' {
  return adaptation.source
}
