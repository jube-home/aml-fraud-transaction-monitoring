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
import {useConfirmStore} from '../confirm'

const request = {title: 'Title', body: 'Body', confirmLabel: 'Yes', cancelLabel: 'No'}

describe('confirm store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('holds the request until it is answered', async () => {
    const confirm = useConfirmStore()
    const answer = confirm.ask(request)

    expect(confirm.request).toEqual(request)

    confirm.answer(true)

    await expect(answer).resolves.toBe(true)
    expect(confirm.request).toBeUndefined()
  })

  it('resolves false when the request is cancelled', async () => {
    const confirm = useConfirmStore()
    const answer = confirm.ask(request)

    confirm.answer(false)

    await expect(answer).resolves.toBe(false)
  })

  it('cancels a pending request when another is asked', async () => {
    const confirm = useConfirmStore()
    const first = confirm.ask(request)
    const second = confirm.ask({...request, title: 'Second'})

    await expect(first).resolves.toBe(false)
    expect(confirm.request?.title).toBe('Second')

    confirm.answer(true)

    await expect(second).resolves.toBe(true)
  })

  it('ignores an answer when nothing is pending', () => {
    const confirm = useConfirmStore()

    confirm.answer(true)

    expect(confirm.request).toBeUndefined()
  })
})
