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

vi.mock('../../../api/useApi', () => ({useApi}))

import {operationsApi} from '../operationsApi'

beforeEach(() => {
  useApi.mockReset()
  useApi.mockResolvedValue({})
})

describe('operationsApi.attachFile', () => {
  it('sends the case identity and the file bytes in one serialised body, never a form', async () => {
    const file = new File([new Uint8Array([37, 80, 68, 70])], 'statement.pdf', {
      type: 'application/pdf',
    })

    await operationsApi.attachFile(
      {caseKey: 'Account', caseKeyValue: 'ACC-441', caseId: 9041, caseNoteId: 55},
      file,
    )

    const [url, options] = useApi.mock.calls[0]!

    expect(url).toBe('/api/CaseFile/Upload')
    expect(options.method).toBe('POST')
    expect(options.formData).toBeUndefined()
    expect(options.body).toMatchObject({
      caseKey: 'Account',
      caseKeyValue: 'ACC-441',
      caseId: 9041,
      caseNoteId: 55,
      fileName: 'statement.pdf',
      contentType: 'application/pdf',
    })
    expect([...options.body.content]).toEqual([37, 80, 68, 70])
  })
})
