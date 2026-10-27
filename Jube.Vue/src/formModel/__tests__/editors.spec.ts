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
  caseFilterChanges,
  caseFilterInitial,
  claimedNames,
  datasetChanges,
  datasetLocked,
  datasetReference,
  datasetShown,
  editorOf,
  exhaustiveFilterChanges,
  exhaustiveFilterInitial,
  exhaustiveSource,
  firstClaimed,
  ruleEditorChanges,
  ruleEditorInitial,
  datasourceChanges,
  datasourceInitial,
} from '../editors'
import type { FormEditor, FormModel } from '../types'

const full: FormEditor = {
  kind: 'ruleBuilder',
  parseType: 2,
  roles: {
    scriptType: 'ruleScriptTypeId',
    builder: 'builderRuleScript',
    builderJson: 'json',
    coder: 'coderRuleScript',
  },
  reads: {},
}
const coderOnly: FormEditor = {
  kind: 'ruleBuilder',
  parseType: 4,
  roles: { coder: 'functionScript' },
  reads: {},
}
const filter: FormEditor = {
  kind: 'caseFilter',
  parseType: null,
  roles: { select: 'selectJson', filter: 'filterJson' },
  reads: {},
}

const model = (editors: FormEditor[], names: string[]): FormModel =>
  ({ editors, fields: names.map((name) => ({ name })) }) as unknown as FormModel

describe('editor claims', () => {
  it('names every property an editor stands in for, and finds the editor of a property', () => {
    const subject = model([full, filter], ['name', 'json', 'selectJson'])

    expect(claimedNames(subject)).toEqual(
      new Set([
        'ruleScriptTypeId',
        'builderRuleScript',
        'json',
        'coderRuleScript',
        'selectJson',
        'filterJson',
      ]),
    )
    expect(editorOf(subject, 'json')).toBe(full)
    expect(editorOf(subject, 'name')).toBeUndefined()
  })

  it('finds the first claimed field in the model order, where the component is drawn', () => {
    expect(firstClaimed(full, model([full], ['name', 'json', 'builderRuleScript']))).toBe('json')
    expect(firstClaimed(coderOnly, model([coderOnly], ['name']))).toBeUndefined()
  })
})

describe('rule editor values', () => {
  it('gives nothing to start from for a new record', () => {
    expect(ruleEditorInitial(full, {})).toBeUndefined()
    expect(
      ruleEditorInitial(full, { coderRuleScript: '', builderRuleScript: '', json: '' }),
    ).toBeUndefined()
  })

  it('reads the four stored properties into the component value', () => {
    expect(
      ruleEditorInitial(full, {
        ruleScriptTypeId: 2,
        builderRuleScript: 'If a Then',
        json: '{"rules":[]}',
        coderRuleScript: 'Return True',
      }),
    ).toEqual({
      ruleTextBuilder: 'If a Then',
      ruleJsonBuilder: '{"rules":[]}',
      ruleTextCoder: 'Return True',
      ruleType: 2,
    })
  })

  it('ignores a script type that is neither builder nor coder', () => {
    expect(
      ruleEditorInitial(full, { ruleScriptTypeId: 9, coderRuleScript: 'x' })!.ruleType,
    ).toBeUndefined()
  })

  it('reads a coder-only record from its one property', () => {
    expect(ruleEditorInitial(coderOnly, { functionScript: 'Return 1' })).toEqual({
      ruleTextBuilder: '',
      ruleJsonBuilder: '',
      ruleTextCoder: 'Return 1',
      ruleType: undefined,
    })
  })

  it('writes the component value back to the named properties, turning builder rules into JSON text', () => {
    expect(
      ruleEditorChanges(full, {
        ruleType: 1,
        ruleTextBuilder: 'b',
        ruleJsonBuilder: { condition: 'AND', rules: [] },
        ruleTextCoder: 'c',
      }),
    ).toEqual([
      ['coderRuleScript', 'c'],
      ['builderRuleScript', 'b'],
      ['json', '{"condition":"AND","rules":[]}'],
      ['ruleScriptTypeId', 1],
    ])
  })

  it('writes only the coder for a coder-only editor and leaves the script type when none is reported', () => {
    expect(ruleEditorChanges(coderOnly, { ruleTextCoder: 'x', ruleTextBuilder: 'b' })).toEqual([
      ['functionScript', 'x'],
    ])
    expect(ruleEditorChanges(full, { ruleTextCoder: 'x' }).map(([name]) => name)).not.toContain(
      'ruleScriptTypeId',
    )
  })
})

describe('case filter values', () => {
  it('starts from nothing when both are empty, else from the two stored texts', () => {
    expect(caseFilterInitial(filter, {})).toBeUndefined()
    expect(caseFilterInitial(filter, { selectJson: 's', filterJson: 'f' })).toEqual({
      selectJson: 's',
      filterJson: 'f',
    })
  })

  it('writes the two texts to their properties', () => {
    expect(caseFilterChanges(filter, { selectJson: 's', filterJson: 'f' })).toEqual([
      ['selectJson', 's'],
      ['filterJson', 'f'],
    ])
  })
})

const dataset: FormEditor = {
  kind: 'exhaustiveDataset',
  parseType: null,
  roles: { csv: 'exhaustiveInstanceCsvId', json: 'exhaustiveInstanceJsonId' },
  reads: { source: 'dataSourceId', status: 'statusId' },
}

