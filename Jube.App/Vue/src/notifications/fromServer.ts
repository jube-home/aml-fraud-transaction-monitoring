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

import type {AppNotification} from './notifications'
import type {ServerNotification} from './api'
import {caseLabel} from '../cases/labels'

type Translate = (key: string, named?: Record<string, unknown>) => string

const caseServiceLevelBreached = 1
const sanctionRescreenHits = 2
const caseDigest = 3
const modelDrift = 4
const entityRiskReviewDue = 5
const reportAwaitingApproval = 6
const reportDecided = 7
const reportFeedback = 8

function numberArgument(argumentsJson: string | null, name: string): number {
  if (!argumentsJson) return 0

  try {
    const parsed = JSON.parse(argumentsJson) as Record<string, unknown>
    const value = parsed[name]

    return typeof value === 'number' ? value : 0
  } catch {
    return 0
  }
}

function stringArgument(argumentsJson: string | null, name: string): string {
  if (!argumentsJson) return ''

  try {
    const parsed = JSON.parse(argumentsJson) as Record<string, unknown>
    const value = parsed[name]

    return typeof value === 'string' ? value : ''
  } catch {
    return ''
  }
}

function dimensionOf(argumentsJson: string | null): number {
  return numberArgument(argumentsJson, 'dimension')
}

export function fromServer(item: ServerNotification, t: Translate): AppNotification {
  const dimension = dimensionOf(item.argumentsJson)

  const known = item.kindId === caseServiceLevelBreached && item.caseId !== null

  if (
    item.kindId === reportAwaitingApproval ||
    item.kindId === reportDecided ||
    item.kindId === reportFeedback
  ) {
    return {
      id: item.guid,
      title: t(`notifications.kinds.${item.kindId}`, {
        id: numberArgument(item.argumentsJson, 'caseId'),
        detail: stringArgument(item.argumentsJson, 'detail'),
      }),
      severity: 'info',
      at: item.createdDate,
      read: item.read,
    }
  }

  if (item.kindId === entityRiskReviewDue) {
    return {
      id: item.guid,
      title: t('notifications.kinds.5', {count: numberArgument(item.argumentsJson, 'count')}),
      severity: 'info',
      at: item.createdDate,
      read: item.read,
    }
  }

  if (item.kindId === modelDrift) {
    return {
      id: item.guid,
      title: t('notifications.kinds.4', {
        model: stringArgument(item.argumentsJson, 'model'),
        score: stringArgument(item.argumentsJson, 'score'),
        psi: numberArgument(item.argumentsJson, 'psi'),
      }),
      severity: 'warning',
      at: item.createdDate,
      read: item.read,
    }
  }

  if (item.kindId === caseDigest) {
    return {
      id: item.guid,
      title: t('notifications.kinds.3', {
        workflow: stringArgument(item.argumentsJson, 'workflow'),
        open: numberArgument(item.argumentsJson, 'open'),
        unassigned: numberArgument(item.argumentsJson, 'unassigned'),
        overdue: numberArgument(item.argumentsJson, 'overdue'),
        closed: numberArgument(item.argumentsJson, 'closed'),
      }),
      severity: item.priorityId >= 3 ? 'warning' : 'info',
      at: item.createdDate,
      read: item.read,
    }
  }

  if (item.kindId === sanctionRescreenHits) {
    return {
      id: item.guid,
      title: t('notifications.kinds.2', {count: numberArgument(item.argumentsJson, 'count')}),
      severity: item.priorityId >= 3 ? 'warning' : 'info',
      at: item.createdDate,
      read: item.read,
    }
  }

  return {
    id: item.guid,
    title: known
      ? t('notifications.kinds.1', {
        id: caseLabel(item.caseId as number),
        dimension: t(`notifications.dimensions.${dimension}`),
      })
      : t('notifications.kinds.unknown'),
    severity: item.priorityId >= 3 ? 'warning' : 'info',
    at: item.createdDate,
    read: item.read,
    caseId: item.caseId ?? undefined,
  }
}
