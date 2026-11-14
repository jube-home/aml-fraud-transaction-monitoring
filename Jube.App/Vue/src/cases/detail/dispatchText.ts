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

export function kindText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.messages.kind.form')
  if (id === 2) return t('caseDetail.messages.kind.macro')
  if (id === 3) return t('caseDetail.messages.kind.action')
  if (id === 5) return t('caseDetail.messages.kind.releaseHook')

  return t('caseDetail.messages.kind.status')
}

export function channelText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.messages.channel.email')
  if (id === 2) return t('caseDetail.messages.channel.sms')
  if (id === 3) return t('caseDetail.messages.channel.post')

  return t('caseDetail.messages.channel.get')
}

export function outcomeText(t: Translate, id: number): string {
  if (id === 1) return t('caseDetail.messages.outcome.sent')
  if (id === 2) return t('caseDetail.messages.outcome.failed')

  return t('caseDetail.messages.outcome.refused')
}

export function modeText(t: Translate, id: number): string {
  return id === 2 ? t('caseDetail.messages.mode.sql') : t('caseDetail.messages.mode.payload')
}

export function prettyRows(json: string | null): string {
  if (!json) return ''

  try {
    return JSON.stringify(JSON.parse(json), null, 2)
  } catch {
    return json
  }
}
