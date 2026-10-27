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

import { HighlightStyle, syntaxHighlighting } from '@codemirror/language'
import { EditorView } from '@codemirror/view'
import { tags } from '@lezer/highlight'

const highlight = HighlightStyle.define([
  { tag: [tags.keyword, tags.operatorKeyword], color: 'var(--color-primary)', fontWeight: '600' },
  { tag: [tags.string, tags.special(tags.string)], color: 'var(--color-info)' },
  { tag: [tags.number, tags.bool, tags.atom], color: 'var(--color-base-content)' },
  {
    tag: [tags.comment, tags.lineComment, tags.blockComment],
    color: 'var(--color-secondary)',
    fontStyle: 'italic',
  },
  { tag: [tags.typeName, tags.standard(tags.name)], color: 'var(--color-base-content)' },
  { tag: [tags.tagName, tags.angleBracket], color: 'var(--color-primary)' },
  { tag: tags.attributeName, color: 'var(--color-base-content)', fontWeight: '600' },
  { tag: tags.attributeValue, color: 'var(--color-info)' },
])

export function editorLook() {
  return [
    syntaxHighlighting(highlight, { fallback: false }),
    EditorView.theme({
      '&': {
        backgroundColor: 'var(--color-base-100)',
        color: 'var(--color-base-content)',
        height: '100%',
      },
      '&.cm-focused': { outline: '2px solid var(--color-primary)' },
      '.cm-scroller': { fontFamily: 'var(--font-mono, monospace)', overflow: 'auto' },
      '.cm-gutters': {
        backgroundColor: 'var(--color-base-200)',
        color: 'var(--color-secondary)',
        borderRight: '1px solid var(--color-base-300)',
      },
      '.cm-activeLine, .cm-activeLineGutter': { backgroundColor: 'var(--color-base-200)' },
      '.cm-cursor': { borderLeftColor: 'var(--color-base-content)' },
    }),
  ]
}
