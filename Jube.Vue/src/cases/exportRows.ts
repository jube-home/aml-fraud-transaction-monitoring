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

import type { SheetColumn } from '../export/xlsx'
import { caseLabel, impactKeys, laneKeys } from './labels'
import type { CaseRow } from './types'

export type Translate = (key: string) => string

export const exportCap = 5000
export const exportPage = 200

export function exportColumns(t: Translate): SheetColumn[] {
  return [
    { title: t('cases.export.col.case'), field: 'case', type: 'text' },
    { title: t('cases.export.col.key'), field: 'key', type: 'text' },
    { title: t('cases.export.col.workflow'), field: 'workflow', type: 'text' },
    { title: t('cases.export.col.status'), field: 'status', type: 'text' },
    { title: t('cases.export.col.holder'), field: 'holder', type: 'text' },
    { title: t('cases.export.col.created'), field: 'created', type: 'date' },
    { title: t('cases.export.col.age'), field: 'age', type: 'number' },
    { title: t('cases.export.col.lane'), field: 'lane', type: 'text' },
    { title: t('cases.export.col.likelihood'), field: 'likelihood', type: 'number' },
    { title: t('cases.export.col.impact'), field: 'impact', type: 'text' },
    { title: t('cases.export.col.priority'), field: 'priority', type: 'number' },
    { title: t('cases.export.col.value'), field: 'value', type: 'number' },
    { title: t('cases.export.col.why'), field: 'why', type: 'text' },
    { title: t('cases.export.col.due'), field: 'due', type: 'text' },
    { title: t('cases.export.col.dueDate'), field: 'dueDate', type: 'date' },
    { title: t('cases.export.col.diary'), field: 'diary', type: 'text' },
    { title: t('cases.export.col.diaryDate'), field: 'diaryDate', type: 'date' },
    { title: t('cases.export.col.qa'), field: 'qa', type: 'text' },
    { title: t('cases.export.col.firstViewed'), field: 'firstViewed', type: 'date' },
    { title: t('cases.export.col.firstViewedBy'), field: 'firstViewedBy', type: 'text' },
    { title: t('cases.export.col.firstOwned'), field: 'firstOwned', type: 'date' },
    { title: t('cases.export.col.firstOwnedBy'), field: 'firstOwnedBy', type: 'text' },
  ]
}

export function flattenRow(row: CaseRow, t: Translate): Record<string, unknown> {
  const assessed = row.risk.assessed

  return {
    case: caseLabel(row.caseId),
    key: row.caseKeyValue,
    workflow: row.caseWorkflowName,
    status: row.statusName,
    holder: row.lockedUser,
    created: row.createdDate,
    age: row.ageMinutes,
    lane: assessed ? t(laneKeys[row.risk.lane] ?? 'cases.lane.standard') : null,
    likelihood: assessed ? row.risk.likelihood : null,
    impact: assessed && row.risk.impact > 0 ? t(impactKeys[row.risk.impact] ?? '') : null,
    priority: assessed ? row.risk.priority : null,
    value: row.value,
    why: row.attention.reasons
      .map((reason) => reason.text ?? '')
      .filter((text) => text !== '')
      .join('; '),
    due: row.serviceLevel.stateName,
    dueDate: row.serviceLevel.worst?.dueDate ?? null,
    diary: row.diary ? row.diaryType : null,
    diaryDate: row.diaryDate,
    qa: row.qaSample ? t('cases.export.yes') : t('cases.export.no'),
    firstViewed: row.firstViewedDate,
    firstViewedBy: row.firstViewedBy,
    firstOwned: row.firstOwnedDate,
    firstOwnedBy: row.firstOwnedBy,
  }
}

function field(value: unknown, type: SheetColumn['type']): string {
  if (value === null || value === undefined) return ''

  const text = type === 'date' ? new Date(String(value)).toISOString() : String(value)

  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text
}

export function toCsv(columns: SheetColumn[], rows: Record<string, unknown>[]): string {
  const header = columns.map((column) => field(column.title, 'text')).join(',')
  const lines = rows.map((row) =>
    columns.map((column) => field(row[column.field], column.type)).join(','),
  )

  return `﻿${[header, ...lines].join('\r\n')}\r\n`
}

export function saveText(text: string, filename: string): void {
  const url = URL.createObjectURL(new Blob([text], { type: 'text/csv;charset=utf-8' }))
  const link = document.createElement('a')

  link.href = url
  link.download = filename
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

export function exportName(extension: string, now: Date): string {
  return `cases-${now.toISOString().slice(0, 10)}.${extension}`
}
