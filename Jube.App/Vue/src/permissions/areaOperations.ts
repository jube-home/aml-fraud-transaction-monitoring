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
import type {FormOperation} from '../formModel/types'

export const reprocessingPermission = {
  editRule: 26,
  triggerRun: 73,
  stopRun: 74,
} as const

const reprocessingRead = [
  reprocessingPermission.editRule,
  reprocessingPermission.triggerRun,
  reprocessingPermission.stopRun,
]

type OperationPermissions = Partial<Record<FormOperation, readonly number[]>>

export const areaOperationPermissions: Record<string, OperationPermissions> = {
  EntityAnalysisModelReprocessingRule: {
    list: reprocessingRead,
    read: reprocessingRead,
    create: [reprocessingPermission.editRule],
    update: [reprocessingPermission.editRule],
    delete: [reprocessingPermission.editRule],
  },
  EntityAnalysisModelReprocessingRuleInstance: {
    list: reprocessingRead,
    read: reprocessingRead,
    create: [reprocessingPermission.triggerRun],
    update: [reprocessingPermission.triggerRun],
    delete: [reprocessingPermission.stopRun],
  },
}

export function operationPermitted(
  area: string,
  operation: FormOperation,
  me: Me | undefined,
): boolean {
  const required = areaOperationPermissions[area]?.[operation]

  if (required === undefined) return true

  return hasPermission(me, ...required)
}

export function deniedOperations(
  area: string,
  operations: readonly FormOperation[],
  me: Me | undefined,
): FormOperation[] {
  return operations.filter((operation) => !operationPermitted(area, operation, me))
}
