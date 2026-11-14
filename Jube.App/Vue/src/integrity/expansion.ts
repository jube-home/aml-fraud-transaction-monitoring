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

import type {ModelGraph} from './types'

export const clusterPrefix = 'Cluster:'

export const noneMarker = '-'

export interface DependencyKind {
  kind: string
  count: number
  flagged: number
  open: boolean
}

export function decodeExpanded(value: string | null | undefined): string[] | null {
  if (value === null || value === undefined) return null
  if (value === noneMarker || value === '') return []

  return value.split(',').filter((part) => part.length > 0)
}

export function encodeExpanded(kinds: string[]): string {
  return kinds.length === 0 ? noneMarker : kinds.join(',')
}

export function dependencyKinds(
  graph: ModelGraph | undefined,
  expanded: string[] | null,
): DependencyKind[] {
  if (!graph) return []

  const linked = new Set(graph.edges.flatMap((edge) => [edge.from, edge.to]))
  const connected = graph.nodes.filter((node) => linked.has(node.id) && node.kind !== 'Missing')
  const automatic = linked.size <= graph.collapseAbove
  const counts = new Map<string, DependencyKind>()

  for (const node of connected) {
    const entry = counts.get(node.kind) ?? {
      kind: node.kind,
      count: 0,
      flagged: 0,
      open: expanded === null ? automatic : expanded.includes(node.kind),
    }

    entry.count += 1
    if (node.errors > 0 || node.warnings > 0) entry.flagged += 1
    counts.set(node.kind, entry)
  }

  return [...counts.values()].sort((a, b) => a.kind.localeCompare(b.kind))
}

export function toggleKind(kinds: DependencyKind[], kind: string): string[] {
  const open = new Set(kinds.filter((entry) => entry.open).map((entry) => entry.kind))

  if (open.has(kind)) open.delete(kind)
  else open.add(kind)

  return [...open].sort()
}

export function clusterKind(id: string | null): string | null {
  return id !== null && id.startsWith(clusterPrefix) ? id.slice(clusterPrefix.length) : null
}
