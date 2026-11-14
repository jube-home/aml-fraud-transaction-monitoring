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
export type VariableKind = 'record' | 'scan'

export interface RuleVariable {
  name: string
  type: string
  kind: VariableKind
  line: number
}

export const variableTypes = ['Double', 'Integer', 'String', 'Boolean', 'DateTime']

const declaration =
  /^[ \t]*(Dim|Static)[ \t]+([A-Za-z][A-Za-z0-9]*)[ \t]+As[ \t]+(Double|Integer|String|Boolean|DateTime)(?![A-Za-z0-9_])/i

export function declaredVariables(text: string): RuleVariable[] {
  const variables: RuleVariable[] = []
  const seen = new Set<string>()

  text.split('\n').forEach((lineText, line) => {
    const found = declaration.exec(lineText)

    if (!found) return

    const name = found[2]!

    if (seen.has(name.toLowerCase())) return

    seen.add(name.toLowerCase())
    variables.push({
      name,
      type: variableTypes.find((type) => type.toLowerCase() === found[3]!.toLowerCase())!,
      kind: found[1]!.toLowerCase() === 'static' ? 'scan' : 'record',
      line,
    })
  })

  return variables
}

export function findVariable(variables: RuleVariable[], name: string): RuleVariable | undefined {
  const wanted = name.toLowerCase()

  return variables.find((variable) => variable.name.toLowerCase() === wanted)
}
