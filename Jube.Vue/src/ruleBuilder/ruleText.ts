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

import type {
  ArgumentKind,
  BuilderGroup,
  BuilderOperator,
  BuilderRule,
  OperatorArgument,
  RuleContext,
} from './types'

export const emptyRuleText = 'Return False'

export function operatorOf(context: RuleContext, type: string): BuilderOperator | undefined {
  return context.operators.find((operator) => operator.type === type)
}

export function argumentKind(
  argument: OperatorArgument,
  dataType: string | undefined,
): ArgumentKind {
  if (argument.kind !== 'value') return argument.kind

  switch (dataType) {
    case 'integer':
      return 'integer'
    case 'double':
      return 'number'
    case 'datetime':
      return 'date'
    case 'string':
      return 'text'
    case 'boolean':
      return 'boolean'
    default:
      return 'field'
  }
}

export function referableTypes(kind: ArgumentKind, dataType: string | undefined): string[] {
  switch (kind) {
    case 'list':
      return ['list']
    case 'field':
      return dataType === 'list' ? ['string'] : dataType ? [dataType] : []
    case 'number':
      return ['double', 'integer']
    case 'integer':
      return ['integer']
    case 'date':
      return ['datetime']
    default:
      return []
  }
}

export function fieldsOfTypes(context: RuleContext, types: string[]): string[] {
  return Object.keys(context.fieldTypes)
    .filter((id) => types.includes(context.fieldTypes[id]!))
    .sort()
}

export function operatorsFor(operators: BuilderOperator[], dataType: string): string[] {
  return operators.filter((operator) => operator.applyTo.includes(dataType)).map((o) => o.type)
}

export function argumentItems(argument: OperatorArgument, value: unknown): string[] {
  if (!argument.many) return [value as string]

  const raw = Array.isArray(value) ? value : String(value ?? '').split(',')

  return raw.map((item) => String(item).trim()).filter((item) => item.length > 0)
}

export function isoDate(text: string): string | null {
  const trimmed = String(text).trim()
  const zoned =
    /([zZ]|[+-]\d\d:?\d\d)$/.test(trimmed) || !/\d[T ]\d/.test(trimmed) ? trimmed : trimmed + 'Z'
  const date = new Date(/[T ]/.test(zoned) || /[zZ]$/.test(zoned) ? zoned : zoned + 'T00:00:00Z')

  if (Number.isNaN(date.getTime())) return null

  return date
    .toISOString()
    .replace(/\.000Z$/, 'Z')
    .replace(/(\.\d*?)0+Z$/, '$1Z')
}

function hasControlCharacter(text: string): boolean {
  return [...text].some((character) => {
    const code = character.charCodeAt(0)

    return code <= 0x1f || code === 0x7f
  })
}

export type ItemErrorCode =
  'needed' | 'integer' | 'number' | 'date' | 'list' | 'field' | 'controlCharacters'

export function itemError(
  context: RuleContext,
  kind: ArgumentKind,
  dataType: string | undefined,
  item: unknown,
): ItemErrorCode | null {
  const text = String(item ?? '').trim()

  if (text.length === 0) return 'needed'

  if (fieldsOfTypes(context, referableTypes(kind, dataType)).includes(text)) return null

  switch (kind) {
    case 'integer':
      return /^[+-]?\d+$/.test(text) ? null : 'integer'
    case 'number':
      return Number.isFinite(Number(text)) ? null : 'number'
    case 'date':
      return isoDate(text) ? null : 'date'
    case 'list':
      return 'list'
    case 'field':
      return 'field'
    case 'text':
      return hasControlCharacter(text) ? 'controlCharacters' : null
    default:
      return null
  }
}

export interface ArgumentFailure {
  argument: string
  code: ItemErrorCode
}

