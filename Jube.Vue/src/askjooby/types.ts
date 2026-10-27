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

export interface Attachment {
  id: number
  name: string
}

export interface PendingAttachment {
  key: string
  id?: number
  name: string
  uploading: boolean
}

export interface ConversationSummary {
  id: number
  title: string | null
  createdDate: string
  updatedDate: string | null
}

export type ActivityKind =
  | 'Step'
  | 'Thought'
  | 'Thinking'
  | 'Delegation'
  | 'Handoff'
  | 'Validation'
  | 'Revision'
  | 'Guard'
  | 'Error'

export interface MessageStats {
  inputTokens: number | null
  outputTokens: number | null
  durationMs: number | null
  cachedInputTokens: number | null
  joobrikCount: number | null
  distinctJoobrikCount: number | null
  totalCost: number | null
}

export type TranscriptEntry =
  | ({
      kind: 'Message'
      id: number
      role: 'User' | 'Assistant'
      text: string
      createdDate: string
      attachments: Attachment[] | null
    } & Partial<MessageStats>)
  | {
      kind: Exclude<ActivityKind, 'Error'>
      text: string
      detail: string | null
      iconId: number | null
      createdDate: string
    }

export type StreamItem =
  | { kind: 'Title' }
  | { kind: 'Text'; text?: string; messageId?: number; attachments?: Attachment[] }
  | { kind: 'Thinking'; text?: string; agentLabel?: string }
  | { kind: 'Step'; text: string; detail?: string; iconId?: number; agentLabel?: string }
  | { kind: 'Thought'; text: string; agentLabel?: string }
  | { kind: 'Guard'; text: string; detail?: string }
  | {
      kind: 'Delegation' | 'Handoff' | 'Validation' | 'Revision'
      text: string
      detail?: string
      agentLabel?: string
    }
  | ({ kind: 'Stats' } & Partial<MessageStats>)
  | { kind: 'Suggestions'; suggestions: string[] }
  | { kind: 'Clarification'; options: string[]; allowFreeText: boolean }
  | { kind: 'Error'; text: string }

export interface UploadedFile {
  id: number
  name?: string
}
