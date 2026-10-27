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

import { parseState, serialiseState, type CasesPageState } from './pageState'

const storageKey = 'jube.cases.last'

export function saveLastUsed(state: CasesPageState): void {
  if (state.workflow === '') return

  try {
    const query = serialiseState({ ...state, caseId: null, tab: 'queue' })

    window.localStorage.setItem(storageKey, JSON.stringify(query))
  } catch {
    return
  }
}

export function loadLastUsed(): CasesPageState | null {
  try {
    const raw = window.localStorage.getItem(storageKey)

    if (!raw) return null

    const stored = JSON.parse(raw) as Record<string, string>
    const state = parseState(stored)

    return state.workflow === '' ? null : state
  } catch {
    return null
  }
}
