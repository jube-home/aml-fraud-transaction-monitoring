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

import {effectScope, ref} from 'vue'
import {afterEach, describe, expect, it, vi} from 'vitest'
import {pendingLimit, useWatcher} from '../useWatcher'
import type {WatcherSource} from '../source'

const source: WatcherSource = {replay: async () => []}

afterEach(() => {
  vi.useRealTimers()
})

describe('useWatcher backpressure', () => {
  it('keeps the newest activations when more arrive than the page can draw', () => {
    vi.useFakeTimers()

    const scope = effectScope()
    const watcher = scope.run(() =>
      useWatcher(ref({mode: 'latest' as const, maxRows: 10}), ref(false), source),
    )!

    const batch = Array.from({length: pendingLimit + 50}, (_, index) => ({
      key: 'AccountId',
      keyValue: `ACC-${index}`,
      latitude: 1,
      longitude: 1,
      createdDate: '2026-10-21T10:00:00Z',
      id: index,
    }))

    watcher.push(batch)

    expect(watcher.received.value).toBe(pendingLimit + 50)
    expect(watcher.pendingCount.value).toBe(pendingLimit)

    vi.advanceTimersByTime(400)

    expect(watcher.pendingCount.value).toBe(pendingLimit - 100)
    expect(watcher.state.value.ingested).toBe(100)
    expect(watcher.state.value.history.some((item) => item.keyValue === 'ACC-0')).toBe(false)
    expect(watcher.state.value.history.some((item) => item.keyValue === 'ACC-50')).toBe(true)

    scope.stop()
  })
})
