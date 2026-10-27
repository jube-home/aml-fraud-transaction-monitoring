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

import { argumentKind, fieldsOfTypes, operatorOf, referableTypes } from './ruleText'
import type { OperatorArgument, RuleContext } from './types'

export interface InputText {
  placeholder: (argument: OperatorArgument, isDate: boolean) => string
  title: (placeholder: string, referable: boolean) => string
}

export function escapeHtml(text: string): string {
  return String(text)
    .replace(/&/g, '&amp;')
    .replace(/"/g, '&quot;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

export function isCustomised(
  context: RuleContext,
  rule: { operator?: { type: string }; filter?: { input?: string } },
): boolean {
  const operator = rule.operator && operatorOf(context, rule.operator.type)

  return (
    !!operator && operator.arguments.length > 0 && !!rule.filter && rule.filter.input !== 'radio'
  )
}

export function inputHtml(
  context: RuleContext,
  text: InputText,
  ruleId: string,
  fieldId: string,
  operatorType: string,
  index: number,
): string {
  const dataType = context.fieldTypes[fieldId]
  const argument = operatorOf(context, operatorType)!.arguments[index]!
  const kind = argumentKind(argument, dataType)
  const name = `${ruleId}_value_${index}`
  const referable = fieldsOfTypes(context, referableTypes(kind, dataType))

  if (kind === 'list' || kind === 'field') {
    return (
      `<select class="select select-sm" name="${name}" aria-label="${escapeHtml(argument.name)}">` +
      `<option value="">${escapeHtml(argument.name)}</option>` +
      referable
        .map((id) => `<option value="${escapeHtml(id)}">${escapeHtml(id)}</option>`)
        .join('') +
      '</select>'
    )
  }

  const placeholder = text.placeholder(argument, kind === 'date')
  const listId = referable.length > 0 ? `${name}_fields` : null

  return (
    `<input class="input input-sm" type="text" name="${name}" placeholder="${escapeHtml(placeholder)}"` +
    ` aria-label="${escapeHtml(argument.name)}" title="${escapeHtml(text.title(placeholder, referable.length > 0))}"` +
    (listId ? ` list="${listId}"` : '') +
    '>' +
    (listId
      ? `<datalist id="${listId}">` +
        referable.map((id) => `<option value="${escapeHtml(id)}">`).join('') +
        '</datalist>'
      : '')
  )
}
