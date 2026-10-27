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

import { ref } from 'vue'

export interface PaneState {
  left: boolean
  right: boolean
}

const prefix = 'jube.panes.'

export function readPanes(key: string): PaneState {
  try {
    const raw = localStorage.getItem(prefix + key)
    const parsed: unknown = raw ? JSON.parse(raw) : null

    if (parsed && typeof parsed === 'object') {
      const { left, right } = parsed as Record<string, unknown>

      return { left: left !== false, right: right !== false }
    }
  } catch {
    return { left: true, right: true }
  }

  return { left: true, right: true }
}

export function usePanes(key: string) {
  const start = readPanes(key)
  const left = ref(start.left)
  const right = ref(start.right)

  function save(): void {
    try {
      localStorage.setItem(prefix + key, JSON.stringify({ left: left.value, right: right.value }))
    } catch {
      return
    }
  }

  function toggleLeft(): void {
    left.value = !left.value
    save()
  }

  function toggleRight(): void {
    right.value = !right.value
    save()
  }

  return { left, right, toggleLeft, toggleRight }
}
