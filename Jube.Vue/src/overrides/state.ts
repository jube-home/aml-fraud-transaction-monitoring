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

import type { OverrideKind } from './types'

export const kinds: { value: OverrideKind; key: 'suppress' | 'force' }[] = [
  { value: 0, key: 'suppress' },
  { value: 1, key: 'force' },
]

export function kindKey(kind: number | null | undefined): 'suppress' | 'force' {
  return kind === 1 ? 'force' : 'suppress'
}

export function offeredKinds(current: number, enableForce: boolean): OverrideKind[] {
  return kinds
    .filter((kind) => kind.value !== 1 || enableForce || current === 1)
    .map((kind) => kind.value)
}
