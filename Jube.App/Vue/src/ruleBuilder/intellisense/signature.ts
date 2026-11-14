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
import type {RuleWordOverload, RuleWordParameter} from '../types'

export function parameterText(parameter: RuleWordParameter): string {
  const base = `${parameter.name} As ${parameter.type}`

  return parameter.optional ? `${base} = ${parameter.defaultValue ?? 'Nothing'}` : base
}

export function overloadNotes(overload: RuleWordOverload, active: number | null): string[] {
  const parameter = active === null ? undefined : overload.parameters[active]
  const notes = overload.description ? [overload.description] : []

  if (parameter?.description) notes.push(`${parameter.name}: ${parameter.description}`)

  return notes
}

export function overloadDetail(overload: RuleWordOverload): string {
  return `(${overload.parameters.map(parameterText).join(', ')}) As ${overload.returnType}`
}

export function overloadSnippet(name: string, overload: RuleWordOverload): string {
  if (!overload.signature.includes('(')) return name

  const required = overload.parameters.filter((parameter) => !parameter.optional)

  if (required.length === 0) return `${name}()`

  return `${name}(${required.map((parameter) => '${' + parameter.name + '}').join(', ')})`
}

export interface KeywordSnippet {
  label: string
  detail: string
  template: string
}

export const blockSnippets: KeywordSnippet[] = [
  {label: 'If', detail: 'If … Then … End If', template: 'If (${condition}) Then\n  ${}\nEnd If'},
  {
    label: 'If',
    detail: 'If … Then … Else … End If',
    template: 'If (${condition}) Then\n  ${}\nElse\n  ${}\nEnd If',
  },
  {label: 'ElseIf', detail: 'ElseIf … Then', template: 'ElseIf (${condition}) Then\n  ${}'},
  {label: 'Return', detail: 'Return True', template: 'Return True'},
  {label: 'Return', detail: 'Return False', template: 'Return False'},
  {label: 'Matched', detail: 'Matched = …', template: 'Matched = ${expression}'},
]

export const declarationSnippets: KeywordSnippet[] = [
  {label: 'Dim', detail: 'Dim name As Type', template: 'Dim ${name} As ${Double}'},
  {label: 'Static', detail: 'Static name As Type', template: 'Static ${name} As ${Integer}'},
]

export type AbstractionPatternId =
  'nth' | 'firstMatching' | 'runningTotal' | 'skipCurrent' | 'ownResult'

export interface AbstractionPattern {
  id: AbstractionPatternId
  detail: string
  template: string
}

export const abstractionPatterns: AbstractionPattern[] = [
  {
    id: 'nth',
    detail: 'Cursor.Position = n',
    template: 'Matched = Cursor.Position = ${1:3}\nIf Cursor.Position >= ${1:3} Then Exit For',
  },
  {
    id: 'firstMatching',
    detail: 'Cursor.Matches >= n',
    template: 'If Cursor.Matches >= ${2:5} Then Exit For\nMatched = ${1:condition}',
  },
  {
    id: 'runningTotal',
    detail: 'Static total As Double',
    template:
      'Static total As Double\ntotal += ${1:Payload.Amount}\nMatched = True\nIf total >= ${2:10000} Then Exit For',
  },
  {
    id: 'skipCurrent',
    detail: 'Cursor.IsCurrent',
    template: 'If Cursor.IsCurrent Then Continue For',
  },
  {
    id: 'ownResult',
    detail: 'Cursor.Result',
    template: 'Cursor.Result = Cursor.Result + ${1:Payload.Amount}',
  },
]
