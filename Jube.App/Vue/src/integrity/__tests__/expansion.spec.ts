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

import {describe, expect, it} from 'vitest'
import {
  clusterKind,
  decodeExpanded,
  dependencyKinds,
  encodeExpanded,
  toggleKind,
} from '../expansion'
import type {ModelGraph} from '../types'

function graph(rules: number, collapseAbove = 40): ModelGraph {
  const nodes = [
    ...Array.from({length: rules}, (_, i) => ({
      id: `ActivationRule:${i}`,
      label: `Rule ${i}`,
      kind: 'ActivationRule',
      active: true,
      detail: null,
      errors: 0,
      warnings: i === 0 ? 1 : 0,
    })),
    {
      id: 'TtlCounter:1',
      label: 'Spend',
      kind: 'TtlCounter',
      active: true,
      detail: null,
      errors: 0,
      warnings: 0,
    },
    {
      id: 'Missing:Gone',
      label: 'Gone',
      kind: 'Missing',
      active: false,
      detail: null,
      errors: 0,
      warnings: 0,
    },
    {
      id: 'List:9',
      label: 'Loose',
      kind: 'List',
      active: true,
      detail: null,
      errors: 0,
      warnings: 0,
    },
  ]
  const edges = [
    ...Array.from({length: rules}, (_, i) => ({
      from: `ActivationRule:${i}`,
      to: 'TtlCounter:1',
      label: 'x',
      dashed: false,
    })),
    {from: 'ActivationRule:0', to: 'Missing:Gone', label: 'y', dashed: false},
  ]

  return {nodes, edges, truncated: false, maxNodes: 150, focus: null, collapseAbove}
}

describe('dependency expansion', () => {
  it('encodes none as a marker so an empty choice survives the address bar', () => {
    expect(encodeExpanded([])).toBe('-')
    expect(decodeExpanded('-')).toEqual([])
    expect(decodeExpanded(encodeExpanded(['A', 'B']))).toEqual(['A', 'B'])
  })

  it('treats a missing parameter as automatic', () => {
    expect(decodeExpanded(null)).toBeNull()
    expect(decodeExpanded(undefined)).toBeNull()
  })

  it('counts connected entities per kind and leaves out the missing and unconnected ones', () => {
    const kinds = dependencyKinds(graph(5), null)

    expect(kinds.map((entry) => entry.kind)).toEqual(['ActivationRule', 'TtlCounter'])
    expect(kinds[0]).toMatchObject({count: 5, flagged: 1})
  })

  it('opens every kind automatically on a small model and none on a large one', () => {
    expect(dependencyKinds(graph(5), null).every((entry) => entry.open)).toBe(true)
    expect(dependencyKinds(graph(60), null).some((entry) => entry.open)).toBe(false)
  })

  it('follows an explicit list over the automatic rule', () => {
    const kinds = dependencyKinds(graph(60), ['TtlCounter'])

    expect(kinds.find((entry) => entry.kind === 'TtlCounter')?.open).toBe(true)
    expect(kinds.find((entry) => entry.kind === 'ActivationRule')?.open).toBe(false)
  })

  it('toggles from the effective state so the first click on a small model collapses that kind only', () => {
    const kinds = dependencyKinds(graph(5), null)

    expect(toggleKind(kinds, 'ActivationRule')).toEqual(['TtlCounter'])
    expect(toggleKind(dependencyKinds(graph(60), null), 'TtlCounter')).toEqual(['TtlCounter'])
  })

  it('recognises a grouped box and ignores an ordinary entity', () => {
    expect(clusterKind('Cluster:TtlCounter')).toBe('TtlCounter')
    expect(clusterKind('TtlCounter:1')).toBeNull()
    expect(clusterKind(null)).toBeNull()
  })
})
