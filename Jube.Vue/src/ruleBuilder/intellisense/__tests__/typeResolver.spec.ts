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
import { describe, expect, it } from 'vitest'
import {
  argumentCount,
  buildVocabulary,
  fieldTypes,
  bindReceiver,
  isCompatible,
  substituteReturn,
  matchingOverloads,
  overloadsFor,
  resolveType,
  vbTypeOf,
} from '../typeResolver'
import { overloadDetail, overloadSnippet } from '../signature'
import type { Completion, RuleWord } from '../../types'

function fn(
  name: string,
  receiverType: string,
  returnType: string,
  parameters: { name: string; type: string; optional?: boolean }[] = [],
) {
  return {
    signature: `<${receiverType}>.${name}()`,
    receiverType,
    returnType,
    parameters: parameters.map((p) => ({ optional: false, defaultValue: null, ...p })),
  }
}

const words: RuleWord[] = [
  { name: 'Trim', kind: 'Function', overloads: [fn('Trim', 'String', 'String')] },
  {
    name: 'Replace',
    kind: 'Function',
    overloads: [
      fn('Replace', 'String', 'String', [
        { name: 'find', type: 'String' },
        { name: 'with', type: 'String' },
      ]),
    ],
  },
  { name: 'Length', kind: 'Function', overloads: [fn('Length', 'String', 'Integer')] },
  {
    name: 'Between',
    kind: 'Function',
    overloads: [
      fn('Between', 'Double', 'Boolean', [
        { name: 'low', type: 'Double' },
        { name: 'high', type: 'Double' },
      ]),
      fn('Between', 'DateTime', 'Boolean', [
        { name: 'from', type: 'DateTime' },
        { name: 'to', type: 'DateTime' },
      ]),
    ],
  },
  {
    name: 'Round',
    kind: 'Function',
    overloads: [
      fn('Round', 'Double', 'Double', [{ name: 'places', type: 'Integer', optional: true }]),
    ],
  },
  { name: 'If', kind: 'Keyword', overloads: [] },
  {
    name: 'Start',
    kind: 'Function',
    overloads: [fn('Start', 'Double', 'Flow(Of Double)'), fn('Start', 'Flow(Of T)', 'Flow(Of T)')],
  },
  {
    name: 'MatchGreater',
    kind: 'Function',
    overloads: [
      fn('MatchGreater', 'Flow(Of Double)', 'Flow(Of Double)', [{ name: 'other', type: 'Double' }]),
    ],
  },
  {
    name: 'Then',
    kind: 'Function',
    overloads: [fn('Then', 'Flow(Of T)', 'Flow(Of TNext)', [{ name: 'next', type: 'Object' }])],
  },
  { name: 'Result', kind: 'Function', overloads: [fn('Result', 'Flow(Of T)', 'T')] },
]

const fields: Completion[] = [
  {
    name: 'Payload.Name',
    value: 'Payload.Name',
    meta: '',
    score: 1,
    dataType: 'string',
    group: 'Payload',
  },
  {
    name: 'Payload.Amount',
    value: 'Payload.Amount',
    meta: '',
    score: 1,
    dataType: 'double',
    group: 'Payload',
  },
  {
    name: 'Payload.Count',
    value: 'Payload.Count',
    meta: '',
    score: 1,
    dataType: 'integer',
    group: 'Payload',
  },
]

const vocabulary = buildVocabulary(words)
const types = fieldTypes(fields)

describe('buildVocabulary', () => {
  it('indexes functions case-insensitively and keeps keywords apart', () => {
    expect(vocabulary.functions.has('trim')).toBe(true)
    expect(vocabulary.keywords.map((word) => word.name)).toEqual(['If'])
  })
})

describe('compatibility', () => {
  it('widens numbers but not text to numbers', () => {
    expect(isCompatible('Integer', 'Double')).toBe(true)
    expect(isCompatible('Double', 'Integer')).toBe(false)
    expect(isCompatible('String', 'Double')).toBe(false)
    expect(isCompatible('DateTime', 'Object')).toBe(true)
  })

  it('maps field data types to Visual Basic types', () => {
    expect(vbTypeOf('datetime')).toBe('DateTime')
    expect(vbTypeOf('list')).toBeNull()
  })
})

