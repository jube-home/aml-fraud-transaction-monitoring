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

import {noteSource, sourceOf} from './noteGroups'
import type {CaseNoteEntry} from './types'

export type AvatarKind = 'askJooby' | 'system' | 'user'

export interface NoteAvatar {
  kind: AvatarKind
  letter: string
  name: string | null
}

export function firstLetter(userName: string | null): string {
  const first = Array.from((userName ?? '').trim())[0]

  return first ? first.toLocaleUpperCase() : '?'
}

export function avatarOf(note: CaseNoteEntry): NoteAvatar {
  const source = sourceOf(note)

  if (source === noteSource.askJooby) return {kind: 'askJooby', letter: '', name: null}
  if (source === noteSource.system) return {kind: 'system', letter: '', name: null}

  return {kind: 'user', letter: firstLetter(note.createdUser), name: note.createdUser}
}
