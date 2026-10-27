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
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'

const root = join(import.meta.dirname, '..', '..')
const skipDirectories = new Set([
  'node_modules',
  'dist',
  'test-results',
  'playwright-report',
  'coverage',
])
const skipFiles = new Set(['schema.d.ts', 'query-builder.standalone.min.js'])
const extensions = new Set(['.ts', '.vue', '.css', '.mjs', '.js'])

function sourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name)

    if (statSync(path).isDirectory()) return skipDirectories.has(name) ? [] : sourceFiles(path)

    const extension = name.slice(name.lastIndexOf('.'))

    return extensions.has(extension) && !skipFiles.has(name) ? [path] : []
  })
}

describe('copyright headers', () => {
  it('starts every source file with the standard Jube header', () => {
    const missing = sourceFiles(root)
      .filter((path) => {
        const head = readFileSync(path, 'utf8').slice(0, 600)

        return (
          !head.includes('Copyright (C) 2022-present Jube Holdings Limited.') ||
          !head.includes('GNU Affero General Public License')
        )
      })
      .map((path) => path.slice(root.length + 1))

    expect(missing).toEqual([])
  })
})
