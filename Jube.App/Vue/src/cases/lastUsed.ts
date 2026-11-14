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

import {parseState, serialiseState, type CasesPageState} from './pageState'

const storagePrefix = 'jube.cases.last'

export interface LastUsedScope {
  userName: string
  tenantRegistryId: number
}

function storageKey(scope: LastUsedScope | undefined): string | null {
  if (!scope || scope.userName === '') return null

  return `${storagePrefix}:${scope.tenantRegistryId}:${encodeURIComponent(scope.userName)}`
}

export function saveLastUsed(state: CasesPageState, scope: LastUsedScope | undefined): void {
  const key = storageKey(scope)

  if (key === null || state.workflow === '') return

  try {
    const query = serialiseState({...state, caseId: null, tab: 'queue'})

    window.localStorage.setItem(key, JSON.stringify(query))
  } catch {
    return
  }
}

export function loadLastUsed(scope: LastUsedScope | undefined): CasesPageState | null {
  const key = storageKey(scope)

  if (key === null) return null

  try {
    const raw = window.localStorage.getItem(key)

    if (!raw) return null

    const stored = JSON.parse(raw) as Record<string, string>
    const state = parseState(stored)

    return state.workflow === '' ? null : state
  } catch {
    return null
  }
}
