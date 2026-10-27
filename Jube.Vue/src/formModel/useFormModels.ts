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
import { computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { queryKeys } from '../api/queryKeys'
import { formModelApi } from './api'
import type { FormModel } from './types'

export function useFormModels() {
  const client = useQueryClient()

  const query = useQuery({
    queryKey: computed(() => queryKeys.formModels()),
    queryFn: async (): Promise<FormModel[]> => {
      const areas = await formModelApi.areas()

      return Promise.all(
        areas.map((area) =>
          client.fetchQuery({
            queryKey: queryKeys.formModel(area),
            queryFn: () => formModelApi.model(area),
            staleTime: 30_000,
          }),
        ),
      )
    },
    retry: false,
  })

  const models = computed(() =>
    [...(query.data.value ?? [])].sort((a, b) => a.area.localeCompare(b.area)),
  )

  return {
    query,
    models,
    withApproval: computed(() => models.value.filter((model) => model.approval !== null)),
    withRoles: computed(() => models.value.filter((model) => model.roles !== null)),
    withValues: computed(() => models.value.filter((model) => model.values !== null)),
  }
}
