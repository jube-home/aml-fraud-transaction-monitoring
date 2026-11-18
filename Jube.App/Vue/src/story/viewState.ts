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

export interface GridViewState {
  search: string
  page: number
  expanded: unknown[]
  glimpses: Record<string, { open: boolean; rows: number[] }>
}

const states = new Map<string, GridViewState>()
const limit = 200

export function viewStateFor(key: string): GridViewState {
  const found = states.get(key)

  if (found) {
    states.delete(key)
    states.set(key, found)

    return found
  }

  const created: GridViewState = {search: '', page: 1, expanded: [], glimpses: {}}

  states.set(key, created)

  if (states.size > limit) states.delete(states.keys().next().value!)

  return created
}

export function forgetViewStates(): void {
  states.clear()
}
