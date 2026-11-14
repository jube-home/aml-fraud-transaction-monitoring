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

import type {QueryClient} from '@tanstack/vue-query'
import {ApiError} from '../../api/useApi'
import {checklistKeys} from './api'

export interface ChecklistRefusal {
  message: string
  openItems: string[]
}

export interface RefusalDependencies {
  client: QueryClient
  error: (text: string) => number
  fallback: string
}

function record(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null ? (value as Record<string, unknown>) : null
}

export function checklistRefusal(error: unknown): ChecklistRefusal | null {
  if (!(error instanceof ApiError) || error.status !== 409) return null

  const body = record(error.body)

  if (body === null || body.code !== 'ChecklistIncomplete') return null

  const items = Array.isArray(body.openItems) ? body.openItems.map(String) : []

  return {message: typeof body.message === 'string' ? body.message : '', openItems: items}
}

export function serverMessage(error: unknown): string | null {
  if (!(error instanceof ApiError)) return null

  const first = Object.values(error.fieldErrors)[0]?.[0]

  if (first) return first

  if (typeof error.body === 'string' && error.body.trim() !== '') return error.body

  const body = record(error.body)

  return typeof body?.message === 'string' && body.message !== '' ? body.message : null
}

export function handleChecklistRefusal(error: unknown, dependencies: RefusalDependencies): boolean {
  const refusal = checklistRefusal(error)

  if (refusal === null) return false

  dependencies.error(refusal.message || dependencies.fallback)
  void dependencies.client.invalidateQueries({queryKey: checklistKeys.all})

  return true
}
