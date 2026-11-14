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
import {readFileSync} from 'node:fs'
import {resolve} from 'node:path'
import {describe, expect, it} from 'vitest'
import {contrastRatio, type Rgb} from '../../../composables/contrast'

function rgb(hex: string): Rgb {
  const value = hex.replace('#', '')

  return {
    r: Number.parseInt(value.slice(0, 2), 16),
    g: Number.parseInt(value.slice(2, 4), 16),
    b: Number.parseInt(value.slice(4, 6), 16),
  }
}

function themes(): Record<string, Record<string, string>> {
  const css = readFileSync(resolve(__dirname, '../../../assets/main.css'), 'utf8')
  const found: Record<string, Record<string, string>> = {}

  for (const block of css.matchAll(/@plugin 'daisyui\/theme' \{([^}]*)\}/g)) {
    const body = block[1]!
    const name = /name: '([^']+)'/.exec(body)![1]!
    const tokens: Record<string, string> = {}

    for (const token of body.matchAll(/(--color-[\w-]+): (#[0-9a-fA-F]{6});/g)) {
      tokens[token[1]!] = token[2]!
    }

    found[name] = tokens
  }

  return found
}

const text = ['language', 'data', 'method', 'literal', 'comment']

describe('code colours', () => {
  it('are defined in both themes', () => {
    const found = themes()

    expect(Object.keys(found).sort()).toEqual(['jube', 'jube-dark'])

    for (const theme of Object.values(found)) {
      for (const role of [...text, 'mark']) expect(theme[`--color-code-${role}`]).toBeDefined()
    }
  })

  it.each(['jube', 'jube-dark'])(
    'read at 4.5 to 1 or better on the editor and active-line surfaces of %s',
    (name) => {
      const theme = themes()[name]!

      for (const role of text) {
        for (const surface of ['--color-base-100', '--color-base-200']) {
          expect(
            contrastRatio(rgb(theme[`--color-code-${role}`]!), rgb(theme[surface]!)),
            `${role} on ${surface}`,
          ).toBeGreaterThanOrEqual(4.5)
        }
      }
    },
  )

  it.each(['jube', 'jube-dark'])('keep the answer readable on its tinted ground in %s', (name) => {
    const theme = themes()[name]!

    expect(
      contrastRatio(rgb(theme['--color-code-language']!), rgb(theme['--color-code-mark']!)),
    ).toBeGreaterThanOrEqual(4.5)
  })
})
