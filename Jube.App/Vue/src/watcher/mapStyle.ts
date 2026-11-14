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

import type {StyleSpecification} from 'maplibre-gl'
import world from './world.json'

const env = import.meta.env as Record<string, string | undefined>

function remote(value: string | undefined): string | null {
  const trimmed = value?.trim() ?? ''

  return /^https:\/\/[^\s]+$/i.test(trimmed) ? trimmed : null
}

export const remoteStyles = {
  light: remote(env.VITE_JUBE_MAP_STYLE_LIGHT),
  dark: remote(env.VITE_JUBE_MAP_STYLE_DARK),
}

function localStyle(land: string, line: string, sea: string): StyleSpecification {
  return {
    version: 8,
    sources: {world: {type: 'geojson', data: world as never}},
    layers: [
      {id: 'sea', type: 'background', paint: {'background-color': sea}},
      {id: 'land', type: 'fill', source: 'world', paint: {'fill-color': land}},
      {
        id: 'borders',
        type: 'line',
        source: 'world',
        paint: {'line-color': line, 'line-width': 0.6},
      },
    ],
  }
}

export const localStyles = {
  light: localStyle('#eef0f2', '#b7bec6', '#dde3e8'),
  dark: localStyle('#2b3138', '#4a525b', '#1b2025'),
}

export function remoteStyleFor(dark: boolean): string | null {
  return dark ? remoteStyles.dark : remoteStyles.light
}

export function styleFor(dark: boolean): StyleSpecification | string {
  return remoteStyleFor(dark) ?? (dark ? localStyles.dark : localStyles.light)
}

export function localStyleFor(dark: boolean): StyleSpecification {
  return dark ? localStyles.dark : localStyles.light
}
