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

import {afterEach, describe, expect, it, vi} from 'vitest'
import {QueryClient} from '@tanstack/vue-query'
import {createMemoryHistory} from 'vue-router'
import {createAppRouter} from '..'
import {newVersionAvailable, noteReload, reloadAttempts, targetBuild} from '../../events/clientVersion'

afterEach(() => {
  newVersionAvailable.value = false
  targetBuild.value = ''
  sessionStorage.clear()
})

async function routerAt(path: string) {
  const navigate = vi.fn<(href: string) => void>()
  const client = new QueryClient()

  vi.spyOn(client, 'fetchQuery').mockResolvedValue({} as never)

  const router = createAppRouter(createMemoryHistory(), client, navigate)

  await router.push(path)

  return {router, navigate}
}

describe('forcing the new client version on navigation', () => {
  it('navigates inside the app as normal while the client is current', async () => {
    const {router, navigate} = await routerAt('/models')

    await router.push('/cases')

    expect(navigate).not.toHaveBeenCalled()
    expect(router.currentRoute.value.path).toBe('/cases')
  })

  it('does a full page load of the destination instead when a newer client exists', async () => {
    const {router, navigate} = await routerAt('/models')

    newVersionAvailable.value = true
    targetBuild.value = '2000'
    await router.push('/cases')

    expect(navigate).toHaveBeenCalledWith('/cases')
    expect(reloadAttempts('2000')).toBe(1)
    expect(router.currentRoute.value.path).toBe('/models')
  })

  it('stops forcing a reload once the attempts for that target are spent', async () => {
    const {router, navigate} = await routerAt('/models')

    newVersionAvailable.value = true
    targetBuild.value = '2000'

    for (let index = 0; index < 5; index += 1) noteReload('2000')

    await router.push('/cases')

    expect(navigate).not.toHaveBeenCalled()
    expect(router.currentRoute.value.path).toBe('/cases')
  })

  it('never reloads the very first navigation', async () => {
    newVersionAvailable.value = true

    const {navigate} = await routerAt('/models')

    expect(navigate).not.toHaveBeenCalled()
  })
})
