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

import {StreamLanguage, type StreamParser} from '@codemirror/language'
import {classHighlighter, highlightTree} from '@lezer/highlight'

type Loader = () => Promise<StreamParser<unknown>>

const loaders: Record<string, Loader> = {
  sql: () => import('@codemirror/legacy-modes/mode/sql').then((m) => m.pgSQL),
  cs: () => import('@codemirror/legacy-modes/mode/clike').then((m) => m.csharp),
  java: () => import('@codemirror/legacy-modes/mode/clike').then((m) => m.java),
  js: () => import('@codemirror/legacy-modes/mode/javascript').then((m) => m.javascript),
  ts: () => import('@codemirror/legacy-modes/mode/javascript').then((m) => m.typescript),
  json: () => import('@codemirror/legacy-modes/mode/javascript').then((m) => m.json),
  xml: () => import('@codemirror/legacy-modes/mode/xml').then((m) => m.xml),
  html: () => import('@codemirror/legacy-modes/mode/xml').then((m) => m.html),
  css: () => import('@codemirror/legacy-modes/mode/css').then((m) => m.css),
  py: () => import('@codemirror/legacy-modes/mode/python').then((m) => m.python),
  sh: () => import('@codemirror/legacy-modes/mode/shell').then((m) => m.shell),
  yaml: () => import('@codemirror/legacy-modes/mode/yaml').then((m) => m.yaml),
  ini: () => import('@codemirror/legacy-modes/mode/properties').then((m) => m.properties),
  toml: () => import('@codemirror/legacy-modes/mode/toml').then((m) => m.toml),
  docker: () => import('@codemirror/legacy-modes/mode/dockerfile').then((m) => m.dockerFile),
  ps: () => import('@codemirror/legacy-modes/mode/powershell').then((m) => m.powerShell),
}

const aliases: Record<string, string> = {
  sql: 'sql',
  postgres: 'sql',
  postgresql: 'sql',
  plpgsql: 'sql',
  tsql: 'sql',
  cs: 'cs',
  csharp: 'cs',
  'c#': 'cs',
  java: 'java',
  js: 'js',
  javascript: 'js',
  jsx: 'js',
  node: 'js',
  ts: 'ts',
  typescript: 'ts',
  json: 'json',
  json5: 'json',
  xml: 'xml',
  xsd: 'xml',
  xsl: 'xml',
  svgxml: 'xml',
  html: 'html',
  htm: 'html',
  cshtml: 'html',
  razor: 'html',
  css: 'css',
  scss: 'css',
  less: 'css',
  py: 'py',
  python: 'py',
  sh: 'sh',
  bash: 'sh',
  shell: 'sh',
  zsh: 'sh',
  console: 'sh',
  yml: 'yaml',
  yaml: 'yaml',
  ini: 'ini',
  conf: 'ini',
  properties: 'ini',
  toml: 'toml',
  dockerfile: 'docker',
  docker: 'docker',
  ps1: 'ps',
  powershell: 'ps',
  pwsh: 'ps',
}

const parsers = new Map<string, Promise<StreamLanguage<unknown>>>()

export const highlightedMarker = 'data-jooby-highlighted'

export function highlightKeyFor(language: string | undefined): string | undefined {
  return language ? aliases[language.trim().toLowerCase()] : undefined
}

export function languageOf(code: Element): string | undefined {
  return /(?:^|\s)language-([^\s]+)/.exec(code.className)?.[1]
}

function languageFor(key: string): Promise<StreamLanguage<unknown>> {
  let loaded = parsers.get(key)

  if (loaded === undefined) {
    loaded = loaders[key]!().then((parser) => StreamLanguage.define(parser))
    parsers.set(key, loaded)
  }

  return loaded
}

export function highlightedNodes(
  document: Document,
  source: string,
  language: StreamLanguage<unknown>,
): Node[] {
  const nodes: Node[] = []
  let position = 0

  const plain = (to: number): void => {
    if (to > position) nodes.push(document.createTextNode(source.slice(position, to)))
  }

  highlightTree(language.parser.parse(source), classHighlighter, (from, to, classes) => {
    plain(from)

    const span = document.createElement('span')

    span.className = classes
    span.textContent = source.slice(from, to)
    nodes.push(span)
    position = to
  })

  plain(source.length)

  return nodes
}

export async function highlightCodeBlocks(root: ParentNode): Promise<void> {
  const blocks = Array.from(
    root.querySelectorAll<HTMLElement>(`pre code:not([${highlightedMarker}])`),
  )

  await Promise.all(
    blocks.map(async (block) => {
      const key = highlightKeyFor(languageOf(block))

      block.setAttribute(highlightedMarker, key === undefined ? 'skipped' : 'pending')

      if (key === undefined) return

      try {
        const language = await languageFor(key)
        const source = block.textContent ?? ''

        if (!block.isConnected) return

        block.replaceChildren(...highlightedNodes(block.ownerDocument, source, language))
        block.setAttribute(highlightedMarker, 'done')
      } catch {
        block.setAttribute(highlightedMarker, 'skipped')
      }
    }),
  )
}
