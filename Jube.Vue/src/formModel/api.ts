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
import { ApiError, useApi } from '../api/useApi'
import type { FormModel, RecordValues } from './types'

function capitalise(name: string): string {
  return `${name.charAt(0).toUpperCase()}${name.slice(1)}`
}

export const formModelApi = {
  areas: () => useApi<string[]>('/api/FormModel'),
  model: (area: string) => useApi<FormModel>(`/api/FormModel/${encodeURIComponent(area)}`),
  records: async (
    model: FormModel,
    parentId: number | string | undefined,
    listRoute?: string,
  ): Promise<RecordValues[]> => {
    if (listRoute && parentId !== undefined) {
      return useApi<RecordValues[]>(`${listRoute}/${encodeURIComponent(String(parentId))}`)
    }

    const parent = model.keys.parent

    if (!parent || parentId === undefined) return useApi<RecordValues[]>(model.endpoint)

    try {
      return await useApi<RecordValues[]>(`${model.endpoint}/By${capitalise(parent)}/${parentId}`)
    } catch (error) {
      if (!(error instanceof ApiError) || error.kind !== 'notFound') throw error

      const all = await useApi<RecordValues[]>(model.endpoint)

      return all.filter((record) => record[parent] === parentId)
    }
  },
  record: (model: FormModel, id: number) => useApi<RecordValues>(`${model.endpoint}/${id}`),
  create: (model: FormModel, body: RecordValues) =>
    useApi<RecordValues>(model.endpoint, { method: 'POST', body }),
  update: (model: FormModel, body: RecordValues) =>
    useApi<RecordValues>(model.endpoint, { method: 'PUT', body }),
  remove: (model: FormModel, id: number) => useApi(`${model.endpoint}/${id}`, { method: 'DELETE' }),
}
