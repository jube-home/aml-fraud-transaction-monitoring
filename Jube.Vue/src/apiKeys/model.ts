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

export interface ApiKey {
  id: number
  userRegistryId: number
  name: string | null
  description: string | null
  createdDate: string | null
  apiKeyDisplay: string | null
}

export interface ApiKeyDraft {
  name: string
  description: string
}

export type ApiKeyProblem = 'nameRequired' | 'nameTooLong' | 'descriptionTooLong'

export const maxNameLength = 256
export const maxDescriptionLength = 1024
export const prefixLength = 8

export function emptyDraft(): ApiKeyDraft {
  return { name: '', description: '' }
}

export function draftProblems(draft: ApiKeyDraft): ApiKeyProblem[] {
  const problems: ApiKeyProblem[] = []
  const name = draft.name.trim()

  if (name === '') problems.push('nameRequired')
  else if (name.length > maxNameLength) problems.push('nameTooLong')

  if (draft.description.trim().length > maxDescriptionLength) problems.push('descriptionTooLong')

  return problems
}

export function payloadOf(
  userRegistryId: number,
  draft: ApiKeyDraft,
): Pick<ApiKey, 'userRegistryId' | 'name' | 'description'> {
  const description = draft.description.trim()

  return {
    userRegistryId,
    name: draft.name.trim(),
    description: description === '' ? null : description,
  }
}

export function prefixOf(display: string | null): string {
  return (display ?? '').slice(0, prefixLength)
}

export function newestFirst(keys: ApiKey[]): ApiKey[] {
  return [...keys].sort((a, b) => {
    const aTime = Date.parse(a.createdDate ?? '')
    const bTime = Date.parse(b.createdDate ?? '')
    const aDated = !Number.isNaN(aTime)
    const bDated = !Number.isNaN(bTime)

    if (aDated !== bDated) return aDated ? -1 : 1

    if (aDated && bDated && aTime !== bTime) return bTime - aTime

    return b.id - a.id
  })
}
