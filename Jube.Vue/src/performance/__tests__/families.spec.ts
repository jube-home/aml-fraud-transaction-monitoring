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

import { describe, expect, it } from 'vitest'
import { existsSync, readdirSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import { performanceFamilies, securityAreas } from '../families'
import en from '../../locales/en.json'

const dtoDirectory = join(import.meta.dirname, '../../../../Jube.Dto')

function backendAreas(): string[] {
  return readdirSync(dtoDirectory, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .flatMap((entry) =>
      readdirSync(join(dtoDirectory, entry.name))
        .filter((name) => name.endsWith('.cs'))
        .flatMap((name) => {
          const text = readFileSync(join(dtoDirectory, entry.name, name), 'utf8')

          return [...text.matchAll(/\[GridEndpoint\("([A-Za-z0-9]+)"/g)].map((match) => match[1]!)
        }),
    )
}

function lookup(key: string): unknown {
  return key
    .split('.')
    .reduce<unknown>((node, part) => (node as Record<string, unknown> | undefined)?.[part], en)
}

describe('performance families', () => {
  const placed = performanceFamilies.flatMap((family) => family.areas.map((item) => item.area))

  it('finds the backend grid areas to compare against', () => {
    expect(existsSync(dtoDirectory)).toBe(true)
    expect(backendAreas().length).toBeGreaterThan(40)
  })

  it('places every backend performance area in exactly one family', () => {
    const wanted = backendAreas().filter((area) => !securityAreas.includes(area))

    for (const area of wanted) {
      expect(
        placed.filter((name) => name === area),
        area,
      ).toHaveLength(1)
    }
  })

  it('names no area the backend does not list', () => {
    expect(placed.filter((area) => !backendAreas().includes(area))).toEqual([])
  })

  it('never repeats an area or a family', () => {
    expect(new Set(placed).size).toBe(placed.length)
    expect(new Set(performanceFamilies.map((family) => family.id)).size).toBe(
      performanceFamilies.length,
    )
  })

  it('keeps no family empty', () => {
    for (const family of performanceFamilies)
      expect(family.areas.length, family.id).toBeGreaterThan(0)
  })

  it('has an English title for every family, hint and area', () => {
    const keys = performanceFamilies.flatMap((family) => [
      family.titleKey,
      family.hintKey,
      ...family.areas.map((item) => item.titleKey),
    ])

    for (const key of keys) expect(typeof lookup(key), key).toBe('string')
  })
})
