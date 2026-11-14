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

import {afterEach, describe, expect, it, vi} from 'vitest'
import {datasourceApi} from '../api'

const useApi = vi.hoisted(() => vi.fn())

vi.mock('../../api/useApi', () => ({useApi}))

describe('datasourceApi.checkSql', () => {
  afterEach(() => useApi.mockReset())

  it('posts the command to the parse-only endpoint and returns the findings', async () => {
    useApi.mockResolvedValue({
      valid: false,
      findings: [{code: 'SqlUnknownColumn', message: 'm', line: 1, column: 8, suggestion: 'Id'}],
    })

    const result = await datasourceApi.checkSql({
      visualisationRegistryId: 4,
      command: 'select id',
    })

    expect(useApi).toHaveBeenCalledWith(
      '/api/VisualisationRegistryDatasource/CheckSql',
      expect.objectContaining({method: 'POST'}),
    )
    expect(result.valid).toBe(false)
    expect(result.findings[0]?.suggestion).toBe('Id')
  })

  it('treats a response without findings as valid with none', async () => {
    useApi.mockResolvedValue({valid: true})

    const result = await datasourceApi.checkSql({visualisationRegistryId: 4, command: 'select 1'})

    expect(result).toEqual({valid: true, findings: []})
  })
})
