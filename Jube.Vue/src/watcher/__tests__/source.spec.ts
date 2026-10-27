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

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'
import type { ReplayQuery, WatcherSource } from '../source'
import { useWatcher } from '../useWatcher'
import type { WatcherOptions } from '../types'

const options = ref<WatcherOptions>({ mode: 'all', maxRows: 10 })

const journal = (id: number, latitude: number, longitude: number, at: string) => ({
  id,
  key: 'CaseKey',
  keyValue: 'CASE-9',
  latitude,
  longitude,
  createdDate: at,
  activationRuleSummary: 'Card present',
})

describe('a different source of the same shape', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('replays whatever the source returns, asking it for exactly the query, with no stream needed', async () => {
    const asked: ReplayQuery[] = []
    const source: WatcherSource = {
      replay: (query) => {
        asked.push(query)

        return Promise.resolve([
          journal(1, 51.5, -0.12, '2026-10-21T10:00:00Z'),
          journal(2, 48.86, 2.35, '2026-10-21T10:40:00Z'),
        ])
      },
    }

    const scope = effectScope()
    const watcher = scope.run(() => useWatcher(options, ref(true), source))!

    await watcher.replay(null, null, 'CaseKey', 'CASE-9')
    vi.advanceTimersByTime(1000)

    expect(asked).toEqual([{ from: null, to: null, key: 'CaseKey', keyValue: 'CASE-9' }])
    expect(watcher.state.value.history.map((item) => item.entity)).toEqual([
      'CaseKey:CASE-9',
      'CaseKey:CASE-9',
    ])
    expect(watcher.status.value).toBe('idle')

    scope.stop()
  })

  it('listens to a live source when it has one, and says so when it fails to replay', async () => {
    let deliver: (raw: unknown) => void = () => undefined
    let closed = 0
    const source: WatcherSource = {
      replay: () => Promise.reject(new Error('no')),
      stream: (onEvent, onStatus) => {
        deliver = onEvent
        onStatus('open')

        return () => {
          closed += 1
        }
      },
    }

    const live = ref(true)
    const scope = effectScope()
    const watcher = scope.run(() => useWatcher(options, live, source))!

    deliver(journal(3, 40.7, -74, '2026-10-21T11:00:00Z'))
    vi.advanceTimersByTime(1000)
    await watcher.replay(null, null)

    expect(watcher.status.value).toBe('open')
    expect(watcher.state.value.history).toHaveLength(1)
    expect(watcher.replayFailed.value).toBe(true)

    live.value = false
    await nextTick()

    expect(closed).toBe(1)
    expect(watcher.status.value).toBe('idle')

    scope.stop()
  })
})
