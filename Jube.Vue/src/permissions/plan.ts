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
export interface Grant {
  id: number
  roleRegistryId: number
  permissionSpecificationId: number
  active: boolean
  locked: boolean
}

export type Change =
  | { kind: 'create'; permissionSpecificationId: number }
  | { kind: 'activate'; grantId: number }
  | { kind: 'remove'; grantId: number }

export function grantedIds(grants: Grant[]): Set<number> {
  return new Set(
    grants.filter((grant) => grant.active).map((grant) => grant.permissionSpecificationId),
  )
}

export function planChanges(grants: Grant[], desired: ReadonlySet<number>): Change[] {
  const changes: Change[] = []
  const seen = new Set<number>()

  for (const grant of grants) {
    const wanted = desired.has(grant.permissionSpecificationId)
    const first = !seen.has(grant.permissionSpecificationId)

    seen.add(grant.permissionSpecificationId)

    if (grant.locked) continue

    if (wanted && first && !grant.active) changes.push({ kind: 'activate', grantId: grant.id })
    else if ((!wanted && grant.active) || !first)
      changes.push({ kind: 'remove', grantId: grant.id })
  }

  for (const id of desired) {
    if (!seen.has(id)) changes.push({ kind: 'create', permissionSpecificationId: id })
  }

  return changes
}
