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

export function cssColourHex(token: string): string | null {
  const probe = document.createElement('span')

  probe.style.color = `var(${token})`
  probe.style.display = 'none'
  document.body.append(probe)

  const resolved = getComputedStyle(probe).color

  probe.remove()

  const canvas = document.createElement('canvas')

  canvas.width = 1
  canvas.height = 1

  const context = canvas.getContext('2d', { willReadFrequently: true })

  if (!context || !resolved) return null

  context.fillStyle = resolved
  context.fillRect(0, 0, 1, 1)

  const [red, green, blue] = context.getImageData(0, 0, 1, 1).data

  return `#${[red, green, blue].map((channel) => (channel ?? 0).toString(16).padStart(2, '0')).join('')}`
}
