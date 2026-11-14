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

import {hasPermission, type Me} from '../auth/me'
import type {FormModel, RecordValues} from './types'

export const backtestPermission = 61
export const integrityPermission = 60
export const dependencyPermissions = [8, 10, 13, 14, 16, 17, 25, 26] as const

const ruleTypes: Record<number, string> = {1: 'ActivationRule'}

const backtestRuleTypes = new Set(['ActivationRule', 'GatewayRule', 'ReprocessingRule'])

export interface RuleToolsHost {
  ruleType: string
  ruleId: number
  ruleText: string
  canBacktest: boolean
  canCheckIntegrity: boolean
  canDependencies: boolean
}

function text(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

export function ruleTextOf(model: FormModel, values: RecordValues): string {
  const editor = model.editors.find((candidate) => candidate.kind === 'ruleBuilder')

  if (!editor) return ''

  const {roles} = editor
  const builder = Number(values[roles.scriptType ?? '']) === 1

  return builder && roles.builder ? text(values[roles.builder]) : text(values[roles.coder ?? ''])
}

export function ruleToolsHost(
  model: FormModel | undefined,
  values: RecordValues,
  id: number | undefined,
  me: Me | undefined,
): RuleToolsHost | undefined {
  const editor = model?.editors.find((candidate) => candidate.kind === 'ruleTools')
  const ruleType = editor?.parseType == null ? undefined : ruleTypes[editor.parseType]

  if (!model || !ruleType || id === undefined) return undefined

  return {
    ruleType,
    ruleId: id,
    ruleText: ruleTextOf(model, values),
    canBacktest: backtestRuleTypes.has(ruleType) && hasPermission(me, backtestPermission),
    canCheckIntegrity: hasPermission(me, integrityPermission),
    canDependencies: hasPermission(me, ...dependencyPermissions),
  }
}

export function modelIdOf(parentId: number | string | undefined): number | undefined {
  const number = Number(parentId)

  return parentId !== undefined && Number.isInteger(number) && number > 0 ? number : undefined
}
