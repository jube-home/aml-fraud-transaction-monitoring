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

const ipv4 = /^(25[0-5]|2[0-4]\d|1?\d?\d)(\.(25[0-5]|2[0-4]\d|1?\d?\d)){3}$/
const ipv6 = /^[0-9a-fA-F:.]+$/

export function isIpLiteral(text: string): boolean {
  const value = text.trim()

  if (value === '') return false
  if (ipv4.test(value)) return true

  return value.includes(':') && ipv6.test(value) && (value.match(/:/g) ?? []).length >= 2
}

export function canLookup(text: string): boolean {
  return isIpLiteral(text)
}
