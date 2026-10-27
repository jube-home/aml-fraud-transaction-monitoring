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
import type { RuleWordOverload, RuleWordParameter } from '../types'

export function parameterText(parameter: RuleWordParameter): string {
  const base = `${parameter.name} As ${parameter.type}`

  return parameter.optional ? `${base} = ${parameter.defaultValue ?? 'Nothing'}` : base
}

export function overloadDetail(overload: RuleWordOverload): string {
  return `(${overload.parameters.map(parameterText).join(', ')}) As ${overload.returnType}`
}

export function overloadSnippet(name: string, overload: RuleWordOverload): string {
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
  { label: 'If', detail: 'If … Then … End If', template: 'If (${condition}) Then\n  ${}\nEnd If' },
  {
    label: 'If',
    detail: 'If … Then … Else … End If',
    template: 'If (${condition}) Then\n  ${}\nElse\n  ${}\nEnd If',
  },
  { label: 'ElseIf', detail: 'ElseIf … Then', template: 'ElseIf (${condition}) Then\n  ${}' },
  { label: 'Return', detail: 'Return True', template: 'Return True' },
  { label: 'Return', detail: 'Return False', template: 'Return False' },
  { label: 'Matched', detail: 'Matched = …', template: 'Matched = ${expression}' },
]
