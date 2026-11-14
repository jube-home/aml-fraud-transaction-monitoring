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

import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest'
import {QueryClient} from '@tanstack/vue-query'
import {
  applyTarget,
  canReload,
  isDifferentBuild,
  maxReloadAttempts,
  newVersionAvailable,
  noteReload,
  reloadAttempts,
} from '../clientVersion'
import {startAppEvents, stopAppEvents} from '../appEvents'

let meta: HTMLMetaElement

beforeEach(() => {
  sessionStorage.clear()
  meta = document.createElement('meta')
  meta.name = 'jube-build-id'
  meta.content = '1000'
  document.head.append(meta)
})

afterEach(() => {
  meta.remove()
  newVersionAvailable.value = false
  stopAppEvents()
  vi.unstubAllGlobals()
})

describe('isDifferentBuild', () => {
  it('differs in either direction, so a rollback counts as well as a release', () => {
    expect(isDifferentBuild('1001')).toBe(true)
    expect(isDifferentBuild('999')).toBe(true)
    expect(isDifferentBuild('1000')).toBe(false)
  })

  it('never fires without a usable build on either side', () => {
    expect(isDifferentBuild('', '1000')).toBe(false)
    expect(isDifferentBuild('abc', '1000')).toBe(false)
    expect(isDifferentBuild('1001', '')).toBe(false)
  })
})

describe('applyTarget', () => {
  it('waits for the next navigation on a first sighting and does not reload by itself', () => {
    const reload = vi.fn<() => void>()
    const schedule = vi.fn() as unknown as typeof setTimeout

    applyTarget('1001', reload, schedule)

    expect(newVersionAvailable.value).toBe(true)
    expect(schedule).not.toHaveBeenCalled()
    expect(reload).not.toHaveBeenCalled()
  })

  it('retries after a backoff when a reload landed on a replica still on the old build', () => {
    const reload = vi.fn<() => void>()
    let run: (() => void) | undefined
    const schedule = ((callback: () => void) => {
      run = callback
    }) as unknown as typeof setTimeout

    noteReload('1001')
    applyTarget('1001', reload, schedule)
    run?.()

    expect(reload).toHaveBeenCalledOnce()
    expect(reloadAttempts('1001')).toBe(2)
  })

  it('gives up after the attempt limit', () => {
    const schedule = vi.fn() as unknown as typeof setTimeout

    for (let index = 0; index < maxReloadAttempts; index += 1) noteReload('1001')

    applyTarget('1001', vi.fn(), schedule)

    expect(canReload('1001')).toBe(false)
    expect(schedule).not.toHaveBeenCalled()
  })

  it('clears the record once the tab is on the target build', () => {
    noteReload('1000')
    applyTarget('1000', vi.fn())

    expect(newVersionAvailable.value).toBe(false)
    expect(reloadAttempts('1000')).toBe(0)
  })
})

describe('the stream', () => {
  class FakeEventSource {
    static instance: FakeEventSource | undefined
    readonly listeners = new Map<string, (event: MessageEvent<string>) => void>()

    constructor() {
      FakeEventSource.instance = this
    }

    addEventListener(type: string, listener: (event: MessageEvent<string>) => void): void {
      this.listeners.set(type, listener)
    }

    close(): void {}
  }

  it('applies the cluster target announced on the build event', () => {
    vi.stubGlobal('EventSource', FakeEventSource)
    startAppEvents({
      client: new QueryClient(),
      onNotification: vi.fn(),
      onRevoked: vi.fn(),
      reload: vi.fn(),
    })

    FakeEventSource.instance!.listeners.get('build')!(new MessageEvent('build', {data: '999'}))

    expect(newVersionAvailable.value).toBe(true)
  })
})
