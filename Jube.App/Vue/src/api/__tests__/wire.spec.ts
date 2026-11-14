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

import {decode, encode} from '@msgpack/msgpack'
import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {ApiError, resetAntiforgeryToken, setApiHandlers, useApi} from '../useApi'
import {
  decodeWire,
  encodeWire,
  messagePackType,
  resetWire,
  setWireSchema,
  wireSchemaHeader,
  wireShapeHeader,
} from '../wire'
import type {WireSchema} from '../wire'

const schema: WireSchema = {
  hash: 'abc',
  types: {
    'Jube.Row': {
      fields: [
        {index: 0, name: 'id', type: 'int'},
        {index: 1, name: 'name', type: 'string'},
        {index: 2, name: 'when', type: 'date'},
        {index: 3, name: 'child', type: 'Jube.Child'},
        {index: 4, name: 'children', type: 'Jube.Child[]'},
        {index: 5, name: 'extras', type: 'any{}'},
        {index: 6, name: 'score', type: 'double?'},
        {index: 7, name: 'active', type: 'bool'},
        {index: 8, name: 'interval', type: 'char'},
      ],
    },
    'Jube.Child': {fields: [{index: 0, name: 'label', type: 'string'}]},
  },
  routes: [
    {method: 'POST', template: 'api/Row', shape: 'Jube.Row'},
    {method: 'PUT', template: 'api/Row/{id:int}', shape: 'Jube.Row'},
    {method: 'POST', template: 'api/Row/Special', shape: 'Jube.Child'},
  ],
}

function packed(shape: string, value: unknown, status = 200): Response {
  return new Response(encode(value) as BodyInit, {
    status,
    headers: {
      'Content-Type': messagePackType,
      [wireShapeHeader]: shape,
      [wireSchemaHeader]: schema.hash,
    },
  })
}

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {status})
}

