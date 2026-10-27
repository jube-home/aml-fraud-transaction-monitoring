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

import type { CaseStatusChoice } from './types'

export function escalationTarget(
  statuses: CaseStatusChoice[],
  currentGuid: string,
): CaseStatusChoice | null {
  const current = statuses.find((choice) => choice.guid === currentGuid)

  if (!current) return null

  const configured = current.escalationTargetCaseWorkflowStatusGuid

  if (configured) {
    const target = statuses.find((choice) => choice.guid === configured)

    if (target) return target
  }

  if (current.priority === undefined) return null

  const stronger = statuses
    .filter((choice) => choice.priority !== undefined && choice.priority < current.priority!)
    .sort((a, b) => b.priority! - a.priority!)

  return stronger[0] ?? null
}

export function notePriorityOf(statuses: CaseStatusChoice[], currentGuid: string): number {
  return statuses.find((choice) => choice.guid === currentGuid)?.priority ?? 3
}
