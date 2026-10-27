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
export function filenameFromDisposition(disposition: string | null, fallback: string): string {
  if (!disposition) return fallback

  const encoded = /filename\*\s*=\s*(?:UTF-8|utf-8)''([^;]+)/.exec(disposition)?.[1]

  if (encoded) {
    try {
      return decodeURIComponent(encoded.trim())
    } catch {
      return fallback
    }
  }

  const plain = /filename\s*=\s*(?:"([^"]*)"|([^;\n]*))/.exec(disposition)
  const name = (plain?.[1] ?? plain?.[2] ?? '').trim()

  return name || fallback
}

export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')

  anchor.href = url
  anchor.download = filename
  anchor.click()

  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
