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

import {useApi} from '../api/useApi'

export interface Replay {
  guid: string
  statusId: number
  agentDefinitionHash: string
  modelName: string | null
  fromDate: string
  toDate: string
  asOfMinutes: number
  casesSelected: number
  scored: number
  unusable: number
  agreed: number
  missedRisk: number
  overEscalation: number
  inconclusive: number
  agreementRate: number | null
  error: string | null
  createdDate: string
  createdUser: string
}

export interface Comparison {
  first: Replay | null
  second: Replay | null
  commonCases: number
  sameRecommendation: number
  firstAgreed: number
  secondAgreed: number
}

export const replayKey = ['service', 'CaseAgentReplay'] as const
export const replayState = {pending: 1, running: 2, completed: 3, failed: 4} as const

const base = '/api/CaseAgentReplay'

export const replayApi = {
  list: () => useApi<Replay[]>(base),
  start: (fromDate: string, toDate: string, maximumCases: number, asOfMinutes: number) =>
    useApi<Replay>(`${base}/Start`, {
      method: 'POST',
      body: {fromDate, toDate, maximumCases, asOfMinutes},
    }),
  compare: (first: string, second: string) =>
    useApi<Comparison>(`${base}/Compare/${first}/${second}`),
}

export function percent(rate: number | null): string {
  return rate === null ? '–' : `${(rate * 100).toFixed(1)}%`
}

export function windowValid(from: string, to: string): boolean {
  return from.length > 0 && to.length > 0 && from <= to
}

export function comparable(selected: string[]): boolean {
  return selected.length === 2 && selected[0] !== selected[1]
}
