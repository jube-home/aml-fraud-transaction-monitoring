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

import {caseSqlApi} from '../caseSqlApi'

beforeEach(() => {
  useApi.mockReset()
  useApi.mockResolvedValue({ok: true})
})

describe('caseSqlApi', () => {
  it('posts the sql and the case id to the test endpoint and nothing else', async () => {
    await caseSqlApi.test('SELECT 1 WHERE 1 = @Id', 42)

    expect(useApi).toHaveBeenCalledTimes(1)
    expect(useApi).toHaveBeenCalledWith('/api/CaseWorkflowDisplayExecution/TestSql', {
      method: 'POST',
      body: {sql: 'SELECT 1 WHERE 1 = @Id', caseId: 42},
    })
  })

  it('passes a failing result through unchanged so the editor can show its error', async () => {
    const failed = {ok: false, error: 'refused', columns: [], rows: [], rowCount: 0}
    useApi.mockResolvedValue(failed)

    await expect(caseSqlApi.test('DROP TABLE x', 1)).resolves.toBe(failed)
  })
})
