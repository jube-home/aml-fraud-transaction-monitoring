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
import {EditorState} from '@codemirror/state'
import {editorLimit, maximumEditorCharacters} from '../editorLimits'

describe('editorLimit', () => {
  it('matches the server limit for SQL and template text', () => {
    expect(maximumEditorCharacters).toBe(65536)
  })

  it('accepts a change that stays within the limit', () => {
    const state = EditorState.create({doc: 'abc', extensions: [editorLimit(10)]})
    const next = state.update({changes: {from: 3, insert: 'defg'}}).state

    expect(next.doc.toString()).toBe('abcdefg')
  })

  it('rejects a paste that would take the document over the limit', () => {
    const state = EditorState.create({doc: 'abc', extensions: [editorLimit(10)]})
    const next = state.update({changes: {from: 3, insert: 'x'.repeat(20)}}).state

    expect(next.doc.toString()).toBe('abc')
  })

  it('still allows deleting from a document that is already at the limit', () => {
    const state = EditorState.create({doc: 'x'.repeat(10), extensions: [editorLimit(10)]})
    const next = state.update({changes: {from: 0, to: 4}}).state

    expect(next.doc.length).toBe(6)
  })
})
