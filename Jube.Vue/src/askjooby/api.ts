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

import { useApi, useApiStream } from '../api/useApi'
import { readStreamItems } from './stream'
import type { StartupStats } from './offlineWelcome'
import type { ConversationSummary, StreamItem, TranscriptEntry, UploadedFile } from './types'

const base = '/api/AskJoobyConversation'

export const joobyApi = {
  list: (search: string) => useApi<ConversationSummary[]>(base, { query: { search } }),
  create: () => useApi<ConversationSummary>(base, { method: 'POST' }),
  remove: (id: number) => useApi(`${base}/${id}`, { method: 'DELETE' }),
  messages: (id: number) => useApi<TranscriptEntry[]>(`${base}/${id}/Messages`),
  startupStats: () => useApi<StartupStats>(`${base}/StartupStats`),
  guidance: (id: number, guidance: string) =>
    useApi(`${base}/${id}/Guidance`, { method: 'POST', body: { guidance } }),
  feedback: (conversationId: number, messageId: number, isPositive: boolean) =>
    useApi<string>(`${base}/${conversationId}/Messages/${messageId}/Feedback`, {
      method: 'POST',
      body: { isPositive },
    }),
  upload: (conversationId: number, file: File) => {
    const formData = new FormData()

    formData.append('files', file)

    return useApi<UploadedFile>(`${base}/${conversationId}/Files`, { method: 'POST', formData })
  },
  removeFile: (fileId: number) => useApi(`${base}/Files/${fileId}`, { method: 'DELETE' }),
  fileText: (fileId: number) => useApi<string>(`${base}/Files/${fileId}`),
  fileUrl: (fileId: number) => `${base}/Files/${fileId}`,
  transcriptUrl: (id: number) => `${base}/${id}/Transcript`,
  auditUrl: (id: number) => `${base}/${id}/Audit`,
  stream(
    path: 'Start' | 'SendMessage',
    conversationId: number,
    body: unknown,
    signal: AbortSignal,
  ): Promise<AsyncGenerator<StreamItem>> {
    return useApiStream(`${base}/${conversationId}/${path}`, { body, signal }).then(readStreamItems)
  },
}

export type JoobyApi = typeof joobyApi
