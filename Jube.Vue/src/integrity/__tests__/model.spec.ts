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

import { describe, expect, it } from 'vitest'
import {
  approvalKindOf,
  dependencyRows,
  engineModelState,
  engineRows,
  findingRows,
  pendingGroups,
  verdictOf,
  sortedChecks,
} from '../model'
import type { IntegrityCheck, IntegrityReport, ModelGraph, PendingRow } from '../types'

const check = (overrides: Partial<IntegrityCheck> = {}): IntegrityCheck => ({
  code: 'C1',
  title: 'Check',
  severity: 'Info',
  category: 'Rules',
  entityKind: null,
  entityId: null,
  entityName: null,
  message: 'm',
  ...overrides,
})

const report = (checks: IntegrityCheck[]): IntegrityReport => ({
  entityAnalysisModelId: 1,
  modelName: 'Payments',
  checkedDate: '2026-10-21T10:00:00Z',
  engineSource: 'in-process',
  errors: 0,
  warnings: 0,
  infos: 0,
  checks,
})

describe('findingRows', () => {
  it('groups checks by entity, counts severities and ranks the worst first', () => {
    const rows = findingRows(
      report([
        check({
          severity: 'Warning',
          entityKind: 'ActivationRule',
          entityId: 5,
          entityName: 'High value',
        }),
        check({
          severity: 'Error',
          entityKind: 'ActivationRule',
          entityId: 5,
          entityName: 'High value',
          category: 'Syntax',
        }),
        check({ severity: 'Info' }),
        check({ severity: 'Error', entityKind: 'TtlCounter', entityId: 9, entityName: 'Spend' }),
      ]),
    )

    expect(rows.map((row) => [row.kind, row.name, row.worst])).toEqual([
      ['ActivationRule', 'High value', 'Error'],
      ['TtlCounter', 'Spend', 'Error'],
      ['Model', 'Payments', 'Info'],
    ])
    expect(rows[0]).toMatchObject({ errors: 1, warnings: 1, infos: 0, categories: 'Rules, Syntax' })
  })

  it('names an engine check against a named instance as an engine instance', () => {
    expect(
      findingRows(report([check({ category: 'Engine', entityName: 'node-1' })]))[0]!.kind,
    ).toBe('Engine instance')
  })

  it('has no rows for no checks and sorts a groups own checks worst first', () => {
    expect(findingRows(report([]))).toEqual([])
    expect(
      sortedChecks([
        check({ severity: 'Info' }),
        check({ severity: 'Error' }),
        check({ severity: 'Warning' }),
      ]).map((item) => item.severity),
    ).toEqual(['Error', 'Warning', 'Info'])
  })
})

describe('engineRows', () => {
  it('joins nodes to the instance state, totals what is loaded and says if the model is started', () => {
    const rows = engineRows({
      source: 'in-process',
      nodes: [
        { instance: 'a', heartbeatDate: '2026-10-21T10:00:00Z', synchronisedDate: null },
        { instance: 'b', heartbeatDate: null, synchronisedDate: null },
      ],
      instances: [
        {
          instance: 'a',
          started: true,
          capturedDate: '2026-10-21T10:00:00Z',
          loaded: [
            { kind: 'Rule', count: 3, entities: [] },
            { kind: 'Counter', count: 2, entities: [] },
          ],
        },
        { instance: 'c', started: false, capturedDate: '2026-10-21T10:00:00Z', loaded: [] },
      ],
    })

    expect(rows.map((row) => [row.instance, row.loadedTotal, engineModelState(row)])).toEqual([
      ['a', 5, 'started'],
      ['b', 0, 'notLoaded'],
      ['c', 0, 'loaded'],
    ])
  })
})

describe('dependencyRows', () => {
  const graph: ModelGraph = {
    nodes: [
      { id: 'A', label: 'Rule', kind: 'ActivationRule', active: true, detail: null },
      { id: 'B', label: 'Counter', kind: 'TtlCounter', active: false, detail: null },
      { id: 'Missing:X', label: 'X', kind: 'Missing', active: false, detail: null },
    ],
    edges: [
      { from: 'A', to: 'B', label: 'Spend', dashed: false },
      { from: 'A', to: 'Missing:X', label: 'X', dashed: true },
    ],
    truncated: false,
    maxNodes: 200,
    focus: null,
  }

  it('lists what each entity uses and is used by, hides missing names as rows and puts them first', () => {
    const rows = dependencyRows(graph)

    expect(rows.map((row) => row.id)).toEqual(['A', 'B'])
    expect(rows[0]).toMatchObject({ usesCount: 2, usedByCount: 0, missingCount: 1 })
    expect(rows[0]!.uses[0]).toEqual({
      entity: 'TtlCounter Counter',
      through: 'Spend',
      how: 'ruleText',
      missing: false,
    })
    expect(rows[0]!.uses[1]).toEqual({
      entity: 'X (does not exist)',
      through: 'X',
      how: 'setting',
      missing: true,
    })
    expect(rows[1]!.usedBy[0]!.entity).toBe('ActivationRule Rule')
  })
})

describe('pendingGroups', () => {
  const row = (overrides: Partial<PendingRow>): PendingRow => ({
    kind: 16,
    entityId: 1,
    name: 'n',
    version: 2,
    deleted: false,
    makerUser: 'maker',
    parentId: null,
    ...overrides,
  })

  it('groups by kind and counts deletions', () => {
    const groups = pendingGroups([
      row({ entityId: 1 }),
      row({ entityId: 2, deleted: true }),
      row({ kind: 6, entityId: 3 }),
    ])

    expect(groups.map((group) => [group.kindName, group.count, group.deletions])).toEqual([
      ['Activation rule', 2, 1],
      ['Request XPath', 1, 0],
    ])
    expect(groups[0]!.entities[1]!.change).toBe('deletion')
  })

  it('folds list values under their list, counting them in the waiting total', () => {
    const groups = pendingGroups([
      row({ kind: 2, entityId: 10, name: 'Watch list' }),
      row({ kind: 3, entityId: 100, parentId: 10, name: 'v1' }),
      row({ kind: 3, entityId: 101, parentId: 10, name: 'v2' }),
      row({ kind: 3, entityId: 102, parentId: 99, name: 'orphan' }),
    ])

    expect(groups.map((group) => [group.kindName, group.count])).toEqual([
      ['List', 3],
      ['List value', 1],
    ])
    expect(groups[0]!.entities[0]!.valueCount).toBe(2)
  })

  it('knows the endpoint and history route of each approvable kind', () => {
    expect(approvalKindOf(16)).toMatchObject({
      endpoint: '/api/EntityAnalysisModelActivationRule',
      historyRoute: 'EntityAnalysisModelActivationRuleVersionHistory',
    })
    expect(approvalKindOf(3)?.endpoint).toBeNull()
    expect(approvalKindOf(99)).toBeUndefined()
  })
})

describe('verdictOf', () => {
  it.each([
    [{ errors: 2, warnings: 0 }, 'unsound'],
    [{ errors: 1, warnings: 5 }, 'unsound'],
    [{ errors: 0, warnings: 3 }, 'warnings'],
    [{ errors: 0, warnings: 0 }, 'sound'],
  ] as const)('%j is %s', (report, verdict) => {
    expect(verdictOf(report)).toBe(verdict)
  })
})
