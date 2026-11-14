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

import {useApi, useApiDownload} from '../api/useApi'
import type {Diff, ImportPreview, MoveOptions, Snapshot, SnapshotRequest, Summary} from './types'

async function packageBody(file: File, password: string) {
  return {
    password,
    fileName: file.name,
    package: new Uint8Array(await file.arrayBuffer()),
  }
}

export const snapshotApi = {
  list: () => useApi<Snapshot[]>('/api/PreservationSnapshot', {query: {limit: 200}}),

  take: (request: SnapshotRequest) =>
    useApi<Snapshot>('/api/PreservationSnapshot', {method: 'POST', body: request}),

  remove: (id: number) => useApi(`/api/PreservationSnapshot/${id}`, {method: 'DELETE'}),

  restore: (id: number) =>
    useApi<Snapshot>(`/api/PreservationSnapshot/${id}/Import`, {method: 'POST'}),

  previewRestore: (id: number) =>
    useApi<ImportPreview>(`/api/PreservationSnapshot/${id}/ImportPreview`, {method: 'POST'}),

  diff: (leftSnapshotId: number | null, rightSnapshotId: number | null) =>
    useApi<Diff>('/api/PreservationSnapshot/Diff', {
      method: 'POST',
      body: {leftSnapshotId, rightSnapshotId},
    }),

  body: (id: number) => useApiDownload(`/api/PreservationSnapshot/${id}/Body`, {method: 'GET'}),
}

export const preservationApi = {
  summary: () => useApi<Summary>('/api/Preservation/Summary'),

  exportPackage: (options: MoveOptions) =>
    useApiDownload('/api/Preservation/Export', {method: 'POST', body: options}),

  previewPackage: async (file: File, password: string) =>
    useApi<ImportPreview>('/api/Preservation/ImportPreview', {
      method: 'POST',
      body: await packageBody(file, password),
    }),

  importPackage: async (file: File, password: string) =>
    useApi('/api/Preservation/Import', {method: 'POST', body: await packageBody(file, password)}),
}
