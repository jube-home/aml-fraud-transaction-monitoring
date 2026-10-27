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

import { ApiError } from './useApi'

export interface ProblemText {
  failed: string
  refused?: string
}

export function bodyReasons(body: unknown, fallback: string): string[] {
  if (typeof body === 'object' && body !== null) {
    const record = body as Record<string, unknown>

    if (Array.isArray(record.reasons) && record.reasons.length > 0) {
      return record.reasons.map(String)
    }

    if (typeof record.message === 'string' && record.message) return [record.message]
  }

  return [fallback]
}

export function validationReasons(error: ApiError, fallback: string): string[] {
  const fields = Object.values(error.fieldErrors).flat()

  return fields.length > 0 ? fields : bodyReasons(error.body, fallback)
}

export function describeApiError(error: unknown, text: ProblemText): string[] {
  if (!(error instanceof ApiError)) return [text.failed]

  if (error.kind === 'unauthorized' || error.kind === 'forbidden') {
    return [text.refused ?? text.failed]
  }

  if (error.kind === 'validation') return validationReasons(error, text.failed)

  return [text.failed]
}
