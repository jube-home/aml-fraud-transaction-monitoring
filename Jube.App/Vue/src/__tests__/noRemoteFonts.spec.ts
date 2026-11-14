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

import {readdirSync, readFileSync, statSync} from 'node:fs'
import {join} from 'node:path'
import {describe, expect, it} from 'vitest'

const srcDirectory = join(import.meta.dirname, '..')
const projectDirectory = join(srcDirectory, '..')

const remoteHosts = [
  'fonts.googleapis.com',
  'fonts.gstatic.com',
  'use.typekit.net',
  'use.fontawesome.com',
  'cdnjs.cloudflare.com',
  'cdn.jsdelivr.net',
  'unpkg.com',
]

function sourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name)

    if (statSync(path).isDirectory()) {
      return name === '__tests__' || name === 'generated' ? [] : sourceFiles(path)
    }

    return /\.(vue|ts|css|html)$/.test(name) ? [path] : []
  })
}

describe('assets are bundled, never fetched from a remote host', () => {
  const files = [...sourceFiles(srcDirectory), join(projectDirectory, 'index.html')]

  it.each(remoteHosts)('no source file references %s', (host) => {
    const offenders = files.filter((path) => readFileSync(path, 'utf8').includes(host))

    expect(offenders).toEqual([])
  })

  it('keeps an inline font family on the chat element so the chat library never injects its Google font link', () => {
    const chat = readFileSync(join(srcDirectory, 'components', 'askjooby', 'JoobyChat.vue'), 'utf8')

    expect(chat).toMatch(/<deep-chat[^>]*style="font-family: inherit"/s)
  })
})
