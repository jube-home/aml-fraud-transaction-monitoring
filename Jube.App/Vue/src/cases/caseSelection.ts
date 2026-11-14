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

import {ref} from 'vue'
import {bulkLimit} from './bulkApi'

export function useCaseSelection() {
  const chosen = new Set<number>()
  const count = ref(0)

  function toggle(caseId: number, on: boolean): boolean {
    if (on) {
      if (chosen.size >= bulkLimit && !chosen.has(caseId)) return false

      chosen.add(caseId)
    } else chosen.delete(caseId)

    count.value = chosen.size

    return true
  }

  function has(caseId: number): boolean {
    return chosen.has(caseId)
  }

  function keepOnly(visible: Iterable<number>): void {
    const keep = new Set(visible)

    for (const id of [...chosen]) if (!keep.has(id)) chosen.delete(id)

    count.value = chosen.size
  }

  function clear(): void {
    chosen.clear()
    count.value = 0
  }

  function ids(): number[] {
    return [...chosen]
  }

  return {count, toggle, has, keepOnly, clear, ids}
}
