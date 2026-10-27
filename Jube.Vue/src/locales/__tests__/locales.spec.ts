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
import { baseCompile } from '@intlify/message-compiler'
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'

const localeDirectory = import.meta.dirname.replace(/__tests__$/, '')
const srcDirectory = join(localeDirectory, '..')

function flatten(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) return [prefix]

  return Object.entries(value).flatMap(([key, child]) =>
    flatten(child, prefix ? `${prefix}.${key}` : key),
  )
}

function sourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name)

    if (statSync(path).isDirectory()) {
      return name === '__tests__' || name === 'locales' ? [] : sourceFiles(path)
    }

    return /\.(vue|ts)$/.test(name) ? [path] : []
  })
}

const locales = readdirSync(localeDirectory)
  .filter((name) => name.endsWith('.json'))
  .map((name) => ({
    name: name.replace('.json', ''),
    keys: flatten(JSON.parse(readFileSync(join(localeDirectory, name), 'utf8'))).sort(),
  }))

const english = locales.find((locale) => locale.name === 'en')!
const source = sourceFiles(srcDirectory)
  .map((path) => readFileSync(path, 'utf8'))
  .join('\n')

describe('locale files', () => {
  it.each(locales.filter((locale) => locale.name !== 'en'))(
    '$name has exactly the keys of en',
    ({ keys }) => {
      expect(keys.filter((key) => !english.keys.includes(key))).toEqual([])
      expect(english.keys.filter((key) => !keys.includes(key))).toEqual([])
    },
  )

  it.each(['en', 'pl', 'ar'])('%s has messages that all compile', (name) => {
    const messages = JSON.parse(
      readFileSync(join(localeDirectory, `${name}.json`), 'utf8'),
    ) as unknown
    const broken: string[] = []

    function walk(value: unknown, prefix: string): void {
      if (typeof value === 'string') {
        baseCompile(value, {
          onError: (error) => {
            broken.push(`${prefix}: ${error.message}`)
          },
        })

        return
      }

      if (typeof value === 'object' && value !== null) {
        for (const [key, child] of Object.entries(value))
          walk(child, prefix ? `${prefix}.${key}` : key)
      }
    }

    walk(messages, '')

    expect(broken).toEqual([])
  })

  it('uses every key in en somewhere in the source', () => {
    const unused = english.keys.filter(
      (key) => !source.includes(`'${key}'`) && !key.startsWith('locale.'),
    )

    expect(unused).toEqual([])
  })

  it('declares a locale option for every locale file', () => {
    expect(locales.map((locale) => locale.name).sort()).toEqual(
      english.keys
        .filter((key) => key.startsWith('locale.'))
        .map((key) => key.slice(7))
        .sort(),
    )
  })

  it('references only keys that exist in en', () => {
    const referenced = [...source.matchAll(/\bt\('([a-zA-Z0-9_.]+)'/g)].map((match) => match[1]!)

    expect(referenced.filter((key) => !english.keys.includes(key))).toEqual([])
  })
})
