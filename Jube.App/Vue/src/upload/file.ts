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
export type UploadProblem = 'none' | 'type' | 'empty'

export function extensionOf(name: string): string {
  const dot = name.lastIndexOf('.')

  return dot < 0 ? '' : name.slice(dot).toLowerCase()
}

export function uploadProblem(
  file: { name: string; size: number } | null,
  extensions: string[],
): UploadProblem | null {
  if (!file) return 'none'
  if (extensions.length > 0 && !extensions.includes(extensionOf(file.name))) return 'type'
  if (file.size === 0) return 'empty'

  return null
}
