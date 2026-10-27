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
import { dropUnknownFields } from '../ruleBuilder/ruleText'
import type { BuilderGroup } from '../ruleBuilder/types'

export interface InitialRules {
  group: BuilderGroup | undefined
  dropped: string[]
  failed: boolean
}

export const emptyGroup: BuilderGroup = { condition: 'AND', rules: [] }

export function prepareInitialRules(
  stored: string | BuilderGroup | null | undefined,
  knownIds: Set<string>,
): InitialRules {
  if (stored === undefined || stored === null || stored === '') {
    return { group: undefined, dropped: [], failed: false }
  }

  try {
    const parsed = (typeof stored === 'string' ? JSON.parse(stored) : stored) as BuilderGroup

    if (!parsed || !Array.isArray(parsed.rules))
      return { group: undefined, dropped: [], failed: true }

    const cleaned = dropUnknownFields(parsed, (id) => knownIds.has(id))

    return { group: cleaned.group, dropped: cleaned.dropped, failed: false }
  } catch {
    return { group: undefined, dropped: [], failed: true }
  }
}
