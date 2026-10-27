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

import type { MapMode } from './types'

export interface WatcherView {
  live: boolean
  mode: MapMode
  window: number
  fade: number
  trails: boolean
  heat: boolean
  from: string
  to: string
}

export interface WatcherPageState extends WatcherView {
  key: string
  keyValue: string
}

export const defaultView: WatcherView = {
  live: true,
  mode: 'latest',
  window: 0,
  fade: 15,
  trails: true,
  heat: false,
  from: '',
  to: '',
}

export const defaultPageState: WatcherPageState = { ...defaultView, key: '', keyValue: '' }

const windows = [0, 1, 5, 15, 60]
const localDateTime = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/

type Query = Record<string, unknown>

function text(query: Query, name: string): string {
  const value = query[name]

  return typeof value === 'string' ? value : ''
}

function choice(query: Query, name: string, fallback: number): number {
  const value = Number(text(query, name))

  return windows.includes(value) && text(query, name) !== '' ? value : fallback
}

function flag(query: Query, name: string, fallback: boolean): boolean {
  const value = text(query, name)

  return value === '1' ? true : value === '0' ? false : fallback
}

function moment(query: Query, name: string): string {
  const value = text(query, name)

  return localDateTime.test(value) && !Number.isNaN(new Date(value).getTime()) ? value : ''
}

export function parsePageState(query: Query): WatcherPageState {
  const from = moment(query, 'from')
  const to = moment(query, 'to')
  const range = from !== '' && to !== ''

  return {
    key: text(query, 'key').trim(),
    keyValue: text(query, 'value').trim(),
    live: flag(query, 'live', defaultView.live),
    mode: text(query, 'mode') === 'all' ? 'all' : 'latest',
    window: choice(query, 'window', defaultView.window),
    fade: choice(query, 'fade', defaultView.fade),
    trails: flag(query, 'trails', defaultView.trails),
    heat: flag(query, 'heat', defaultView.heat),
    from: range ? from : '',
    to: range ? to : '',
  }
}

export function serialisePageState(state: WatcherPageState): Record<string, string> {
  const query: Record<string, string> = {}

  if (state.key) query.key = state.key
  if (state.keyValue) query.value = state.keyValue
  if (state.live !== defaultView.live) query.live = state.live ? '1' : '0'
  if (state.mode !== defaultView.mode) query.mode = state.mode
  if (state.window !== defaultView.window) query.window = String(state.window)
  if (state.fade !== defaultView.fade) query.fade = String(state.fade)
  if (state.trails !== defaultView.trails) query.trails = state.trails ? '1' : '0'
  if (state.heat !== defaultView.heat) query.heat = state.heat ? '1' : '0'

  if (state.from && state.to) {
    query.from = state.from
    query.to = state.to
  }

  return query
}

export function sameState(a: WatcherPageState, b: WatcherPageState): boolean {
  return JSON.stringify(serialisePageState(a)) === JSON.stringify(serialisePageState(b))
}
