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
import {effectScope} from 'vue'
import {useJoobyContextStore} from '../joobyContext'
import {useJoobyPageContext} from '../../composables/useJoobyPageContext'

const route = (routeName: string, path = `/${routeName}`) => ({routeName, path})

describe('joobyContext', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('falls back to the route when a page supplies nothing', () => {
    const store = useJoobyContextStore()

    store.setRoute({...route('adminTenants'), query: {tab: 'a'}}, true)

    const sent = JSON.parse(store.serialised())

    expect(sent.pageName).toBe('adminTenants')
    expect(sent.location.query).toEqual({tab: 'a'})
  })

  it('drops the previous page overlay when the route name changes', () => {
    const store = useJoobyContextStore()

    store.setRoute(route('cases'), true)
    store.set({pageName: 'Cases', state: 'list'})
    store.setRoute(route('adminTenants'), true)

    expect(JSON.parse(store.serialised()).pageName).toBe('adminTenants')
  })

  it('keeps the overlay when only the query string changes', () => {
    const store = useJoobyContextStore()

    store.setRoute(route('cases'), true)
    store.set({pageName: 'Cases', state: 'list'})
    store.setRoute({...route('cases'), query: {tab: 'health'}}, false)

    const sent = JSON.parse(store.serialised())

    expect(sent.pageName).toBe('Cases')
    expect(sent.location.query).toEqual({tab: 'health'})
  })

  it('clears the overlay when the owning component unmounts', () => {
    const store = useJoobyContextStore()
    const scope = effectScope()

    store.setRoute(route('models'), true)
    scope.run(() => useJoobyPageContext(() => ({pageName: 'Model', state: 'edit'})))
    expect(store.context.pageName).toBe('Model')

    scope.stop()
    expect(store.context.pageName).toBe('models')
  })
})
