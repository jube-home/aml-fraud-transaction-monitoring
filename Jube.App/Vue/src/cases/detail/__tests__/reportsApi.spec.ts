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
import {
  canDownload,
  canWithdraw,
  isEditable,
  isValidJsonObject,
  mayDownload,
  mayWithdraw,
  reportPermission,
  reportState,
  reportsApi,
  type CaseReport,
} from '../reportsApi'

function report(stateId: number): CaseReport {
  return {
    guid: 'g',
    caseId: 1,
    profileName: 'p',
    profileVersion: 1,
    reportCode: 'STR',
    stateId,
    currentVersion: 1,
    fiuReference: null,
    createdUser: 'u',
    createdDate: '2026-11-11T00:00:00Z',
    fieldsJson: '{}',
    versions: [],
    approvals: [],
    feedback: [],
    issues: [],
  }
}

describe('report state helpers', () => {
  it('lets only drafts and validated reports be edited', () => {
    const editable = Object.values(reportState).filter((id) => isEditable(report(id)))

    expect(editable).toEqual([reportState.draft, reportState.validated])
  })

  it('lets a report be withdrawn only until it is submitted', () => {
    const withdrawable = Object.values(reportState).filter((id) => canWithdraw(report(id)))

    expect(withdrawable).toEqual([
      reportState.draft,
      reportState.validated,
      reportState.pendingApproval,
    ])
  })

  it('allows a download only once approved and never for a withdrawn report', () => {
    const downloadable = Object.values(reportState).filter((id) => canDownload(report(id)))

    expect(downloadable).toEqual([
      reportState.approved,
      reportState.submitted,
      reportState.acknowledged,
      reportState.rejected,
    ])
  })

  it('addresses the download of a report by its guid', () => {
    expect(reportsApi.downloadUrl('abc')).toBe('/api/CaseReport/Download/abc')
  })
})

describe('isValidJsonObject', () => {
  it('accepts only a JSON object', () => {
    expect(isValidJsonObject('{"a":1}')).toBe(true)
    expect(isValidJsonObject('{}')).toBe(true)
    expect(isValidJsonObject('[1]')).toBe(false)
    expect(isValidJsonObject('"x"')).toBe(false)
    expect(isValidJsonObject('null')).toBe(false)
    expect(isValidJsonObject('{broken')).toBe(false)
    expect(isValidJsonObject('')).toBe(false)
  })
})

describe('report buttons against what the server allows', () => {
  const approved = report(reportState.approved)
  const draft = report(reportState.draft)

  it('offers the download to a filer or an approver, never to a viewer', () => {
    expect(mayDownload(approved, [reportPermission.file])).toBe(true)
    expect(mayDownload(approved, [reportPermission.approve])).toBe(true)
    expect(mayDownload(approved, [reportPermission.view])).toBe(false)
    expect(mayDownload(draft, [reportPermission.file])).toBe(false)
  })

  it('offers withdrawal to the user who started the report or to an approver', () => {
    expect(mayWithdraw(draft, [reportPermission.view], 'U')).toBe(true)
    expect(mayWithdraw(draft, [reportPermission.approve], 'someone')).toBe(true)
    expect(mayWithdraw(draft, [reportPermission.view], 'someone')).toBe(false)
    expect(mayWithdraw(draft, [reportPermission.file], undefined)).toBe(false)
    expect(mayWithdraw(approved, [reportPermission.approve], 'u')).toBe(false)
  })
})
