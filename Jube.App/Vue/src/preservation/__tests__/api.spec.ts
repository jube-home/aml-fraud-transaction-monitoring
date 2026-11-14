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
import {decode} from '@msgpack/msgpack'
import {beforeEach, describe, expect, it, vi} from 'vitest'

const useApi = vi.hoisted(() => vi.fn())

vi.mock('../../api/useApi', () => ({useApi, useApiDownload: vi.fn()}))

import {encodeWire, resetWire, setWireSchema} from '../../api/wire'
import {preservationApi} from '../api'

beforeEach(() => {
  useApi.mockReset()
  useApi.mockResolvedValue({})
  resetWire()
})

describe('preservationApi package transport', () => {
  it.each([
    ['previewPackage', '/api/Preservation/ImportPreview'],
    ['importPackage', '/api/Preservation/Import'],
  ] as const)(
    '%s sends the password and the package bytes in one serialised body, never a form',
    async (method, path) => {
      const file = new File([new Uint8Array([1, 2, 3, 250])], 'abc.jemp')

      await preservationApi[method](file, 'hunter2-hunter2')

      const [url, options] = useApi.mock.calls[0]!

      expect(url).toBe(path)
      expect(options.method).toBe('POST')
      expect(options.formData).toBeUndefined()
      expect(options.query).toBeUndefined()
      expect(options.body.password).toBe('hunter2-hunter2')
      expect(options.body.fileName).toBe('abc.jemp')
      expect(options.body.package).toBeInstanceOf(Uint8Array)
      expect([...options.body.package]).toEqual([1, 2, 3, 250])
    },
  )
})

describe('a bytes member on the wire', () => {
  it('encodes as a MessagePack binary value that decodes to the same bytes', () => {
    setWireSchema({
      hash: 'b',
      types: {
        'Jube.Package': {
          fields: [
            {index: 0, name: 'password', type: 'string'},
            {index: 1, name: 'package', type: 'bytes'},
          ],
        },
      },
      routes: [],
    })

    const bytes = new Uint8Array([0, 1, 2, 3, 255])
    const packed = encodeWire('Jube.Package', {password: 'p', package: bytes})
    const decoded = decode(packed) as [string, Uint8Array]

    expect(decoded[0]).toBe('p')
    expect(decoded[1]).toBeInstanceOf(Uint8Array)
    expect([...decoded[1]]).toEqual([0, 1, 2, 3, 255])
  })

  it('encodes an absent bytes member as nil', () => {
    setWireSchema({
      hash: 'b',
      types: {'Jube.Package': {fields: [{index: 0, name: 'package', type: 'bytes'}]}},
      routes: [],
    })

    expect(decode(encodeWire('Jube.Package', {}))).toEqual([null])
  })
})
