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
import {
  HighlightStyle,
  StreamLanguage,
  syntaxHighlighting,
  type StreamParser,
} from '@codemirror/language'
import type {Extension} from '@codemirror/state'
import {Tag} from '@lezer/highlight'
import {nextToken, type RuleLexState} from './tokens'

const tag = {
  comment: Tag.define(),
  literal: Tag.define(),
  language: Tag.define(),
  answer: Tag.define(),
  scan: Tag.define(),
  root: Tag.define(),
  field: Tag.define(),
  method: Tag.define(),
}

const style = HighlightStyle.define([
  {tag: tag.language, color: 'var(--color-code-language)', fontWeight: '600'},
  {tag: tag.root, color: 'var(--color-code-data)', fontWeight: '600'},
  {tag: tag.field, color: 'var(--color-code-data)'},
  {tag: tag.method, color: 'var(--color-code-method)'},
  {tag: tag.literal, color: 'var(--color-code-literal)'},
  {tag: tag.comment, color: 'var(--color-code-comment)', fontStyle: 'italic'},
  {
    tag: tag.answer,
    color: 'var(--color-code-language)',
    fontWeight: '600',
    backgroundColor: 'var(--color-code-mark)',
    borderRadius: '0.2rem',
  },
  {
    tag: tag.scan,
    color: 'var(--color-code-language)',
    fontWeight: '600',
    textDecoration: 'underline dotted',
    textUnderlineOffset: '0.2em',
  },
])

export function ruleLanguage(indentOnInput: RegExp): Extension[] {
  const parser: StreamParser<RuleLexState> = {
    name: 'jube-rule',
    startState: () => ({afterDot: false}),
    token(stream, state) {
      if (stream.sol()) state.afterDot = false

      const token = nextToken(stream.string, stream.pos, state)

      stream.pos = token.to

      return token.type === 'word' ? null : token.type
    },
    languageData: {commentTokens: {line: "'"}, indentOnInput},
    tokenTable: tag,
  }

  return [StreamLanguage.define(parser), syntaxHighlighting(style)]
}
