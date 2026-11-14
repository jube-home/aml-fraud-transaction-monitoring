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
import {durationText, slaView} from '../slaText'
import {slaState} from '../types'
import {item} from './support'

const t = (key: string, values?: Record<string, unknown>) =>
  `${key.split('.').pop()}:${values ? JSON.stringify(values) : ''}`

describe('slaView', () => {
  it('has nothing to show without an SLA', () => {
    expect(slaView(item({slaStateId: slaState.none}))).toBeNull()
  })

  it('shows the time left on track', () => {
    expect(slaView(item({slaStateId: slaState.onTrack, minutesRemaining: 95}))).toEqual({
      key: 'caseDetail.checklist.sla.dueIn',
      minutes: 95,
      tone: 'neutral',
    })
  })

  it('shows how long overdue in the error tone', () => {
    expect(slaView(item({slaStateId: slaState.breached, minutesRemaining: -70}))).toEqual({
      key: 'caseDetail.checklist.sla.overdue',
      minutes: 70,
      tone: 'error',
    })
  })

  it('shows completed on time as neutral and completed late as neutral too', () => {
    expect(slaView(item({slaStateId: slaState.completedOnTime}))?.tone).toBe('neutral')
    expect(slaView(item({slaStateId: slaState.completedLate}))).toEqual({
      key: 'caseDetail.checklist.sla.late',
      minutes: null,
      tone: 'neutral',
    })
  })
})

describe('durationText', () => {
  it('uses minutes under an hour', () => {
    expect(durationText(t, 45)).toBe('minutes:{"n":45}')
  })

  it('uses the two largest units', () => {
    expect(durationText(t, 125)).toBe('hours:{"n":2} minutes:{"n":5}')
    expect(durationText(t, 1440 + 180 + 7)).toBe('days:{"n":1} hours:{"n":3}')
  })

  it('shows zero minutes rather than nothing', () => {
    expect(durationText(t, 0)).toBe('minutes:{"n":0}')
  })
})