describe('wire', () => {
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

  it('asks for MessagePack first and decodes a positional array into the JSON-shaped object', async () => {
    const row = [
      7,
      'seven',
      new Date('2026-11-05T10:20:30.000Z'),
      ['c'],
      [['d1'], ['d2']],
      {k: 1},
      null,
      true,
      'h',
    ]

    fetchMock
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(packed('Jube.Row', row))

    const result = await useApi<Record<string, unknown>>('/api/Row/7')

    const headers = fetchMock.mock.calls[1]![1]?.headers as Record<string, string>
    expect(headers.Accept).toContain(messagePackType)
    expect(result).toEqual({
      id: 7,
      name: 'seven',
      when: '2026-11-05T10:20:30.000Z',
      child: {label: 'c'},
      children: [{label: 'd1'}, {label: 'd2'}],
      extras: {k: 1},
      score: null,
      active: true,
      interval: 'h',
    })
  })

  it('decodes lists of rows and fills members missing from a shorter array with null', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(packed('Jube.Child[]', [['a'], ['b'], []]))

    expect(await useApi('/api/Rows')).toEqual([{label: 'a'}, {label: 'b'}, {label: null}])
  })

  it('loads the schema once for a hash and reuses it', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(packed('Jube.Child', ['a']))
      .mockResolvedValueOnce(packed('Jube.Child', ['b']))

    await useApi('/api/One')
    await useApi('/api/Two')

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/WireSchema/Head')
  })

  it('encodes a request body positionally using the route the schema names', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(json(200, {ok: true}))

    await useApi('/api/Row', {
      method: 'POST',
      body: {
        id: 3,
        name: 'n',
        when: '2026-01-02T03:04:05.000Z',
        child: {label: 'x'},
        children: [{label: 'y'}],
        extras: {a: 1},
        score: 2.5,
        active: true,
        interval: 'd',
      },
    })

    const [, init] = fetchMock.mock.calls[2]!
    const headers = init?.headers as Record<string, string>
    expect(headers['Content-Type']).toBe(messagePackType)
    expect(headers['X-XSRF-TOKEN']).toBe('tok')

    const sent = decode(init?.body as ArrayBuffer) as unknown[]
    expect(sent).toEqual([
      3,
      'n',
      new Date('2026-01-02T03:04:05.000Z'),
      ['x'],
      [['y']],
      {a: 1},
      2.5,
      true,
      'd',
    ])
  })

  it('matches templates with route parameters and prefers the more specific route', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, schema))
      .mockImplementation(() => Promise.resolve(json(200, {})))

    await useApi('/api/Row/Special', {method: 'POST', body: {label: 'only'}})
    await useApi('/api/Row/12?x=1', {method: 'PUT', body: {id: 12}})

    expect(decode(fetchMock.mock.calls[2]![1]?.body as ArrayBuffer)).toEqual(['only'])

    const second = decode(fetchMock.mock.calls[3]![1]?.body as ArrayBuffer) as unknown[]
    expect(second[0]).toBe(12)
  })

  it('gives omitted value types their C# default and keeps nullable ones null', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(json(200, {}))

    await useApi('/api/Row', {method: 'POST', body: {name: 'only'}})

    const sent = decode(fetchMock.mock.calls[2]![1]?.body as ArrayBuffer) as unknown[]
    expect(sent[0]).toBe(0)
    expect(sent[1]).toBe('only')
    expect(sent[3]).toBeNull()
    expect(sent[6]).toBeNull()
    expect(sent[7]).toBe(false)
    expect(sent[8]).toBe('\u0000')
    expect((sent[2] as Date).getUTCFullYear()).toBe(1)
  })

  it('sends JSON for a body whose route is not in the schema', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(json(200, {}))

    await useApi('/api/NotListed', {method: 'POST', body: {a: 1}})

    const [, init] = fetchMock.mock.calls[2]!
    expect((init?.headers as Record<string, string>)['Content-Type']).toBe('application/json')
    expect(init?.body).toBe('{"a":1}')
  })

  it.each(['Json', 'Mixed'])(
    'sends JSON request bodies when the server mode is %s',
    async (mode) => {
      fetchMock
        .mockResolvedValueOnce(json(200, {token: 'tok'}))
        .mockResolvedValueOnce(json(200, {...schema, mode}))
        .mockResolvedValueOnce(json(200, {}))

      await useApi('/api/Row', {method: 'POST', body: {id: 1, name: 'x'}})

      const [, init] = fetchMock.mock.calls[2]!
      expect((init?.headers as Record<string, string>)['Content-Type']).toBe('application/json')
      expect(init?.body).toBe('{"id":1,"name":"x"}')
    },
  )

  it('sends MessagePack request bodies when the server mode is MessagePack', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, {...schema, mode: 'MessagePack'}))
      .mockResolvedValueOnce(json(200, {}))

    await useApi('/api/Row', {method: 'POST', body: {id: 1, name: 'x'}})

    const [, init] = fetchMock.mock.calls[2]!
    expect((init?.headers as Record<string, string>)['Content-Type']).toBe(messagePackType)
  })

  it('keeps anonymous calls on JSON in both directions without touching the schema', async () => {
    fetchMock.mockResolvedValueOnce(json(200, {ok: true}))

    await useApi('/api/Authentication/Methods', {method: 'POST', body: {a: 1}, anonymous: true})

    expect(fetchMock).toHaveBeenCalledTimes(1)

    const [, init] = fetchMock.mock.calls[0]!
    const headers = init?.headers as Record<string, string>
    expect(headers.Accept).toBe('application/json')
    expect(headers['Content-Type']).toBe('application/json')
  })

  it('still maps JSON validation errors to field errors after a MessagePack request', async () => {
    fetchMock
      .mockResolvedValueOnce(json(200, {token: 'tok'}))
      .mockResolvedValueOnce(json(200, schema))
      .mockResolvedValueOnce(
        json(400, {errors: [{propertyName: 'name', errorMessage: 'Required'}]}),
      )

    const error = await useApi('/api/Row', {method: 'POST', body: {id: 1}}).catch(
      (e: unknown) => e,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).fieldErrors).toEqual({name: ['Required']})
  })
})

