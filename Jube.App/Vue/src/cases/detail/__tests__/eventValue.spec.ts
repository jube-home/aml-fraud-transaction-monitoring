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
import {eventValueKind, formatEventValue, type EventValueContext} from '../eventValue'

const open = 'aaaaaaaa-0000-0000-0000-000000000001'

const context: EventValueContext = {
  t: (key) => `t:${key}`,
  dateTime: (value) => `date:${value}`,
  statusName: (guid) => (guid === open ? 'Open' : null),
}

describe('formatEventValue', () => {
  it('renders ISO dates as dates and treats a date without an offset as UTC', () => {
    expect(formatEventValue(10, 'after', '2026-11-05T09:00:00.0000000Z', context)).toBe(
      'date:2026-11-05T09:00:00.0000000Z',
    )
    expect(formatEventValue(12, 'before', '2026-11-05T09:00:00', context)).toBe(
      'date:2026-11-05T09:00:00Z',
    )
  })

  it('leaves legacy text that is not an ISO date as it was written', () => {
    expect(formatEventValue(10, 'before', '11/05/2026 09:00:00', context)).toBe(
      '11/05/2026 09:00:00',
    )
  })

  it('looks up statuses by Guid and keeps legacy names', () => {
    expect(formatEventValue(9, 'before', open, context)).toBe('Open')
    expect(formatEventValue(17, 'after', 'Escalated name', context)).toBe('Escalated name')
    expect(formatEventValue(9, 'after', 'bbbbbbbb-0000-0000-0000-000000000002', context)).toBe(
      't:caseDetail.history.unknownStatus',
    )
  })

  it('renders enumeration ids through their labels and keeps legacy names', () => {
    expect(formatEventValue(7, 'after', '3', context)).toBe('t:cases.diary.type.regulatoryDeadline')
    expect(formatEventValue(19, 'before', '4', context)).toBe('t:cases.diary.type.paymentHold')
    expect(formatEventValue(15, 'after', '3', context)).toBe('t:cases.closedStatus.closed')
    expect(formatEventValue(7, 'after', 'Review', context)).toBe('Review')
  })

  it('shows plain values for users, ratings and the checklist events', () => {
    expect(formatEventValue(14, 'after', 'alex', context)).toBe('alex')
    expect(formatEventValue(11, 'after', '4', context)).toBe('4')
    expect(formatEventValue(23, 'before', 'Check related cases', context)).toBe(
      'Check related cases',
    )
  })

  it('shows nothing for empty values and for the side an event does not use', () => {
    expect(formatEventValue(9, 'before', null, context)).toBe('')
    expect(formatEventValue(19, 'after', 'x', context)).toBe('')
    expect(eventValueKind(2, 'before')).toBe('text')
  })

  it('names the analyst view on both sides of a recorded assessment and before a withdrawal', () => {
    expect(formatEventValue(34, 'before', '1', context)).toBe('t:caseDetail.assessment.low')
    expect(formatEventValue(34, 'after', '3', context)).toBe('t:caseDetail.assessment.high')
    expect(formatEventValue(34, 'before', null, context)).toBe('')
    expect(formatEventValue(35, 'before', '2', context)).toBe('t:caseDetail.assessment.medium')
    expect(formatEventValue(35, 'after', 'x', context)).toBe('')
  })
})
