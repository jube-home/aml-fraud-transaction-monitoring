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
import type {Extension} from '@codemirror/state'
import {EditorView, GutterMarker, gutter} from '@codemirror/view'
import {tokenise} from '../language/tokens'

export type LineMark = 'stop' | 'skip' | 'answer'

export function lineMarkOf(line: string): LineMark | null {
  const tokens = tokenise(line).filter((token) => token.type !== 'comment')
  const scan = tokens.find((token) => token.type === 'scan')

  if (scan) return scan.text.toLowerCase().startsWith('exit') ? 'stop' : 'skip'

  const answers = tokens.some((token, at) => {
    if (token.type !== 'answer') return false
    if (token.text.toLowerCase() === 'return') return true

    const before = tokens[at - 1]?.text.toLowerCase()
    const starts = before === undefined || before === 'then' || before === 'else'

    return starts && line.slice(token.to).trimStart().startsWith('=')
  })

  return answers ? 'answer' : null
}

class ScanMarker extends GutterMarker {
  constructor(
    private readonly mark: LineMark,
    private readonly label: string,
  ) {
    super()
  }

  override eq(other: ScanMarker): boolean {
    return other.mark === this.mark && other.label === this.label
  }

  override toDOM(): HTMLElement {
    const dom = document.createElement('span')

    dom.className = `cm-scan-mark cm-scan-mark-${this.mark}`
    dom.title = this.label
    dom.dataset.mark = this.mark

    return dom
  }
}

export function scanGutter(labels: Record<LineMark, string>): Extension[] {
  return [
    gutter({
      class: 'cm-scan-gutter',
      lineMarker(view, line) {
        const mark = lineMarkOf(view.state.doc.lineAt(line.from).text)

        return mark ? new ScanMarker(mark, labels[mark]) : null
      },
      lineMarkerChange: (update) => update.docChanged,
    }),
    EditorView.theme({
      '.cm-scan-gutter': {width: '1rem'},
      '.cm-scan-gutter .cm-gutterElement': {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      },
      '.cm-scan-mark': {display: 'inline-block', boxSizing: 'border-box'},
      '.cm-scan-mark-stop': {
        width: '0.5rem',
        height: '0.5rem',
        backgroundColor: 'var(--color-code-language)',
      },
      '.cm-scan-mark-skip': {
        width: '0',
        height: '0',
        borderBlock: '0.3rem solid transparent',
        borderInlineStart: '0.5rem solid var(--color-code-language)',
      },
      '.cm-scan-mark-answer': {
        width: '0.5rem',
        height: '0.5rem',
        borderRadius: '50%',
        backgroundColor: 'var(--color-accent)',
      },
    }),
  ]
}