export function validateRuleValue(
  context: RuleContext,
  operator: BuilderOperator,
  fieldId: string,
  value: unknown,
): ArgumentFailure | null {
  const dataType = context.fieldTypes[fieldId]
  const inputs = operator.arguments.length === 1 ? [value] : ((value as unknown[]) ?? [])

  for (const [index, argument] of operator.arguments.entries()) {
    const kind = argumentKind(argument, dataType)
    const items = argumentItems(argument, inputs[index])

    if (items.length === 0) return { argument: argument.name, code: 'needed' }

    for (const item of items) {
      const code = itemError(context, kind, dataType, item)

      if (code) return { argument: argument.name, code }
    }
  }

  return null
}

export function quote(text: string): string {
  return '"' + String(text).split('"').join('""') + '"'
}

export function literal(
  context: RuleContext,
  kind: ArgumentKind,
  dataType: string | undefined,
  item: unknown,
): string {
  const text = String(item).trim()

  if (fieldsOfTypes(context, referableTypes(kind, dataType)).includes(text)) return text

  switch (kind) {
    case 'integer':
      return String(Number.parseInt(text, 10))
    case 'number':
      return String(Number(text))
    case 'date':
      return quote(isoDate(text) ?? text) + '.ToIsoDateTime()'
    case 'boolean':
      return text === 'True' ? 'True' : 'False'
    case 'list':
    case 'field':
      return text
    default:
      return quote(String(item))
  }
}

function isGroup(node: BuilderRule | BuilderGroup): node is BuilderGroup {
  return 'rules' in node && Array.isArray(node.rules)
}

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || value === ''
}

function renderedArguments(
  context: RuleContext,
  operator: BuilderOperator,
  dataType: string | undefined,
  value: unknown,
  lenient: boolean,
): string {
  const inputs = operator.arguments.length === 1 ? [value] : ((value as unknown[]) ?? [])

  return operator.arguments
    .map((argument, index) => {
      const kind = argumentKind(argument, dataType)
      const given = inputs[index]
      const items = lenient && isBlank(given) ? [] : argumentItems(argument, given)

      return items.map((item) => literal(context, kind, dataType, item)).join(', ')
    })
    .join(', ')
}

export function ruleExpression(
  context: RuleContext,
  rule: BuilderRule,
  lenient = false,
): string | null {
  const operator = operatorOf(context, rule.operator)

  if (!operator) {
    if (lenient) return null

    throw new Error(`Unknown operator ${rule.operator}`)
  }

  if (lenient && !rule.id) return null

  const rendered = renderedArguments(
    context,
    operator,
    context.fieldTypes[rule.id],
    rule.value,
    lenient,
  )

  return rule.id + operator.template.split('?').join(rendered)
}

export function groupExpression(
  context: RuleContext,
  group: BuilderGroup,
  lenient = false,
): string {
  const parts = group.rules
    .map((node) => {
      if (!isGroup(node)) return ruleExpression(context, node, lenient)

      const inner = groupExpression(context, node, lenient)

      return inner === '' ? null : '( ' + inner + ' ) '
    })
    .filter((part): part is string => part !== null)

  const expression = parts.join(' ' + group.condition + ' ')

  return group.not && expression !== '' ? 'NOT ( ' + expression + ' )' : expression
}

export function ruleText(context: RuleContext, rules: BuilderGroup, lenient = false): string {
  const expression = groupExpression(context, rules, lenient)

  if (lenient && expression === '') return ''

  return 'If (' + expression + ') Then\n  Return True\nEnd If'
}

export function dropUnknownFields(
  group: BuilderGroup,
  known: (id: string) => boolean,
): { group: BuilderGroup; dropped: string[] } {
  const dropped: string[] = []

  function strip(node: BuilderGroup): BuilderGroup {
    return {
      ...node,
      rules: node.rules
        .filter((child) => {
          if (isGroup(child)) return true
          if (known(child.id)) return true

          dropped.push(child.id)

          return false
        })
        .map((child) => (isGroup(child) ? strip(child) : child)),
    }
  }

  return { group: strip(group), dropped }
}
