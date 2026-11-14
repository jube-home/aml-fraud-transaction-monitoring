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

import {afterEach, describe, expect, it, vi} from 'vitest'

afterEach(() => {
  vi.unstubAllEnvs()
  vi.resetModules()
})

async function load() {
  vi.resetModules()

  return import('../mapStyle')
}

function hosts(style: unknown): string[] {
  return [...JSON.stringify(style).matchAll(/https?:\/\/[^"\\\s]+/gi)].map((match) => match[0])
}

describe('the map style', () => {
  it('requests nothing from a remote host unless a style is configured', async () => {
    const {styleFor, localStyles} = await load()

    expect(typeof styleFor(false)).toBe('object')
    expect(typeof styleFor(true)).toBe('object')
    expect(hosts(localStyles.light)).toEqual([])
    expect(hosts(localStyles.dark)).toEqual([])
    expect(JSON.stringify(localStyles.light)).not.toContain('glyphs')
    expect(JSON.stringify(localStyles.light)).not.toContain('sprite')
  })

  it('draws the bundled country outlines under a local style', async () => {
    const {localStyles} = await load()
    const source = localStyles.light.sources.world as {
      data: { geometry: { coordinates: unknown[] } }
    }

    expect(source.data.geometry.coordinates.length).toBeGreaterThan(100)
    expect(localStyles.dark.layers.map((layer) => layer.id)).toEqual(['sea', 'land', 'borders'])
  })

  it('accepts a configured style only when it is https', async () => {
    vi.stubEnv('VITE_JUBE_MAP_STYLE_LIGHT', 'https://tiles.example.test/light.json')
    vi.stubEnv('VITE_JUBE_MAP_STYLE_DARK', 'http://tiles.example.test/dark.json')

    const {styleFor, remoteStyleFor} = await load()

    expect(styleFor(false)).toBe('https://tiles.example.test/light.json')
    expect(remoteStyleFor(true)).toBeNull()
    expect(typeof styleFor(true)).toBe('object')
  })

  it('refuses scripts and relative paths as a style', async () => {
    vi.stubEnv('VITE_JUBE_MAP_STYLE_LIGHT', 'javascript:alert(1)')
    vi.stubEnv('VITE_JUBE_MAP_STYLE_DARK', '/styles/dark.json')

    const {remoteStyleFor} = await load()

    expect(remoteStyleFor(false)).toBeNull()
    expect(remoteStyleFor(true)).toBeNull()
  })
})
