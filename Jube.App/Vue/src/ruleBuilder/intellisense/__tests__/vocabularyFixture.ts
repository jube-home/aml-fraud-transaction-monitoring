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
import type {Completion, RuleWord} from '../../types'

function keyword(name: string): RuleWord {
  return {name, kind: 'Keyword', overloads: []}
}

function cursorProperty(name: string, returnType: string, description: string): RuleWord {
  return {
    name,
    kind: 'Keyword',
    description,
    overloads: [
      {
        signature: `Cursor.${name} As ${returnType}`,
        receiverType: 'AbstractionCursor',
        returnType,
        parameters: [],
      },
    ],
  }
}

export const baseWords: RuleWord[] = [
  ...[
    'If',
    'Then',
    'Else',
    'ElseIf',
    'End If',
    'Select',
    'Case',
    'End Select',
    'Return',
    'Matched',
    'True',
    'False',
    'And',
    'Or',
    'Not',
    'AndAlso',
    'Payload',
    'Abstraction',
    'Graph',
    'List',
    'Double',
    'Integer',
    'String',
    'DateTime',
  ].map(keyword),
  {
    name: 'Contains',
    kind: 'Function',
    overloads: [
      {
        signature: '<String>.Contains(value As String) As Boolean',
        receiverType: 'String',
        returnType: 'Boolean',
        parameters: [{name: 'value', type: 'String', optional: false, defaultValue: null}],
      },
    ],
  },
  {
    name: 'Round',
    kind: 'Function',
    overloads: [
      {
        signature: '<Double>.Round(places As Integer) As Double',
        receiverType: 'Double',
        returnType: 'Double',
        parameters: [{name: 'places', type: 'Integer', optional: false, defaultValue: null}],
      },
    ],
  },
]

export const cursorWords: RuleWord[] = [
  {
    name: 'Cursor',
    kind: 'Keyword',
    description: 'Where the rule is in its walk.',
    overloads: [
      {
        signature: 'Cursor As AbstractionCursor',
        receiverType: '',
        returnType: 'AbstractionCursor',
        parameters: [],
      },
    ],
  },
  cursorProperty('Position', 'Integer', 'One-based position.'),
  cursorProperty('Matches', 'Integer', 'How many accepted so far.'),
  cursorProperty('IsCurrent', 'Boolean', 'True for the transaction being processed.'),
  cursorProperty('Result', 'Double', 'A number the rule can set.'),
  {name: 'Exit For', kind: 'Keyword', overloads: [], description: 'Ends the walk.'},
  {name: 'Continue For', kind: 'Keyword', overloads: [], description: 'Moves to the next.'},
  ...['Dim', 'Static', 'As', 'Boolean'].map(keyword),
]

function field(name: string, dataType: string): Completion {
  return {
    name,
    value: name,
    meta: '',
    score: 1,
    dataType,
    group: name.split('.')[0]!,
  } as Completion
}

export const fields: Completion[] = [
  field('Payload.Amount', 'double'),
  field('Payload.AccountId', 'string'),
  field('Payload.Country', 'string'),
  field('Abstraction.Total', 'double'),
  field('Graph.Customer.Degree', 'double'),
]
