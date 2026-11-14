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
import {tokenise} from '../language/tokens'

export type BlockCloser = 'End If' | 'End Select'

type LineKind = 'openIf' | 'closeIf' | 'openSelect' | 'closeSelect' | null

function kindOf(line: string): LineKind {
  const words = tokenise(line)
    .filter((token) => token.type !== 'comment')
    .map((token) => token.text.toLowerCase())

  if (words.length === 0) return null
  if (words[0] === 'end')
    return words[1] === 'if' ? 'closeIf' : words[1] === 'select' ? 'closeSelect' : null
  if (words[0] === 'select' && words[1] === 'case') return 'openSelect'
  if (words[0] === 'if' && words[words.length - 1] === 'then') return 'openIf'

  return null
}

export function blockCloser(lines: string[], lineIndex: number): BlockCloser | null {
  const opened = kindOf(lines[lineIndex] ?? '')

  if (opened !== 'openIf' && opened !== 'openSelect') return null

  const closes = opened === 'openIf' ? 'closeIf' : 'closeSelect'
  let balance = 0

  for (const line of lines) {
    const kind = kindOf(line)

    if (kind === opened) balance++
    else if (kind === closes) balance--
  }

  if (balance <= 0) return null

  return opened === 'openIf' ? 'End If' : 'End Select'
}

export function leadingSpace(line: string): string {
  return /^[ \t]*/.exec(line)![0]
}
