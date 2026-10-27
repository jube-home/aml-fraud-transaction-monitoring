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

import type { FormModel, RecordValues } from '../formModel/types'
import type { StoryFact } from './model'

export interface FactLabels {
  active: string
  inactive: string
  locked: string
  version: (version: number) => string
  awaiting: (count: number) => string
}

export function recordFacts(
  model: FormModel | undefined,
  values: RecordValues,
  labels: FactLabels,
  awaiting?: number,
): StoryFact[] {
  const facts: StoryFact[] = []
  const version = values.version

  if (typeof version === 'number' && version > 0) facts.push({ text: labels.version(version) })

  if (model?.fields.some((field) => field.name === 'active' && field.type === 'boolean')) {
    facts.push(
      values.active === true
        ? { text: labels.active, tone: 'success' }
        : { text: labels.inactive, tone: 'neutral' },
    )
  }

  const lock = model?.lockFields[0]

  if (lock && values[lock] === true) facts.push({ text: labels.locked, tone: 'warning' })

  if (awaiting !== undefined && awaiting > 0) {
    facts.push({ text: labels.awaiting(awaiting), tone: 'warning' })
  }

  return facts
}
