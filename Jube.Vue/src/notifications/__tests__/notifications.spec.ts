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

import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { parseNotification, useNotificationStore } from '../notifications'

beforeEach(() => {
  setActivePinia(createPinia())
})

describe('parseNotification', () => {
  it('reads either casing and defaults the severity', () => {
    expect(parseNotification({ Id: 7, Title: 'Case assigned', Severity: 'Warning' })).toMatchObject(
      {
        id: '7',
        title: 'Case assigned',
        severity: 'warning',
        read: false,
      },
    )
    expect(parseNotification({ id: 'a', title: 'x' })!.severity).toBe('info')
  })

  it('rejects anything without an id and a title', () => {
    expect(parseNotification({ title: 'x' })).toBeUndefined()
    expect(parseNotification({ id: 1 })).toBeUndefined()
    expect(parseNotification('x')).toBeUndefined()
  })
})

describe('the notification store', () => {
  it('counts unread, ignores duplicates, and marks read', () => {
    const store = useNotificationStore()

    store.receive(parseNotification({ id: 1, title: 'a' })!)
    store.receive(parseNotification({ id: 1, title: 'a again' })!)
    store.receive(parseNotification({ id: 2, title: 'b' })!)

    expect(store.items.map((item) => item.id)).toEqual(['2', '1'])
    expect(store.unread).toBe(2)

    store.markRead('1')
    expect(store.unread).toBe(1)
    store.markAllRead()
    expect(store.unread).toBe(0)
  })

  it('keeps the newest fifty', () => {
    const store = useNotificationStore()

    for (let id = 0; id < 60; id += 1) store.receive(parseNotification({ id, title: 'n' })!)

    expect(store.items).toHaveLength(50)
    expect(store.items[0]!.id).toBe('59')
  })
})
