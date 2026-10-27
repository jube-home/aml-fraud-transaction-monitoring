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
export type AreaKey =
  | 'cases'
  | 'rules'
  | 'models'
  | 'lists'
  | 'visualisation'
  | 'security'
  | 'operations'
  | 'analytics'
  | 'other'

export const areaOrder: AreaKey[] = [
  'cases',
  'rules',
  'models',
  'lists',
  'visualisation',
  'security',
  'operations',
  'analytics',
  'other',
]

const areaByPermission: Record<number, AreaKey> = {
  1: 'cases',
  2: 'cases',
  18: 'cases',
  19: 'cases',
  20: 'cases',
  21: 'cases',
  22: 'cases',
  23: 'cases',
  24: 'cases',
  25: 'cases',
  26: 'cases',
  44: 'cases',
  45: 'cases',
  61: 'cases',
  3: 'lists',
  4: 'lists',
  11: 'lists',
  12: 'lists',
  29: 'lists',
  37: 'lists',
  6: 'models',
  7: 'models',
  8: 'models',
  9: 'models',
  10: 'rules',
  13: 'rules',
  14: 'rules',
  15: 'models',
  16: 'models',
  17: 'rules',
  40: 'models',
  60: 'models',
  28: 'visualisation',
  31: 'visualisation',
  32: 'visualisation',
  33: 'visualisation',
  34: 'security',
  35: 'security',
  36: 'security',
  5: 'operations',
  27: 'operations',
  30: 'operations',
  38: 'operations',
  39: 'operations',
  41: 'operations',
  42: 'operations',
  43: 'operations',
  65: 'analytics',
}

export function areaOf(permissionId: number): AreaKey {
  return areaByPermission[permissionId] ?? 'other'
}

export interface PermissionSpecification {
  id: number
  name: string
}

export interface PermissionGroup {
  area: AreaKey
  items: PermissionSpecification[]
}

export function groupPermissions(specifications: PermissionSpecification[]): PermissionGroup[] {
  const sorted = [...specifications].sort((a, b) => a.id - b.id)

  return areaOrder
    .map((area) => ({ area, items: sorted.filter((item) => areaOf(item.id) === area) }))
    .filter((group) => group.items.length > 0)
}
