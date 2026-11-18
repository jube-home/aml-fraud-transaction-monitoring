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

export const checklistOverridePermission = 71

export const overrideReasonMaximum = 1000

export const itemState = {shown: 1, hidden: 2, done: 3} as const

export const slaState = {
  none: 1,
  onTrack: 2,
  breached: 3,
  completedOnTime: 4,
  completedLate: 5,
} as const

export const historyAction = {
  shown: 1,
  hidden: 2,
  checked: 3,
  unchecked: 4,
  validationFailed: 5,
  gateBlocked: 6,
  gateOverridden: 7,
  gateBypassed: 8,
  slaBreached: 9,
} as const

export interface ChecklistItem {
  id: number
  caseWorkflowChecklistItemGuid: string
  name: string | null
  priority: number
  stateId: number
  firstShownDate: string
  shownDate: string
  dueDate: string | null
  slaBreachedDate: string | null
  completedDate: string | null
  completedUser: string | null
  slaStateId: number
  minutesRemaining: number | null
}

export interface ChecklistGroup {
  caseWorkflowChecklistGuid: string
  name: string | null
  items: ChecklistItem[]
}

export interface ChecklistOverride {
  id: number
  caseId: number
  reason: string
  grantedUser: string
  grantedDate: string
  openItems: string[]
}

export interface ChecklistView {
  caseId: number
  checklists: ChecklistGroup[]
  required: number
  done: number
  open: number
  breached: number
  reconciledDate: string | null
  override: ChecklistOverride | null
  complete: boolean
}

export interface ChecklistHistoryEntry {
  id: number
  actionId: number
  itemName: string | null
  actor: string | null
  createdDate: string
  detail: string | null
}

export interface ChecklistGate {
  names: string[]
  blocked: boolean
  override: ChecklistOverride | null
  own: boolean
  canOverride: boolean
}

export interface ChecklistGateContext {
  open: boolean
  mine: boolean
  permitted: boolean
  userName?: string
}
