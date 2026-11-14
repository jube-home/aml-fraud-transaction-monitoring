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

import type {ActivationCard} from './types'

export type ActivationSort = 'score' | 'fired' | 'recent' | 'name'

export const activationSorts: readonly ActivationSort[] = ['score', 'fired', 'recent', 'name']

export type BandKey = 'unlikely' | 'possible' | 'likely' | 'veryLikely'

export function bandKey(id: number | null): BandKey | null {
  if (id === 1) return 'unlikely'
  if (id === 2) return 'possible'
  if (id === 3) return 'likely'
  if (id === 4) return 'veryLikely'

  return null
}

export function parseSort(text: string | null): ActivationSort {
  return (activationSorts as readonly string[]).includes(text ?? '')
    ? (text as ActivationSort)
    : 'score'
}

function byName(a: ActivationCard, b: ActivationCard): number {
  return a.name.localeCompare(b.name)
}

export function sortCards(
  cards: readonly ActivationCard[],
  sort: ActivationSort,
): ActivationCard[] {
  const copy = [...cards]

  if (sort === 'name') return copy.sort(byName)
  if (sort === 'fired') return copy.sort((a, b) => b.fired - a.fired || byName(a, b))
  if (sort === 'recent')
    return copy.sort(
      (a, b) => (b.anchorDate ?? '').localeCompare(a.anchorDate ?? '') || byName(a, b),
    )

  return copy.sort((a, b) => {
    if (a.kind !== b.kind) return a.kind === 'model' ? -1 : 1
    if (a.kind === 'model') return (b.value ?? 0) - (a.value ?? 0) || byName(a, b)

    return b.fired - a.fired || byName(a, b)
  })
}

export function ownCards(
  cards: readonly ActivationCard[],
  names: readonly string[],
  own: boolean,
): ActivationCard[] {
  if (!own) return [...cards]

  const wanted = new Set(names.map((name) => name.toLowerCase()))

  return cards.filter((card) => wanted.has(card.name.toLowerCase()))
}
