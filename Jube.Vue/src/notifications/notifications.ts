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

import { computed, ref } from 'vue'
import { defineStore } from 'pinia'

export type NotificationSeverity = 'info' | 'warning' | 'error'

export interface AppNotification {
  id: string
  title: string
  body?: string
  severity: NotificationSeverity
  at: string
  read: boolean
}

export function parseNotification(payload: unknown): AppNotification | undefined {
  if (typeof payload !== 'object' || payload === null) return undefined

  const value = payload as Record<string, unknown>
  const id = value.id ?? value.Id
  const title = value.title ?? value.Title

  if (typeof id !== 'string' && typeof id !== 'number') return undefined
  if (typeof title !== 'string') return undefined

  const severity = String(value.severity ?? value.Severity ?? 'info').toLowerCase()
  const body = value.body ?? value.Body
  const at = value.at ?? value.At

  return {
    id: String(id),
    title,
    body: typeof body === 'string' ? body : undefined,
    severity: severity === 'warning' || severity === 'error' ? severity : 'info',
    at: typeof at === 'string' ? at : new Date().toISOString(),
    read: false,
  }
}

export const useNotificationStore = defineStore('notifications', () => {
  const items = ref<AppNotification[]>([])
  const unread = computed(() => items.value.filter((notification) => !notification.read).length)

  function receive(notification: AppNotification): void {
    if (items.value.some((existing) => existing.id === notification.id)) return

    items.value = [notification, ...items.value].slice(0, 50)
  }

  function markRead(id: string): void {
    const notification = items.value.find((existing) => existing.id === id)

    if (notification) notification.read = true
  }

  function markAllRead(): void {
    items.value.forEach((notification) => {
      notification.read = true
    })
  }

  function clear(): void {
    items.value = []
  }

  return { items, unread, receive, markRead, markAllRead, clear }
})
