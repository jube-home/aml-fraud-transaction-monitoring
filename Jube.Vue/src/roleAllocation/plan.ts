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
export interface RoleItem {
  guid: string
  name: string
}

export interface Assignment {
  id: number
  roleRegistryGuid: string
}

export interface RoleChanges {
  assign: string[]
  unassign: number[]
}

export function planRoleChanges(
  assignments: Assignment[],
  desired: ReadonlySet<string>,
): RoleChanges {
  const stored = new Set(assignments.map((assignment) => assignment.roleRegistryGuid))
  const seen = new Set<string>()
  const unassign: number[] = []

  for (const assignment of assignments) {
    const first = !seen.has(assignment.roleRegistryGuid)

    seen.add(assignment.roleRegistryGuid)

    if (!desired.has(assignment.roleRegistryGuid) || !first) unassign.push(assignment.id)
  }

  return { assign: [...desired].filter((guid) => !stored.has(guid)), unassign }
}

export function changeCount(changes: RoleChanges): number {
  return changes.assign.length + changes.unassign.length
}

export function lookupPath(baseEndpoint: string, parentKeyField: string, guid: string): string {
  return `${baseEndpoint}/By${parentKeyField.charAt(0).toUpperCase()}${parentKeyField.slice(1)}/${guid}`
}
