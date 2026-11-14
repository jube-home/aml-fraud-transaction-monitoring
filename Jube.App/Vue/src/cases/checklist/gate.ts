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

import {
  itemState,
  type ChecklistGate,
  type ChecklistGateContext,
  type ChecklistView,
} from './types'

const emptyGate: ChecklistGate = {names: [], blocked: false, override: null, canOverride: false}

export function openItemNames(view: ChecklistView | undefined): string[] {
  if (!view) return []

  return view.checklists
    .flatMap((group) => group.items)
    .filter((item) => item.stateId === itemState.shown)
    .map((item) => item.name ?? '')
}

export function deriveGate(
  view: ChecklistView | undefined,
  context: ChecklistGateContext,
): ChecklistGate {
  if (!view || !context.open) return emptyGate

  const names = openItemNames(view)
  const override = view.override ?? null
  const unresolved = names.length > 0 && override === null

  return {
    names,
    blocked: unresolved,
    override,
    canOverride: unresolved && context.permitted && !context.mine,
  }
}

export function progressOf(view: ChecklistView | undefined): { done: number; required: number } {
  return {done: view?.done ?? 0, required: view?.required ?? 0}
}
