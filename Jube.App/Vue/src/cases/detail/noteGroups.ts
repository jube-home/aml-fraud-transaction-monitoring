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

import type {CaseNoteEntry} from './types'

export const noteSource = {user: 1, system: 2, askJooby: 3} as const
export const noteKind = {note: 1, narrative: 2, document: 4} as const

export interface NarrativeChain {
  caseId: number
  latest: CaseNoteEntry
  earlier: CaseNoteEntry[]
}

export function sourceOf(note: CaseNoteEntry): number {
  return note.sourceId ?? noteSource.user
}

export function kindOf(note: CaseNoteEntry): number {
  return note.kindId ?? noteKind.note
}

export function isOwnNote(note: CaseNoteEntry, userName: string | undefined): boolean {
  return (
    sourceOf(note) === noteSource.user &&
    userName !== undefined &&
    note.createdUser !== null &&
    note.createdUser === userName
  )
}

export function narrativeChains(notes: CaseNoteEntry[]): NarrativeChain[] {
  const byCase = new Map<number, CaseNoteEntry[]>()

  for (const note of notes.filter((entry) => kindOf(entry) === noteKind.narrative)) {
    byCase.set(note.caseId, [...(byCase.get(note.caseId) ?? []), note])
  }

  return [...byCase.entries()]
    .map(([caseId, chain]) => {
      const ordered = [...chain].sort((a, b) => (b.version ?? 1) - (a.version ?? 1))

      return {caseId, latest: ordered[0]!, earlier: ordered.slice(1)}
    })
    .sort((a, b) => b.latest.createdDate.localeCompare(a.latest.createdDate))
}

export function timelineNotes(notes: CaseNoteEntry[]): CaseNoteEntry[] {
  return notes.filter((entry) => kindOf(entry) !== noteKind.narrative)
}

export function safeAddress(url: string): string | null {
  try {
    const parsed = new URL(url)

    return parsed.protocol === 'https:' || parsed.protocol === 'http:' ? parsed.href : null
  } catch {
    return null
  }
}