describe('wire columnar rows', () => {
  beforeEach(() => {
    resetWire()
    setWireSchema({
      hash: 'c',
      types: {
        'Jube.Grid': {
          fields: [
            {index: 0, name: 'columns', type: 'string[]'},
            {index: 1, name: 'rows', type: 'any{}#'},
          ],
        },
      },
      routes: [],
    })
  })

  function wireBytes(value: unknown): ArrayBuffer {
    const packed = encode(value)

    return packed.buffer.slice(
      packed.byteOffset,
      packed.byteOffset + packed.byteLength,
    ) as ArrayBuffer
  }

  it('rebuilds row objects from columns, honouring sparse cells and null rows', () => {
    const columnar = [
      4,
      ['id', 'name', 'extra'],
      [
        [1, 2, null, 4],
        ['a', 'b', null, 'd'],
        [null, null, null, true],
      ],
      [[1, [1]]],
      [2],
    ]

    expect(decodeWire(wireBytes(columnar), 'any{}#')).toEqual([
      {id: 1, name: 'a', extra: null},
      {id: 2, extra: null},
      null,
      {id: 4, name: 'd', extra: true},
    ])
  })

  it('keeps the row count of empty rows and of no rows', () => {
    expect(decodeWire(wireBytes([3, [], [], null, null]), 'any{}#')).toEqual([{}, {}, {}])
    expect(decodeWire(wireBytes([0, [], [], null, null]), 'any{}#')).toEqual([])
  })

  it('does not let a column named __proto__ reach the prototype', () => {
    const rows = decodeWire(
      wireBytes([1, ['__proto__', 'ok'], [[{polluted: true}], [1]], null, null]),
      'any{}#',
    ) as Record<string, unknown>[]

    expect(Object.getPrototypeOf(rows[0])).toBe(Object.prototype)
    expect(({} as Record<string, unknown>).polluted).toBeUndefined()
    expect(Object.keys(rows[0]!)).toEqual(['__proto__', 'ok'])
  })

  it('round-trips rows through encode and decode, including ragged and null rows', () => {
    const rows = [{id: 1, name: 'a'}, {id: 2}, null, {name: 'z', id: 4}]

    const bytes = encodeWire('any{}#', rows)

    expect(decodeWire(bytes, 'any{}#')).toEqual(rows)
  })

  it('writes the sparse table as column and row pairs the server can read', () => {
    const bytes = encodeWire('any{}#', [{a: 1}, {}])
    const parts = decode(bytes) as unknown[]

    expect(parts[0]).toBe(2)
    expect(parts[1]).toEqual(['a'])
    expect(parts[3]).toEqual([[0, [1]]])
  })

  it('decodes a DTO member of columnar rows nested in a positional type', () => {
    const body = [
      ['x', 'y'],
      [
        2,
        ['x', 'y'],
        [
          [1, 2],
          ['a', 'b'],
        ],
        null,
        null,
      ],
    ]

    expect(decodeWire(wireBytes(body), 'Jube.Grid')).toEqual({
      columns: ['x', 'y'],
      rows: [
        {x: 1, y: 'a'},
        {x: 2, y: 'b'},
      ],
    })
  })

  it('is much smaller than the same rows written as maps or as JSON', () => {
    const rows = Array.from({length: 1000}, (_, i) => ({
      transactionIdentifier: i,
      counterpartyName: 'ACME',
      amountInSettlementCurrency: i * 1.5,
      flagged: i % 2 === 0,
    }))

    const columnar = encodeWire('any{}#', rows).byteLength
    const mapped = encode(rows).byteLength
    const json = new TextEncoder().encode(JSON.stringify(rows)).byteLength

    console.info(`rows=1000 json=${json} msgpack-maps=${mapped} msgpack-columnar=${columnar}`)

    expect(columnar).toBeLessThan(mapped * 0.6)
    expect(columnar).toBeLessThan(json * 0.4)
  })

  it('writes and reads exactly the bytes the server formatter writes and reads', () => {
    const golden =
      '950493a26964a46e616d65a5657874726193940102c00494a161c0c0a17a94c0c0c0c3929201910192029200019102'
    const rows = [{id: 1, name: 'a'}, {id: 2}, null, {name: 'z', id: 4, extra: true}]

    expect(Buffer.from(new Uint8Array(encodeWire('any{}#', rows))).toString('hex')).toBe(golden)

    const bytes = Uint8Array.from(Buffer.from(golden, 'hex')).buffer

    expect(decodeWire(bytes, 'any{}#')).toEqual(
      [
        {id: 1, name: 'a', extra: undefined},
        {id: 2},
        null,
        {id: 4, name: 'z', extra: true},
      ].map((row) => (row === null ? null : JSON.parse(JSON.stringify(row)))),
    )
  })
})
