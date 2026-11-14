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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {resetAntiforgeryToken, setApiHandlers, useApi} from '../useApi'
import {
  columnarJsonType,
  decodeColumnarJson,
  decodeWire,
  encodeWire,
  resetWire,
  setWireSchema,
  wireSchemaHeader,
  wireShapeHeader,
} from '../wire'

const serverText =
  '{"count":3,"keys":["id","name"],"columns":[[1,2,null],["a",null,null]],"missing":[[1,[1]]],"nulls":[2]}'

describe('columnar JSON', () => {
  beforeEach(() => {
    resetWire()
    setWireSchema({
      hash: 'j',
      types: {
        'Jube.Grid': {
          fields: [
            {index: 0, name: 'title', type: 'string'},
            {index: 1, name: 'rows', type: 'any{}#'},
          ],
        },
        'Jube.Page': {
          fields: [
            {index: 0, name: 'grids', type: 'Jube.Grid[]'},
            {index: 1, name: 'plain', type: 'Jube.Plain'},
          ],
        },
        'Jube.Plain': {fields: [{index: 0, name: 'label', type: 'string'}]},
      },
      routes: [],
    })
  })

  it('rebuilds exactly the rows the server writes, honouring missing cells and null rows', () => {
    const grid = decodeColumnarJson({title: 't', rows: JSON.parse(serverText)}, 'Jube.Grid') as {
      rows: unknown[]
    }

    expect(grid.rows).toEqual([{id: 1, name: 'a'}, {id: 2}, null])
    expect(Object.keys(grid.rows[1] as object)).toEqual(['id'])
  })

  it('walks lists and nested members by name and leaves plain members alone', () => {
    const page = decodeColumnarJson(
      {
        grids: [{title: 'one', rows: JSON.parse(serverText)}],
        plain: {label: 'x'},
      },
      'Jube.Page',
    ) as { grids: { rows: unknown[] }[]; plain: { label: string } }

    expect(page.grids[0]!.rows).toHaveLength(3)
    expect(page.plain).toEqual({label: 'x'})
  })

  it('returns a value with no row grids untouched', () => {
    const plain = {label: 'x'}

    expect(decodeColumnarJson(plain, 'Jube.Plain')).toBe(plain)
  })

  it('gives the same rows as the MessagePack path for ragged and null rows', () => {
    const rows = [{a: 1, b: 'x'}, null, {b: 'y'}, {a: 4, b: null}, {}]
    const viaMessagePack = decodeWire(
      encodeWire('Jube.Grid', {title: 't', rows}),
      'Jube.Grid',
    ) as { rows: unknown[] }

    const keys = ['a', 'b']
    const columns = keys.map((key) =>
      rows.map((row) => (row && key in row ? row[key as 'a'] : null)),
    )
    const viaJson = decodeColumnarJson(
      {
        title: 't',
        rows: {
          count: rows.length,
          keys,
          columns,
          missing: [
            [0, [2, 4]],
            [1, [4]],
          ],
          nulls: [1],
        },
      },
      'Jube.Grid',
    ) as { rows: unknown[] }

    expect(viaJson.rows).toEqual(viaMessagePack.rows)
  })

  it('does not let a column named __proto__ reach the prototype', () => {
    const grid = decodeColumnarJson(
      {title: 't', rows: {count: 1, keys: ['__proto__'], columns: [[{polluted: true}]]}},
      'Jube.Grid',
    ) as { rows: Record<string, unknown>[] }

    expect(({} as Record<string, unknown>).polluted).toBeUndefined()
    expect(Object.keys(grid.rows[0]!)).toEqual(['__proto__'])
  })
})

describe('useApi with columnar JSON', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    resetAntiforgeryToken()
    resetWire()
    setApiHandlers({})
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('asks for columnar JSON and rebuilds the rows of a response that carries it', async () => {
    const schema = {
      hash: 'j',
      mode: 'Json',
      types: {
        'Jube.Grid': {
          fields: [
            {index: 0, name: 'title', type: 'string'},
            {index: 1, name: 'rows', type: 'any{}#'},
          ],
        },
      },
      routes: [],
    }

    fetchMock
      .mockResolvedValueOnce(new Response(JSON.stringify(schema), {status: 200}))
      .mockResolvedValueOnce(
        new Response(`{"title":"t","rows":${serverText}}`, {
          status: 200,
          headers: {
            'Content-Type': columnarJsonType,
            [wireShapeHeader]: 'Jube.Grid',
            [wireSchemaHeader]: 'j',
          },
        }),
      )

    const result = await useApi<{ rows: unknown[] }>('/api/Grid')

    const headers = fetchMock.mock.calls[1]![1]?.headers as Record<string, string>
    expect(headers.Accept).toContain(columnarJsonType)
    expect(result.rows).toEqual([{id: 1, name: 'a'}, {id: 2}, null])
  })
})
