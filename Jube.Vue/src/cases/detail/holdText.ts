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

export type Translate = (key: string) => string

export function stateText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.hold.state.held')
  if (id === 2) return t('caseDetail.hold.state.declined')
  if (id === 3) return t('caseDetail.hold.state.released')

  return t('caseDetail.hold.state.refused')
}

export function stateClass(id: number): string {
  if (id === 1) return 'badge-warning'
  if (id === 2) return 'badge-error'
  if (id === 3) return 'badge-success'

  return 'badge-neutral'
}

export function legKindText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.hold.leg.kind.email')
  if (id === 2) return t('caseDetail.hold.leg.kind.sms')
  if (id === 3) return t('caseDetail.hold.leg.kind.webhook')
  if (id === 4) return t('caseDetail.hold.leg.kind.release')

  return t('caseDetail.hold.leg.kind.refuse')
}

export function legOutcomeText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.hold.leg.outcome.queued')
  if (id === 2) return t('caseDetail.hold.leg.outcome.sent')
  if (id === 3) return t('caseDetail.hold.leg.outcome.failed')

  return t('caseDetail.hold.leg.outcome.refused')
}

export function legOutcomeClass(id: number): string {
  if (id === 2) return 'badge-success'
  if (id === 3 || id === 4) return 'badge-error'

  return 'badge-ghost'
}

export function rangeText(minimum: number, maximum: number): string {
  return minimum === maximum ? String(minimum) : `${minimum}-${maximum}`
}
