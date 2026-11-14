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

const hexColour = /^#(?:[0-9a-f]{3}|[0-9a-f]{6})$/i

export function statusStyle(
  back: string | null | undefined,
  fore: string | null | undefined,
): string | undefined {
  const background = back?.trim() ?? ''
  const foreground = fore?.trim() ?? ''

  if (!hexColour.test(background)) return undefined

  return `background-color:${background};color:${hexColour.test(foreground) ? foreground : 'inherit'}`
}
