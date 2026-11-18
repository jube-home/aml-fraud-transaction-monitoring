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

import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

export interface Step {
  verb: string
  argument: string
}

export interface Shot {
  id: string
  page: string
  image: string
  route: string
  steps: Step[]
  frame: string
  alt: string
}

const fields = ['id', 'route', 'steps', 'frame', 'state', 'conveys', 'alt']
const comment = /\{% comment %\}SHOT\s([\s\S]*?)\{% endcomment %\}/g
const field = new RegExp(
  `(?:^|\\n)\\s*(${fields.join('|')})=([\\s\\S]*?)(?=\\n\\s*(?:${fields.join('|')})=|$)`,
  'g',
)

export const docsRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../../docs')

function pagesUnder(directory: string): string[] {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name)

    if (statSync(path).isDirectory()) return pagesUnder(path)

    return name.endsWith('.md') ? [path] : []
  })
}

function stepsOf(text: string): Step[] {
  return text
    .split(',')
    .map((step) => step.trim())
    .filter((step) => step.length > 0)
    .map((step) => {
      const separator = step.indexOf(':')

      return separator < 0
        ? { verb: step, argument: '' }
        : { verb: step.slice(0, separator).trim(), argument: step.slice(separator + 1).trim() }
    })
}

function shotOf(page: string, body: string): Shot {
  const values = new Map<string, string>()

  for (const match of body.matchAll(field)) values.set(match[1]!, match[2]!.trim())

  const id = values.get('id') ?? ''

  return {
    id,
    page,
    image: join(dirname(page), `${id}.png`),
    route: values.get('route') ?? '',
    steps: stepsOf(values.get('steps') ?? ''),
    frame: values.get('frame') ?? 'page',
    alt: (values.get('alt') ?? '').replace(/\s+/g, ' ').replace(/[[\]]/g, ''),
  }
}

export function readShots(root: string = docsRoot): Shot[] {
  return pagesUnder(root).flatMap((page) =>
    [...readFileSync(page, 'utf8').matchAll(comment)].map((match) => shotOf(page, match[1]!)),
  )
}

export function problemsOf(shots: Shot[]): string[] {
  const seen = new Set<string>()
  const problems: string[] = []

  for (const shot of shots) {
    if (!/^[a-z0-9-]+$/.test(shot.id))
      problems.push(`${shot.page}: id "${shot.id}" is not kebab case`)
    if (seen.has(shot.id)) problems.push(`${shot.page}: id "${shot.id}" is used more than once`)
    if (!shot.route.startsWith('/')) problems.push(`${shot.page}: ${shot.id} has no route`)
    if (/[{}]/.test(shot.route))
      problems.push(`${shot.page}: ${shot.id} has a placeholder in its route`)
    if (shot.alt.length === 0) problems.push(`${shot.page}: ${shot.id} has no alt text`)

    seen.add(shot.id)
  }

  return problems
}

export function syncImages(root: string = docsRoot): { inserted: number; removed: number } {
  let inserted = 0
  let removed = 0

  for (const page of pagesUnder(root)) {
    const before = readFileSync(page, 'utf8')
    const after = before.replace(
      /(\{% comment %\}SHOT\s([\s\S]*?)\{% endcomment %\})(\n\n!\[[^\]]*\]\([^)]*\))?/g,
      (_, marker: string, body: string, line: string | undefined) => {
        const shot = shotOf(page, body)
        const wanted = existsSync(shot.image) ? `\n\n![${shot.alt}](${shot.id}.png)` : ''

        if (wanted && wanted !== line) inserted += 1
        if (!wanted && line) removed += 1

        return marker + wanted
      },
    )

    if (after !== before) writeFileSync(page, after, 'utf8')
  }

  return { inserted, removed }
}
