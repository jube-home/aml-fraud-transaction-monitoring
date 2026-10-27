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

import { ref } from 'vue'
import { defineStore } from 'pinia'
import { useI18n } from 'vue-i18n'
import { ApiError } from '../api/useApi'

export type ToastKind = 'info' | 'success' | 'error'

export interface Toast {
  id: number
  kind: ToastKind
  text: string
}

const lifetime: Record<ToastKind, number> = { info: 6000, success: 6000, error: 10000 }
const visibleLimit = 4

export const useToastStore = defineStore('toasts', () => {
  const items = ref<Toast[]>([])
  let nextId = 1

  function dismiss(id: number): void {
    items.value = items.value.filter((toast) => toast.id !== id)
  }

  function push(kind: ToastKind, text: string): number {
    const existing = items.value.find((toast) => toast.kind === kind && toast.text === text)

    if (existing) return existing.id

    const id = nextId++

    items.value = [...items.value, { id, kind, text }].slice(-visibleLimit)
    setTimeout(() => dismiss(id), lifetime[kind])

    return id
  }

  return { items, push, dismiss }
})

export function useToasts() {
  const store = useToastStore()
  const { t } = useI18n()

  function apiError(error: unknown): number {
    if (!(error instanceof ApiError)) return store.push('error', t('errors.unexpected'))

    switch (error.kind) {
      case 'forbidden':
        return store.push('error', t('errors.forbidden'))
      case 'conflict':
        return store.push('error', t('errors.conflict'))
      case 'network':
        return store.push('error', t('errors.network'))
      case 'notFound':
        return store.push('error', t('errors.notFound'))
      default:
        return store.push('error', t('errors.server'))
    }
  }

  return {
    info: (text: string) => store.push('info', text),
    success: (text: string) => store.push('success', text),
    error: (text: string) => store.push('error', text),
    apiError,
    dismiss: store.dismiss,
  }
}
