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
import {beforeEach, describe, expect, it, vi} from 'vitest'

const useApi = vi.hoisted(() => vi.fn())

vi.mock('../../api/useApi', () => ({useApi}))

import {sanctionsApi} from '../../sanctions/api'
import {sanctionApi} from '../api'
import {sanctionDefaults} from '../types'

const params = {multiPartString: '  Robert Mugabe ', ...sanctionDefaults}

beforeEach(() => {
  useApi.mockReset()
})

describe('sanctionApi.search', () => {
  it('sends the trimmed text and the three limits as the query of the invoke endpoint', async () => {
    useApi.mockResolvedValue({})

    await sanctionApi.search(params)

    expect(useApi).toHaveBeenCalledWith('/api/Invoke/Sanction', {
      query: {
        multiPartString: 'Robert Mugabe',
        distance: 2,
        maxDistanceRatio: 0.3,
        maxCoverageRatio: 2,
      },
    })
  })

  it('turns a missing aggregation block into empty results rather than undefined', async () => {
    useApi.mockResolvedValue({entries: null, aggregations: null})

    await expect(sanctionApi.search(params)).resolves.toEqual({
      entries: [],
      total: null,
      bySource: [],
    })
  })

  it('passes entries, the total and the per-source breakdown through', async () => {
    const total = {sum: 0, count: 1, confidence: 1}
    const bySource = [{sourceId: 1, sourceName: 'SDN'}]
    const entries = [{id: 1, value: 'robert mugabe', distance: 0}]
    useApi.mockResolvedValue({entries, aggregations: {total, bySource}})

    await expect(sanctionApi.search(params)).resolves.toEqual({entries, total, bySource})
  })
})

describe('sanctionsApi.importFile', () => {
  it('posts the file alone as multipart form data to a route that names the source', async () => {
    useApi.mockResolvedValue({})
    const file = new File(['1,A,B'], 'sdn.csv', {type: 'text/csv'})

    await sanctionsApi.importFile(7, file)

    const [url, options] = useApi.mock.calls[0]!
    const form = options.formData as FormData

    expect(url).toBe('/api/SanctionEntrySource/7/Import')
    expect(options.method).toBe('POST')
    expect([...form.keys()]).toEqual(['files'])
    expect((form.get('files') as File).name).toBe('sdn.csv')
  })
})
