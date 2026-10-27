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

export type Operate = 'sample' | 'integrity' | 'analytics'

export interface StoryStep {
  area: string
  id: number
}

export interface StoryAddress {
  modelId: number | undefined
  operate: Operate | undefined
  steps: StoryStep[]
}

const operates: Operate[] = ['sample', 'integrity', 'analytics']

export const modelArea = 'EntityAnalysisModel'

export function encodeAddress(address: StoryAddress): string {
  if (address.modelId === undefined) return ''

  return [
    `${modelArea}:${address.modelId}`,
    ...(address.operate ? [`~${address.operate}`] : []),
    ...address.steps.map((step) => `${step.area}:${step.id}`),
  ].join(',')
}

export function decodeAddress(text: string | undefined): StoryAddress {
  const empty: StoryAddress = { modelId: undefined, operate: undefined, steps: [] }

  if (!text) return empty

  const parts = text.split(',')
  const [area, id] = (parts[0] ?? '').split(':')
  const modelId = Number(id)

  if (area !== modelArea || !Number.isInteger(modelId) || modelId <= 0) return empty

  const operate = operates.find((candidate) => `~${candidate}` === parts[1])
  const rest = parts.slice(operate ? 2 : 1)

  return {
    modelId,
    operate,
    steps: rest.flatMap((part): StoryStep[] => {
      const [stepArea, stepId] = part.split(':')
      const number = Number(stepId)

      return stepArea && Number.isInteger(number) && number > 0
        ? [{ area: stepArea, id: number }]
        : []
    }),
  }
}
