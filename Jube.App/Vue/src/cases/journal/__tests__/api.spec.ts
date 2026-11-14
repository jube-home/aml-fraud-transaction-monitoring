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
  chartBody,
  chartUrl,
  transactionChartBody,
  transactionChartUrl,
  type JournalQuery,
} from '../api'

const query: JournalQuery = {
  caseWorkflowGuid: '11111111-1111-1111-1111-111111111111',
  key: 'AccountId',
  value: 'A 1&x=2',
  from: '2026-01-01T00:00:00Z',
  lens: 'rules',
  caseId: 7,
  sections: 'summary',
  pinnedOnly: true,
  merge: [{key: 'Ip', value: '10.0.0.1'}],
  activationsOnly: true,
  responseElevation: 5,
  archiveIds: '1,2',
  filter: '{"condition":"AND","rules":[]}',
}

describe('journal chart requests', () => {
  it('sends the scope a chart reads in the body and leaves out what the server ignores for a chart', () => {
    const body = chartBody(query)

    expect(body.caseWorkflowGuid).toBe(query.caseWorkflowGuid)
    expect(body.key).toBe('AccountId')
    expect(body.value).toBe('A 1&x=2')
    expect(body.activationsOnly).toBe(true)
    expect(body.archiveIds).toBe('1,2')

    for (const name of ['lens', 'caseId', 'sections', 'pinnedOnly', 'merge', 'filter'])
      expect(name in body).toBe(false)
  })

  it('addresses a chart with only its scalar parameters', () => {
    const url = new URL(chartUrl('Progression', 'Amount', true), 'http://jube.test')

    expect(url.pathname).toBe('/api/CaseKeyJournal/Chart/Progression')
    expect(url.searchParams.get('field')).toBe('Amount')
    expect(url.searchParams.get('dark')).toBe('true')
    expect([...url.searchParams.keys()].sort()).toEqual(['dark', 'field'])
  })

  it('keeps a hostile value out of the address', () => {
    const url = new URL(chartUrl('Gaps', undefined, false), 'http://jube.test')

    expect(url.search).not.toContain('A 1')
    expect(url.searchParams.has('field')).toBe(false)
    expect(url.searchParams.get('dark')).toBe('false')
  })

  it('addresses a transaction chart with every part encoded and the transaction in the body', () => {
    const url = new URL(
      transactionChartUrl('Gauge', 'Score & <b>x</b>', 'exhaustive', false),
      'http://jube.test',
    )

    expect(url.pathname).toBe('/api/CaseKeyJournal/Transaction/Chart/Gauge')
    expect(url.searchParams.get('adaptation')).toBe('Score & <b>x</b>')
    expect(url.searchParams.get('source')).toBe('exhaustive')
    expect(url.search).not.toContain('<b>')
    expect(transactionChartBody('wf', 12)).toEqual({caseWorkflowGuid: 'wf', id: 12})
  })
})
