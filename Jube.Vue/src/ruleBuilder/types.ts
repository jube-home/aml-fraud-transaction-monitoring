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

export type FieldDataType = 'string' | 'integer' | 'double' | 'datetime' | 'boolean' | 'list'

export type ArgumentKind =
  'value' | 'number' | 'integer' | 'text' | 'date' | 'list' | 'field' | 'boolean'

export interface Completion {
  name: string
  value: string
  meta: string
  score: number
  dataType: FieldDataType
  group: string
  field?: string | null
  xPath?: string | null
}

export interface OperatorArgument {
  name: string
  kind: ArgumentKind
  many: boolean
}

export interface BuilderOperator {
  type: string
  label: string
  group: string
  arguments: OperatorArgument[]
  applyTo: string[]
  template: string
}

export interface ErrorSpan {
  start: number
  length: number
  message: string
  line: number
  column?: number | null
  endColumn?: number | null
}

export interface RuleWordParameter {
  name: string
  type: string
  optional: boolean
  defaultValue?: string | null
}

export interface RuleWordOverload {
  signature: string
  receiverType: string
  returnType: string
  parameters: RuleWordParameter[]
}

export type RuleWordKind = 'Keyword' | 'AllowedWord' | 'Function' | 'CuratedExpression'

export interface RuleWord {
  name: string
  kind: RuleWordKind
  overloads: RuleWordOverload[]
}

export interface ParseResult {
  message?: string | null
  errorSpans?: ErrorSpan[] | null
}

export interface BuilderRule {
  id: string
  operator: string
  value?: unknown
}

export interface BuilderGroup {
  condition: 'AND' | 'OR'
  not?: boolean
  rules: (BuilderRule | BuilderGroup)[]
}

export type RuleType = 1 | 2

export interface BuilderCoderValue {
  ruleTextBuilder?: string
  ruleJsonBuilder?: string | BuilderGroup
  ruleTextCoder: string
  ruleType?: RuleType
}

export interface RuleContext {
  operators: BuilderOperator[]
  fieldTypes: Record<string, string>
}

export interface BuilderFilter {
  optgroup: string
  id: string
  name: string
  operators: string[]
  value_separator: string
  type: 'string' | 'integer' | 'double' | 'datetime'
  input?: 'radio' | 'select'
  default_value?: string
  values?: Record<string, string>
}

export type CompileState =
  'returnFalse' | 'incomplete' | 'compiled' | 'valid' | 'errors' | 'disconnected' | 'checking'
