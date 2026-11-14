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

import {beforeEach, describe, expect, it} from 'vitest'
import {createPinia, setActivePinia} from 'pinia'
import {fromServer} from '../fromServer'
import {useNotificationStore} from '../notifications'
import type {ServerNotification} from '../api'

const t = (key: string, named?: Record<string, unknown>): string =>
  named ? `${key}:${JSON.stringify(named)}` : key

function item(over: Partial<ServerNotification> = {}): ServerNotification {
  return {
    guid: 'g-1',
    kindId: 1,
    priorityId: 2,
    caseId: 42,
    argumentsJson: '{"dimension":3}',
    createdDate: '2026-11-10T10:00:00Z',
    read: false,
    ...over,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('fromServer', () => {
  it('renders a service level breach with the case and the dimension', () => {
    const mapped = fromServer(item(), t)

    expect(mapped.id).toBe('g-1')
    expect(mapped.title).toContain('notifications.kinds.1')
    expect(mapped.title).toContain('notifications.dimensions.3')
    expect(mapped.caseId).toBe(42)
    expect(mapped.read).toBe(false)
  })

  it('maps high priority to a warning and anything lower to info', () => {
    expect(fromServer(item({priorityId: 3}), t).severity).toBe('warning')
    expect(fromServer(item({priorityId: 2}), t).severity).toBe('info')
  })

  it('tolerates missing or malformed arguments', () => {
    expect(fromServer(item({argumentsJson: null}), t).title).toContain(
      'notifications.dimensions.0',
    )
    expect(fromServer(item({argumentsJson: '{nope'}), t).title).toContain(
      'notifications.dimensions.0',
    )
  })

  it('renders a rescreen notification with its count and no case', () => {
    const mapped = fromServer(
      item({kindId: 2, caseId: null, priorityId: 3, argumentsJson: '{"count":4,"run":9}'}),
      t,
    )

    expect(mapped.title).toContain('notifications.kinds.2')
    expect(mapped.title).toContain('"count":4')
    expect(mapped.severity).toBe('warning')
    expect(mapped.caseId).toBeUndefined()
  })

  it('renders a digest with the workflow and its counts', () => {
    const mapped = fromServer(
      item({
        kindId: 3,
        caseId: null,
        priorityId: 1,
        argumentsJson: '{"workflow":"Card fraud","open":3,"unassigned":2,"overdue":1,"closed":4}',
      }),
      t,
    )

    expect(mapped.title).toContain('notifications.kinds.3')
    expect(mapped.title).toContain('"workflow":"Card fraud"')
    expect(mapped.title).toContain('"overdue":1')
    expect(mapped.severity).toBe('info')
  })

  it('falls back for a kind it does not know', () => {
    expect(fromServer(item({kindId: 99}), t).title).toBe('notifications.kinds.unknown')
  })

  it('carries the read flag from the server', () => {
    expect(fromServer(item({read: true}), t).read).toBe(true)
  })
})

describe('the store after a server load', () => {
  it('replaces its items so the server is the source of truth', () => {
    const store = useNotificationStore()

    store.replace([fromServer(item(), t), fromServer(item({guid: 'g-2', read: true}), t)])
    expect(store.items.map((n) => n.id)).toEqual(['g-1', 'g-2'])
    expect(store.unread).toBe(1)

    store.replace([fromServer(item({guid: 'g-3'}), t)])
    expect(store.items.map((n) => n.id)).toEqual(['g-3'])
  })
})
