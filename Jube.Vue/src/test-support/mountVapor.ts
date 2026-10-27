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

import { createVaporApp, shallowReactive, vaporInteropPlugin, type App, type Plugin } from 'vue'

type Handler = (...args: unknown[]) => void

export interface MountedVapor {
  app: App
  root: HTMLElement
  emitted: Record<string, unknown[][]>
  setProps: (next: Record<string, unknown>) => void
  unmount: () => void
}

export interface MountVaporOptions {
  props?: Record<string, unknown>
  events?: string[]
  plugins?: (Plugin | [Plugin, ...unknown[]])[]
}

export function mountVapor(component: object, options: MountVaporOptions = {}): MountedVapor {
  const state = shallowReactive<Record<string, unknown>>({ ...options.props })
  const emitted: Record<string, unknown[][]> = {}
  const rawProps: Record<string, () => unknown> = {}

  for (const key of Object.keys(state)) rawProps[key] = () => state[key]

  for (const event of options.events ?? []) {
    const handler: Handler = (...args) => {
      ;(emitted[event] ??= []).push(args)
    }

    rawProps[`on${event[0]!.toUpperCase()}${event.slice(1)}`] = () => handler
  }

  const root = document.createElement('div')
  document.body.append(root)

  const app = createVaporApp(component as never, rawProps as never)
  app.use(vaporInteropPlugin)

  for (const plugin of options.plugins ?? []) {
    if (Array.isArray(plugin)) app.use(plugin[0] as never, ...(plugin.slice(1) as never[]))
    else app.use(plugin as never)
  }

  app.mount(root)

  return {
    app,
    root,
    emitted,
    setProps(next) {
      for (const [key, value] of Object.entries(next)) {
        if (!(key in rawProps)) rawProps[key] = () => state[key]
        state[key] = value
      }
    },
    unmount() {
      app.unmount()
      root.remove()
    },
  }
}