const exhaustiveFilter: FormEditor = {
  kind: 'exhaustiveFilter',
  parseType: null,
  roles: { filterJson: 'filterJson', filterSql: 'filterSql', filterTokens: 'filterTokens' },
  reads: {
    source: 'dataSourceId',
    csv: 'exhaustiveInstanceCsvId',
    json: 'exhaustiveInstanceJsonId',
  },
}

describe('exhaustive dataset editor', () => {
  it('reads the chosen dataset as a picker reference from whichever id is set', () => {
    expect(datasetReference(dataset, {})).toBe('')
    expect(datasetReference(dataset, { exhaustiveInstanceCsvId: 12 })).toBe('Csv:12')
    expect(datasetReference(dataset, { exhaustiveInstanceJsonId: 7 })).toBe('Json:7')
    expect(
      datasetReference(dataset, { exhaustiveInstanceCsvId: null, exhaustiveInstanceJsonId: 0 }),
    ).toBe('')
  })

  it('writes the id to its own store, clears the other and switches the source to uploaded', () => {
    expect(datasetChanges(dataset, 'Json:7')).toEqual([
      ['exhaustiveInstanceCsvId', null],
      ['exhaustiveInstanceJsonId', 7],
      ['dataSourceId', 2],
    ])
    expect(datasetChanges(dataset, '')).toEqual([
      ['exhaustiveInstanceCsvId', null],
      ['exhaustiveInstanceJsonId', null],
    ])
  })

  it('shows only for the uploaded source', () => {
    expect(datasetShown(dataset, { dataSourceId: 2 })).toBe(true)
    expect(datasetShown(dataset, { dataSourceId: 0 })).toBe(false)
    expect(datasetShown(dataset, {})).toBe(false)
  })

  it.each([
    [undefined, false],
    [0, false],
    [20, false],
    [9, true],
    [16, true],
    [19, true],
  ])('is locked for status %s: %s', (status, locked) => {
    expect(datasetLocked(dataset, { statusId: status })).toBe(locked)
  })
})

describe('exhaustive filter editor', () => {
  it('draws from the model unless an uploaded dataset is chosen', () => {
    expect(exhaustiveSource(exhaustiveFilter, { dataSourceId: 0 }, 5)).toEqual({
      kind: 'model',
      modelId: 5,
    })
    expect(
      exhaustiveSource(exhaustiveFilter, { dataSourceId: 2, exhaustiveInstanceJsonId: 7 }, 5),
    ).toEqual({ kind: 'dataset', datasetKind: 'Json', id: 7 })
    expect(exhaustiveSource(exhaustiveFilter, { dataSourceId: 2 }, 5)).toEqual({
      kind: 'model',
      modelId: 5,
    })
  })

  it('starts from the stored filter json, or nothing', () => {
    expect(exhaustiveFilterInitial(exhaustiveFilter, {})).toBeUndefined()
    expect(exhaustiveFilterInitial(exhaustiveFilter, { filterJson: '{"a":1}' })).toEqual({
      filterJson: '{"a":1}',
    })
  })

  it('writes the json, the sql and the tokens to their properties', () => {
    expect(
      exhaustiveFilterChanges(exhaustiveFilter, {
        filterJson: 'j',
        filterSql: 's',
        filterTokens: 't',
        valid: true,
      }),
    ).toEqual([
      ['filterJson', 'j'],
      ['filterSql', 's'],
      ['filterTokens', 't'],
    ])
  })
})

const datasource: FormEditor = {
  kind: 'datasource',
  parseType: null,
  roles: {
    command: 'command',
    displayKind: 'displayKindId',
    grid: 'includeGrid',
    template: 'templateText',
    mapping: 'mapping',
  },
  reads: {},
}

describe('datasource editor', () => {
  const values = {
    command: 'select 1',
    displayKindId: 2,
    includeGrid: true,
    templateText: null,
    mapping: [{ roleId: 1, columns: ['a'] }],
  }

  it('reads the five properties into one value', () => {
    expect(datasourceInitial(datasource, values)).toEqual({
      command: 'select 1',
      displayKindId: 2,
      includeGrid: true,
      mapping: [{ roleId: 1, columns: ['a'] }],
      templateText: '',
      valid: false,
    })
  })

  it('treats a missing kind and mapping as none', () => {
    const initial = datasourceInitial(datasource, { command: 'x' })

    expect(initial.displayKindId).toBe(0)
    expect(initial.mapping).toEqual([])
    expect(initial.includeGrid).toBe(false)
  })

  it('reports nothing for a value equal to the record, however it was stored', () => {
    const value = { ...datasourceInitial(datasource, values), valid: true }

    expect(datasourceChanges(datasource, value, values)).toEqual([])
    expect(
      datasourceChanges(
        datasource,
        datasourceInitial(datasource, { command: '', displayKindId: null }),
        { command: '', displayKindId: null, templateText: null },
      ),
    ).toEqual([])
  })

  it('reports only the properties that differ', () => {
    const value = {
      ...datasourceInitial(datasource, values),
      command: 'select 2',
      mapping: [],
      valid: true,
    }

    expect(datasourceChanges(datasource, value, values)).toEqual([
      ['command', 'select 2'],
      ['mapping', []],
    ])
  })
})
