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

import type { StoryStep } from '../story/path'

export interface AreaAddress {
  rootId: string | undefined
  recordId: number | undefined
  view: string | undefined
  steps: StoryStep[]
}

export const emptyAddress: AreaAddress = {
  rootId: undefined,
  recordId: undefined,
  view: undefined,
  steps: [],
}

export function encodeAreaAddress(address: AreaAddress): string {
  if (address.rootId === undefined) return ''

  return [
    address.recordId === undefined ? address.rootId : `${address.rootId}:${address.recordId}`,
    ...(address.view ? [`~${address.view}`] : []),
    ...address.steps.map((step) => `${step.area}:${step.id}`),
  ].join(',')
}

export function decodeAreaAddress(text: string | undefined, knownRoots: string[]): AreaAddress {
  if (!text) return emptyAddress

  const parts = text.split(',')
  const [rootId, id] = (parts[0] ?? '').split(':')

  if (!rootId || !knownRoots.includes(rootId)) return emptyAddress

  const recordNumber = id === undefined ? undefined : Number(id)
  const recordId =
    recordNumber !== undefined && Number.isInteger(recordNumber) && recordNumber > 0
      ? recordNumber
      : undefined
  const viewPart = parts[1]?.startsWith('~') ? parts[1].slice(1) : undefined
  const view = viewPart === '' ? undefined : viewPart
  const rest = parts.slice(viewPart === undefined ? 1 : 2)

  return {
    rootId,
    recordId,
    view,
    steps:
      recordId === undefined
        ? []
        : rest.flatMap((part): StoryStep[] => {
            const [area, stepId] = part.split(':')
            const number = Number(stepId)

            return area && Number.isInteger(number) && number > 0 ? [{ area, id: number }] : []
          }),
  }
}
