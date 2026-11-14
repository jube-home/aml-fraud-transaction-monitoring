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

import type {LocationQueryRaw} from 'vue-router'
import type {GraphLayoutId} from './types'

export type GraphView = 'relevance' | 'hops' | 'flow'

export type GraphHops = 1 | 2 | 3

export type FlowDirection = 'forward' | 'backward'

export interface GraphFlow {
  seed: string
  direction: FlowDirection
}

export interface GraphAddress {
  view: GraphView
  hops: GraphHops
  layout: GraphLayoutId | null
  edges: string[]
  expanded: string[]
  selected: string | null
  flow: GraphFlow | null
  pattern: string | null
  registry: string
  seedKey: string
  seedValue: string
  cursor: number | null
  explode: boolean
  flowWindowHours: number | null
  flowTolerance: number | null
  flowArchiveId: number | null
  pathsTo: string | null
  community: string | null
}

export const graphAddressKeys = [
  'gv',
  'gh',
  'gl',
  'ge',
  'gx',
  'gn',
  'gf',
  'gp',
  'gr',
  'gk',
  'gq',
  'gt',
  'gd',
  'gw',
  'go',
  'ga',
  'gy',
  'gc',
] as const

export const maximumExpanded = 20

export const maximumEdgeKinds = 32

export const maximumFlowWindowHours = 720

export const maximumFlowTolerance = 0.9

export const defaultGraphAddress: GraphAddress = {
  view: 'hops',
  hops: 2,
  layout: null,
  edges: [],
  expanded: [],
  selected: null,
  flow: null,
  pattern: null,
  registry: '',
  seedKey: '',
  seedValue: '',
  cursor: null,
  explode: false,
  flowWindowHours: null,
  flowTolerance: null,
  flowArchiveId: null,
  pathsTo: null,
  community: null,
}

type Query = Record<string, unknown>

function first(value: unknown): string | undefined {
  const single = Array.isArray(value) ? value[0] : value

  return typeof single === 'string' ? single : undefined
}

function list(text: string | undefined, limit: number): string[] {
  if (!text) return []

  const seen = new Set<string>()

  for (const part of text.split(',')) {
    const trimmed = part.trim()

    if (trimmed !== '' && !seen.has(trimmed)) seen.add(trimmed)
    if (seen.size >= limit) break
  }

  return [...seen]
}

function hopsOf(text: string | undefined, fallback: GraphHops): GraphHops {
  const value = Number(text)

  return value === 1 || value === 2 || value === 3 ? value : fallback
}

function layoutOf(text: string | undefined): GraphLayoutId | null {
  const value = Number(text)

  return value === 1 || value === 2 || value === 3 || value === 4 ? value : null
}

function flowOf(text: string | undefined): GraphFlow | null {
  if (!text) return null

  const at = text.lastIndexOf(':')
  const seed = at < 0 ? text : text.slice(0, at)
  const direction = at < 0 ? 'f' : text.slice(at + 1)

  if (seed === '') return null

  return {seed, direction: direction === 'b' ? 'backward' : 'forward'}
}

function cursorOf(text: string | undefined): number | null {
  const value = Number(text)

  return text !== undefined && text !== '' && Number.isSafeInteger(value) && value > 0
    ? value
    : null
}

function hoursOf(text: string | undefined): number | null {
  const value = Number(text)

  return text !== undefined &&
  text !== '' &&
  Number.isInteger(value) &&
  value >= 1 &&
  value <= maximumFlowWindowHours
    ? value
    : null
}

function toleranceOf(text: string | undefined): number | null {
  const value = Number(text)

  return text !== undefined &&
  text !== '' &&
  Number.isFinite(value) &&
  value >= 0 &&
  value <= maximumFlowTolerance
    ? value
    : null
}

function nonEmpty(text: string | undefined): string | null {
  return text !== undefined && text !== '' ? text : null
}

function viewOf(text: string | undefined): GraphView {
  if (text === 'r') return 'relevance'
  if (text === 'f') return 'flow'

  return 'hops'
}

export function parseGraphAddress(query: Query): GraphAddress {
  return {
    view: viewOf(first(query.gv)),
    hops: hopsOf(first(query.gh), defaultGraphAddress.hops),
    layout: layoutOf(first(query.gl)),
    edges: list(first(query.ge), maximumEdgeKinds),
    expanded: list(first(query.gx), maximumExpanded),
    selected: nonEmpty(first(query.gn)),
    flow: flowOf(first(query.gf)),
    pattern: nonEmpty(first(query.gp)),
    registry: first(query.gr) ?? '',
    seedKey: first(query.gk) ?? '',
    seedValue: first(query.gq) ?? '',
    cursor: cursorOf(first(query.gt)),
    explode: first(query.gd) === '1',
    flowWindowHours: hoursOf(first(query.gw)),
    flowTolerance: toleranceOf(first(query.go)),
    flowArchiveId: cursorOf(first(query.ga)),
    pathsTo: nonEmpty(first(query.gy)),
    community: nonEmpty(first(query.gc)),
  }
}

export function serialiseGraphAddress(address: GraphAddress): Record<string, string> {
  const query: Record<string, string> = {}

  if (address.view === 'relevance') query.gv = 'r'
  if (address.view === 'flow') query.gv = 'f'
  if (address.hops !== defaultGraphAddress.hops) query.gh = String(address.hops)
  if (address.layout !== null) query.gl = String(address.layout)
  if (address.edges.length > 0) query.ge = address.edges.join(',')
  if (address.expanded.length > 0) query.gx = address.expanded.join(',')
  if (address.selected !== null) query.gn = address.selected
  if (address.flow) {
    query.gf = `${address.flow.seed}:${address.flow.direction === 'backward' ? 'b' : 'f'}`
  }
  if (address.pattern !== null) query.gp = address.pattern
  if (address.registry !== '') query.gr = address.registry
  if (address.seedKey !== '') query.gk = address.seedKey
  if (address.seedValue !== '') query.gq = address.seedValue
  if (address.cursor !== null) query.gt = String(address.cursor)
  if (address.explode) query.gd = '1'
  if (address.flowWindowHours !== null) query.gw = String(address.flowWindowHours)
  if (address.flowTolerance !== null) query.go = String(address.flowTolerance)
  if (address.flowArchiveId !== null) query.ga = String(address.flowArchiveId)
  if (address.pathsTo !== null) query.gy = address.pathsTo
  if (address.community !== null) query.gc = address.community

  return query
}

export function mergeGraphAddress(query: Query, address: GraphAddress): LocationQueryRaw {
  const kept = Object.fromEntries(
    Object.entries(query).filter(
      ([name]) => !(graphAddressKeys as readonly string[]).includes(name),
    ),
  )

  return {...kept, ...serialiseGraphAddress(address)} as LocationQueryRaw
}

export function graphAddressText(address: GraphAddress): string {
  return new URLSearchParams(serialiseGraphAddress(address)).toString()
}

export function parseGraphAddressText(text: string): GraphAddress {
  const query: Query = {}

  for (const [name, value] of new URLSearchParams(text.replace(/^\?/, ''))) query[name] = value

  return parseGraphAddress(query)
}
