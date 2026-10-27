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
import { useApi } from '../api/useApi'
import {
  kindOfFile,
  type Dataset,
  type DatasetKind,
  type DatasetMapping,
  type DatasetSummary,
} from './model'

const endpoints: Record<DatasetKind, string> = {
  Csv: '/api/ExhaustiveInstanceCsv',
  Json: '/api/ExhaustiveInstanceJson',
}

export const datasetApi = {
  list: () => useApi<DatasetSummary[]>('/api/ExhaustiveInstanceDataset'),
  get: (kind: DatasetKind, id: number) => useApi<Dataset>(`${endpoints[kind]}/${id}`),
  upload: (file: File) => {
    const formData = new FormData()

    formData.append('files', file)

    return useApi<Dataset>(`${endpoints[kindOfFile(file.name)]}/Upload`, {
      method: 'POST',
      formData,
    })
  },
  confirm: (kind: DatasetKind, mapping: DatasetMapping) =>
    useApi<Dataset>(`${endpoints[kind]}/Confirm`, { method: 'PUT', body: mapping }),
  remove: (kind: DatasetKind, id: number) =>
    useApi(`${endpoints[kind]}/${id}`, { method: 'DELETE' }),
}
