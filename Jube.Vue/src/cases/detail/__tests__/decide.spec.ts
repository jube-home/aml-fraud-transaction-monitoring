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
import { describe, expect, it } from 'vitest'
import { escalationTarget, notePriorityOf } from '../decide'
import type { CaseStatusChoice } from '../types'

const statuses: CaseStatusChoice[] = [
  { guid: 'a', name: 'Review', priority: 3, escalationTargetCaseWorkflowStatusGuid: 'c' },
  { guid: 'b', name: 'Queue', priority: 4 },
  { guid: 'c', name: 'Urgent', priority: 1 },
  { guid: 'd', name: 'High', priority: 2 },
]

describe('escalationTarget', () => {
  it('uses the configured escalation target', () => {
    expect(escalationTarget(statuses, 'a')?.guid).toBe('c')
  })

  it('falls back to the nearest stronger status', () => {
    expect(escalationTarget(statuses, 'b')?.guid).toBe('a')
  })

  it('has nothing above the strongest status', () => {
    expect(escalationTarget(statuses, 'c')).toBeNull()
  })

  it('ignores a target that is not in the workflow', () => {
    const lone = [
      { guid: 'x', name: 'X', priority: 2, escalationTargetCaseWorkflowStatusGuid: 'gone' },
    ]

    expect(escalationTarget(lone, 'x')).toBeNull()
  })

  it('is null for an unknown current status', () => {
    expect(escalationTarget(statuses, 'zzz')).toBeNull()
  })
})

describe('notePriorityOf', () => {
  it('is the priority of the current status', () => {
    expect(notePriorityOf(statuses, 'd')).toBe(2)
  })

  it('defaults to medium when unknown', () => {
    expect(notePriorityOf(statuses, 'zzz')).toBe(3)
  })
})
