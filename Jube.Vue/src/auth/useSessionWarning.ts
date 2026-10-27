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

import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useApi } from '../api/useApi'
import { sessionExpiresAt } from './sessionExpiry'

export const warningThresholdMs = 5 * 60_000
const checkIntervalMs = 10_000
const activityThrottleMs = 3 * 60_000

export function useSessionWarning() {
  const now = ref(Date.now())
  const extending = ref(false)

  let ticker: ReturnType<typeof setInterval> | undefined
  let lastPing = 0

  const remainingMs = computed(() =>
    sessionExpiresAt.value === undefined ? undefined : sessionExpiresAt.value - now.value,
  )
  const warning = computed(
    () => remainingMs.value !== undefined && remainingMs.value <= warningThresholdMs,
  )

  async function extend(): Promise<boolean> {
    if (extending.value) return false

    extending.value = true

    try {
      await useApi('/api/Session/Extend')
      now.value = Date.now()

      return true
    } catch {
      return false
    } finally {
      extending.value = false
    }
  }

  function onActivity(): void {
    const at = Date.now()

    if (at - lastPing < activityThrottleMs || sessionExpiresAt.value === undefined) return

    lastPing = at
    void useApi('/api/Session/Extend').catch(() => undefined)
  }

  onMounted(() => {
    ticker = setInterval(() => {
      now.value = Date.now()
    }, checkIntervalMs)

    for (const type of ['click', 'change', 'submit']) {
      document.addEventListener(type, onActivity, true)
    }
  })

  onBeforeUnmount(() => {
    clearInterval(ticker)

    for (const type of ['click', 'change', 'submit']) {
      document.removeEventListener(type, onActivity, true)
    }
  })

  return { warning, extend, extending, tick: () => (now.value = Date.now()) }
}