describe('resolveType', () => {
  it('resolves a field, a call and a chain of calls', () => {
    expect(resolveType('Payload.Name', types, vocabulary)).toBe('String')
    expect(resolveType('Payload.Name.Trim()', types, vocabulary)).toBe('String')
    expect(resolveType('Payload.Name.Trim().Replace("a", "b").Length()', types, vocabulary)).toBe(
      'Integer',
    )
  })

  it('resolves a parameterless call written without parentheses', () => {
    expect(resolveType('Payload.Name.Length', types, vocabulary)).toBe('Integer')
  })

  it('lets an Integer field use Double methods through widening', () => {
    expect(resolveType('Payload.Count.Between(1, 2)', types, vocabulary)).toBe('Boolean')
  })

  it('is null for unknown fields, unknown methods and a wrong receiver', () => {
    expect(resolveType('Payload.Nope', types, vocabulary)).toBeNull()
    expect(resolveType('Payload.Name.Nope()', types, vocabulary)).toBeNull()
    expect(resolveType('Payload.Name.Between(1, 2)', types, vocabulary)).toBeNull()
    expect(resolveType('', types, vocabulary)).toBeNull()
  })
})

describe('overloads', () => {
  it('lists only the overloads that fit the receiver', () => {
    expect(overloadsFor(vocabulary, 'Between', 'Double')).toHaveLength(1)
    expect(overloadsFor(vocabulary, 'between', 'DateTime')[0]!.parameters[0]!.name).toBe('from')
    expect(overloadsFor(vocabulary, 'Between', 'String')).toHaveLength(0)
  })

  it('filters by argument count, counting optional parameters', () => {
    expect(matchingOverloads(vocabulary, 'Round', 'Double', 0)).toHaveLength(1)
    expect(matchingOverloads(vocabulary, 'Round', 'Double', 1)).toHaveLength(1)
    expect(matchingOverloads(vocabulary, 'Round', 'Double', 2)).toHaveLength(0)
  })

  it('counts arguments outside nested calls and literals', () => {
    expect(argumentCount('')).toBe(0)
    expect(argumentCount('1')).toBe(1)
    expect(argumentCount('Foo(1, 2), "a, b"')).toBe(2)
  })
})

describe('signature text', () => {
  it('writes the detail and a snippet with required parameters only', () => {
    const between = overloadsFor(vocabulary, 'Between', 'Double')[0]!
    const round = overloadsFor(vocabulary, 'Round', 'Double')[0]!

    expect(overloadDetail(between)).toBe('(low As Double, high As Double) As Boolean')
    expect(overloadSnippet('Between', between)).toBe('Between(${low}, ${high})')
    expect(overloadDetail(round)).toBe('(places As Integer = Nothing) As Double')
    expect(overloadSnippet('Round', round)).toBe('Round()')
  })
})

describe('generic receivers', () => {
  it('binds Flow(Of T) to the flow it is called on', () => {
    expect(bindReceiver('Flow(Of Double)', 'Flow(Of T)')).toEqual({ T: 'Double' })
    expect(bindReceiver('Flow(Of Double)', 'Flow(Of Double)')).toEqual({})
    expect(bindReceiver('Double', 'Flow(Of T)')).toBeNull()
    expect(bindReceiver('List(Of Double)', 'Flow(Of T)')).toBeNull()
  })

  it('substitutes bound and unbound generic return types', () => {
    expect(substituteReturn('T', { T: 'Double' })).toBe('Double')
    expect(substituteReturn('Flow(Of T)', { T: 'String' })).toBe('Flow(Of String)')
    expect(substituteReturn('Flow(Of TNext)', { T: 'String' })).toBe('Flow(Of Object)')
    expect(substituteReturn('TimeSpan', {})).toBe('TimeSpan')
  })

  it('follows a fluent chain through generic flows', () => {
    expect(resolveType('Payload.Amount.Start()', types, vocabulary)).toBe('Flow(Of Double)')
    expect(resolveType('Payload.Amount.Start().MatchGreater(1)', types, vocabulary)).toBe(
      'Flow(Of Double)',
    )
    expect(resolveType('Payload.Amount.Start().MatchGreater(1).Result()', types, vocabulary)).toBe(
      'Double',
    )
    expect(resolveType('Payload.Amount.Start().Then(1)', types, vocabulary)).toBe('Flow(Of Object)')
  })
})
