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

export const historyActionKeys: Record<number, string> = {
  1: 'caseDetail.checklist.history.action.shown',
  2: 'caseDetail.checklist.history.action.hidden',
  3: 'caseDetail.checklist.history.action.checked',
  4: 'caseDetail.checklist.history.action.unchecked',
  5: 'caseDetail.checklist.history.action.validationFailed',
  6: 'caseDetail.checklist.history.action.gateBlocked',
  7: 'caseDetail.checklist.history.action.gateOverridden',
  8: 'caseDetail.checklist.history.action.gateBypassed',
  9: 'caseDetail.checklist.history.action.slaBreached',
}

export function historyActionKey(actionId: number): string {
  return historyActionKeys[actionId] ?? 'caseDetail.checklist.history.action.unknown'
}
