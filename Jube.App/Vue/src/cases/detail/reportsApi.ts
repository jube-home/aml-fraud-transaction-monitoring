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

import {useApi} from '../../api/useApi'

export interface ReportIssue {
  stage: number
  code: string
  message: string
  path: string | null
  line: number | null
  position: number | null
}

export interface ReportVersion {
  version: number
  supersedesVersion: number | null
  payloadSha256: string | null
  hasXml: boolean
  createdUser: string
  createdDate: string
}

export interface ReportApproval {
  reportVersion: number
  decisionId: number
  note: string | null
  createdUser: string
  createdDate: string
}

export interface ReportFeedback {
  reportVersion: number
  kindId: number
  message: string | null
  fieldPath: string | null
  receivedDate: string
  recordedUser: string
}

export interface CaseReport {
  guid: string
  caseId: number
  profileName: string
  profileVersion: number
  reportCode: string
  stateId: number
  currentVersion: number
  fiuReference: string | null
  createdUser: string
  createdDate: string
  fieldsJson: string
  versions: ReportVersion[]
  approvals: ReportApproval[]
  feedback: ReportFeedback[]
  issues: ReportIssue[]
}

export interface ReportProfile {
  name: string
  version: number
  description: string | null
}

export const reportState = {
  draft: 1,
  validated: 2,
  pendingApproval: 3,
  approved: 4,
  submitted: 5,
  acknowledged: 6,
  rejected: 7,
  withdrawn: 8,
} as const

export const reportPermission = {file: 75, approve: 76, view: 77} as const

export const reportsKey = (caseId: number) => ['service', 'CaseReport', 'byCase', caseId] as const

export const maximumNote = 1000
export const maximumReference = 200
export const maximumMessage = 2000

const base = '/api/CaseReport'

function post<T>(path: string, body?: unknown) {
  return useApi<T>(`${base}/${path}`, {method: 'POST', body})
}

export const reportsApi = {
  list: (caseId: number) => useApi<CaseReport[]>(`${base}/ByCaseId/${caseId}`),
  profiles: () => useApi<ReportProfile[]>('/api/CaseReportProfile'),
  start: (caseId: number, profileName: string, reportCode: string) =>
    post<CaseReport>('Start', {caseId, profileName, reportCode}),
  save: (guid: string, fieldsJson: string) => post<CaseReport>('Save', {guid, fieldsJson}),
  validate: (guid: string) => post<CaseReport>(`Validate/${guid}`),
  requestApproval: (guid: string) => post<CaseReport>(`RequestApproval/${guid}`),
  approve: (guid: string, note: string | null) => post<CaseReport>('Approve', {guid, note}),
  reject: (guid: string, note: string) => post<CaseReport>('Reject', {guid, note}),
  withdraw: (guid: string) => post<CaseReport>(`Withdraw/${guid}`),
  newVersion: (guid: string) => post<CaseReport>(`NewVersion/${guid}`),
  recordSubmission: (guid: string, fiuReference: string) =>
    post<CaseReport>('RecordSubmission', {guid, fiuReference}),
  recordFeedback: (
    guid: string,
    kindId: number,
    message: string | null,
    fieldPath: string | null,
  ) => post<CaseReport>('RecordFeedback', {guid, kindId, message, fieldPath}),
  downloadUrl: (guid: string) => `${base}/Download/${guid}`,
}

export function isEditable(report: CaseReport): boolean {
  return report.stateId === reportState.draft || report.stateId === reportState.validated
}

export function canWithdraw(report: CaseReport): boolean {
  return report.stateId <= reportState.pendingApproval
}

export function canDownload(report: CaseReport): boolean {
  return report.stateId >= reportState.approved && report.stateId !== reportState.withdrawn
}

export function mayDownload(report: CaseReport, held: readonly number[]): boolean {
  return (
    canDownload(report) &&
    (held.includes(reportPermission.file) || held.includes(reportPermission.approve))
  )
}

export function mayWithdraw(
  report: CaseReport,
  held: readonly number[],
  userName: string | undefined,
): boolean {
  const own =
    userName !== undefined && report.createdUser.toLowerCase() === userName.toLowerCase()

  return canWithdraw(report) && (own || held.includes(reportPermission.approve))
}

export function isValidJsonObject(text: string): boolean {
  try {
    const value: unknown = JSON.parse(text)

    return typeof value === 'object' && value !== null && !Array.isArray(value)
  } catch {
    return false
  }
}
