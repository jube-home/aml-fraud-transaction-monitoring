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

export type SvirfdLocationQuery = Record<string, unknown>

export interface SvirfdParams {
  ip: string
}

function first(value: unknown): string | undefined {
  const single = Array.isArray(value) ? value[0] : value

  return typeof single === 'string' ? single : undefined
}

export function paramsFromLocation(query: SvirfdLocationQuery): SvirfdParams {
  return {ip: (first(query.ip) ?? '').trim()}
}

export function locationFromParams(params: SvirfdParams): Record<string, string> {
  const ip = params.ip.trim()

  return ip === '' ? {} : {ip}
}

export function locationKey(params: SvirfdParams): string {
  return params.ip.trim()
}
