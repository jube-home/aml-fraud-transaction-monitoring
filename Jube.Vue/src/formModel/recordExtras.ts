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
import type { Component } from 'vue'
import ExhaustiveResults from '../components/ExhaustiveResults.vue'
import { resultsFromStatus } from '../exhaustive/model'

export type FormValues = Record<string, unknown>

export type Translate = (key: string, params?: Record<string, unknown>) => string

export interface RecordExtra {
  component: Component
  propsOf: (values: FormValues) => Record<string, unknown>
  folds?: (values: FormValues) => boolean
  summary?: (values: FormValues, t: Translate) => string
}

function sourceLabel(id: unknown, t: Translate): string {
  switch (Number(id)) {
    case 1:
      return t('exhaustive.fold.demonstration')
    case 2:
      return t('exhaustive.fold.uploaded')
    default:
      return t('exhaustive.fold.archive')
  }
}

export const recordExtras: Record<string, RecordExtra> = {
  ExhaustiveSearchInstance: {
    component: ExhaustiveResults,
    propsOf: (values) => ({ instanceId: Number(values.id) }),
    folds: (values) => Number(values.statusId) >= resultsFromStatus,
    summary: (values, t) => {
      const state = (flag: unknown) => (flag ? t('exhaustive.fold.on') : t('exhaustive.fold.off'))

      return t('exhaustive.fold.summary', {
        source: sourceLabel(values.dataSourceId, t),
        filter: state(values.filter),
        bootstrap: state(values.bootstrap),
        anomaly: state(values.anomaly),
      })
    },
  },
}

export function extraFor(area: string): RecordExtra | undefined {
  return recordExtras[area]
}
