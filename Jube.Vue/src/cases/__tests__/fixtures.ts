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

import type { CaseRow } from '../types'

export function row(overrides: Partial<CaseRow> = {}): CaseRow {
  return {
    caseId: 9041,
    caseKey: 'AccountId',
    caseKeyValue: 'ACC-441',
    createdDate: '2026-10-20T09:00:00Z',
    ageMinutes: 4320,
    caseWorkflowGuid: '11111111-1111-1111-1111-111111111111',
    caseWorkflowName: 'AML structuring',
    caseWorkflowStatusGuid: '22222222-2222-2222-2222-222222222222',
    statusName: 'Awaiting review',
    foreColor: '#112233',
    backColor: '#445566',
    statusEnteredDate: '2026-10-20T09:00:00Z',
    locked: false,
    lockedUser: null,
    mine: false,
    closedStatusId: 0,
    diary: false,
    diaryDate: null,
    diaryTypeId: 1,
    diaryType: 'Review',
    qaSample: false,
    value: 45000,
    risk: {
      assessed: true,
      sequence: 2,
      lane: 4,
      laneName: 'Standard',
      laneReason: 'No deadline applies.',
      likelihood: 0.56,
      likelihoodBand: 3,
      likelihoodBandName: 'Likely',
      impact: 4,
      impactName: 'High',
      priority: 62,
      expectedHarm: 16.8,
      method: 1,
      evidenceCount: 212,
      nextDeadline: null,
      deadlineBreached: false,
      topDriver: 'Structuring 9d',
      topDriverFamily: 'Rule',
      assessedDate: '2026-10-23T08:00:00Z',
    },
    attention: {
      needed: false,
      reasons: [],
      lastViewedDate: null,
      changesSince: 0,
      priorityRise: 0,
    },
    serviceLevel: { state: 0, stateName: 'No target', worst: null, measures: [] },
    firstViewedDate: null,
    firstViewedBy: null,
    firstOwnedDate: null,
    firstOwnedBy: null,
    lastEventTypeId: null,
    lastEventUser: null,
    lastEventDate: null,
    ...overrides,
  }
}
