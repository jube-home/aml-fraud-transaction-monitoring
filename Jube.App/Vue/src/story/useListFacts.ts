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

import {computed, type Ref} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQueries} from '@tanstack/vue-query'
import {queryKeys} from '../api/queryKeys'
import {formModelApi} from '../formModel/api'
import {summariseChildren, type Level} from '../formModel/hierarchy'
import type {FormModel} from '../formModel/types'
import type {StoryFact} from './model'

export function useListFacts(
  levels: Ref<Level[]>,
  modelOf: (area: string) => FormModel | undefined,
  awaitingFor?: (level: Level) => number | undefined,
): (level: Level) => StoryFact[] {
  const {t} = useI18n()

  const listLevels = computed(() =>
    levels.value.filter((level) => !level.record && !level.startNew),
  )

  const results = useQueries({
    queries: computed(() =>
      listLevels.value.map((level) => ({
        queryKey: queryKeys.formRecords(level.area, level.parentValue),
        queryFn: () => formModelApi.records(modelOf(level.area)!, level.parentValue, level.listRoute),
        enabled: modelOf(level.area) !== undefined && level.parentValue !== undefined,
        retry: false,
      })),
    ),
  })

  return (level) => {
    const index = listLevels.value.findIndex((candidate) => candidate.key === level.key)
    const rows = index < 0 ? undefined : results.value[index]?.data

    if (!rows) return []

    const summary = summariseChildren(modelOf(level.area), rows)

    if (summary.total === 0) return [{text: t('hierarchy.none')}]

    const facts: StoryFact[] = [{text: t('hierarchy.total', {count: summary.total})}]

    if (summary.active !== null) {
      facts.push({text: t('hierarchy.active', {count: summary.active}), tone: summary.active > 0 ? 'success' : 'neutral'})
    }

    if (summary.locked !== null && summary.locked > 0) {
      facts.push({text: t('hierarchy.locked', {count: summary.locked}), tone: 'warning'})
    }

    const awaiting = awaitingFor?.(level)

    if (awaiting !== undefined && awaiting > 0) {
      facts.push({text: t('hierarchy.awaiting', {count: awaiting}), tone: 'warning'})
    }

    return facts
  }
}
