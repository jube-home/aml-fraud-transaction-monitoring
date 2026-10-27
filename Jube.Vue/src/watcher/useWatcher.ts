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

import { computed, onBeforeUnmount, ref, shallowRef, watch, type Ref } from 'vue'
import { parseActivation } from './parse'
import { emptyState, ingest } from './state'
import { activationWatcherSource, type WatcherSource } from './source'
import type { WatcherStreamStatus } from './stream'
import type { Activation, WatcherOptions, WatcherState } from './types'

const batchSize = 100
const tickMilliseconds = 400

export function useWatcher(
  options: Ref<WatcherOptions>,
  realtime: Ref<boolean>,
  source: WatcherSource = activationWatcherSource,
) {
  const state = shallowRef<WatcherState>(emptyState())
  const status = ref<WatcherStreamStatus>('idle')
  const pending = ref<Activation[]>([])
  const received = ref(0)
  const replaying = ref(false)
  const replayFailed = ref(false)

  let close: (() => void) | undefined

  function push(raw: unknown[]): void {
    const parsed = raw
      .map((item) => parseActivation(item))
      .filter((item): item is Activation => item !== null)

    received.value += raw.length

    if (parsed.length > 0) pending.value = [...pending.value, ...parsed]
  }

  function drain(): void {
    if (pending.value.length === 0) return

    const batch = pending.value.slice(0, batchSize)

    pending.value = pending.value.slice(batchSize)
    state.value = ingest(state.value, batch, options.value)
  }

  const timer = setInterval(drain, tickMilliseconds)

  function connect(): void {
    close?.()
    close = source.stream?.(
      (payload) => push([payload]),
      (next) => {
        status.value = next
      },
    )
  }

  watch(
    realtime,
    (on) => {
      if (on) connect()
      else {
        close?.()
        close = undefined
        status.value = 'idle'
      }
    },
    { immediate: true },
  )

  async function replay(
    from: Date | null,
    to: Date | null,
    key?: string,
    keyValue?: string,
  ): Promise<void> {
    replaying.value = true
    replayFailed.value = false

    try {
      push(await source.replay({ from, to, key, keyValue }))
    } catch {
      replayFailed.value = true
    } finally {
      replaying.value = false
    }
  }

  function clear(): void {
    pending.value = []
    state.value = emptyState()
  }

  onBeforeUnmount(() => {
    clearInterval(timer)
    close?.()
  })

  return {
    state,
    status,
    pendingCount: computed(() => pending.value.length),
    received,
    replaying,
    replayFailed,
    push,
    replay,
    clear,
  }
}
