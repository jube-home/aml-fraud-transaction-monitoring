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

import {describe, expect, it} from 'vitest'
import {highlightCodeBlocks, highlightKeyFor, languageOf} from '../highlight'

function host(html: string): HTMLElement {
  const element = document.createElement('div')

  element.innerHTML = html
  document.body.append(element)

  return element
}

describe('highlightKeyFor', () => {
  it('maps aliases and ignores case', () => {
    expect(highlightKeyFor('PostgreSQL')).toBe('sql')
    expect(highlightKeyFor('c#')).toBe('cs')
    expect(highlightKeyFor('mermaid')).toBeUndefined()
    expect(highlightKeyFor(undefined)).toBeUndefined()
  })
})

describe('languageOf', () => {
  it('reads the language class', () => {
    const code = document.createElement('code')

    code.className = 'hljs language-sql'

    expect(languageOf(code)).toBe('sql')
  })
})

describe('highlightCodeBlocks', () => {
  it('wraps sql tokens in classed spans and keeps the text identical', async () => {
    const root = host('<pre><code class="language-sql">select 1 -- one</code></pre>')

    await highlightCodeBlocks(root)

    const code = root.querySelector('code')!

    expect(code.textContent).toBe('select 1 -- one')
    expect(code.querySelector('.tok-keyword')?.textContent).toBe('select')
    expect(code.querySelector('.tok-comment')).not.toBeNull()
    expect(code.getAttribute('data-jooby-highlighted')).toBe('done')
  })

  it('never turns code into markup', async () => {
    const root = host(
      '<pre><code class="language-html">&lt;img src=x onerror=alert(1)&gt;</code></pre>',
    )

    await highlightCodeBlocks(root)

    expect(root.querySelector('img')).toBeNull()
    expect(root.querySelector('code')!.textContent).toBe('<img src=x onerror=alert(1)>')
  })

  it('leaves unknown languages alone and does not repeat work', async () => {
    const root = host(
      '<pre><code class="language-mermaid">graph TD</code></pre><pre><code>plain</code></pre>',
    )

    await highlightCodeBlocks(root)
    await highlightCodeBlocks(root)

    const codes = root.querySelectorAll('code')

    expect(codes[0]!.querySelector('span')).toBeNull()
    expect(codes[1]!.getAttribute('data-jooby-highlighted')).toBe('skipped')
  })
})
