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
import {optionLabel, recordFactGroups} from '../recordFacts'
import type {FormField, FormModel, RecordValues} from '../types'

const format = {yes: 'Yes', no: 'No', dateTime: (value: string) => `at ${value}`}

function recordFacts(form: FormModel, row: RecordValues, shape: typeof format, shown?: string[]) {
  return recordFactGroups(form, row, shape, shown).flatMap((group) =>
    group.sections.flatMap((section) => section.facts),
  )
}

function field(name: string, extra: Partial<FormField> = {}): FormField {
  return {
    name,
    label: name.toUpperCase(),
    type: 'string',
    options: [],
    group: 'Identity',
    order: 10,
    visibleWhen: [],
    requiredWhen: [],
    enabledWhen: [],
    ...extra,
  } as FormField
}

function model(fields: FormField[], listed: string[] = []): FormModel {
  return {
    fields,
    groups: [
      {path: 'Identity', label: 'Identity', order: 10, collapsed: false, widget: ''},
      {path: 'Audit', label: 'Audit', order: 90, collapsed: true, widget: ''},
    ],
    listColumns: listed.map((name, index) => ({field: name, title: name, order: index})),
  } as unknown as FormModel
}

describe('recordFacts', () => {
  it('leaves out the fields the list already shows as columns', () => {
    const facts = recordFacts(
      model([field('name'), field('note')], ['name']),
      {name: 'alpha', note: 'hello'},
      format,
    )

    expect(facts).toEqual([{label: 'NOTE', value: 'hello', mono: false}])
  })

  it('leaves out the fields the caller says its grid already shows', () => {
    const facts = recordFacts(
      model([field('name'), field('note')], ['note']),
      {name: 'alpha', note: 'hello'},
      format,
      ['name'],
    )

    expect(facts).toEqual([{label: 'NOTE', value: 'hello', mono: false}])
  })

  it('leaves out empty values, json fields and values that are not scalar', () => {
    const facts = recordFacts(
      model([
        field('blank'),
        field('missing'),
        field('none'),
        field('payload', {type: 'json'}),
        field('list'),
      ]),
      {blank: '', none: null, payload: '{}', list: [1, 2]},
      format,
    )

    expect(facts).toEqual([])
  })

  it('formats switches, dates, numbers and identifiers', () => {
    const facts = recordFacts(
      model([
        field('active', {type: 'boolean', order: 10}),
        field('locked', {type: 'boolean', order: 20}),
        field('created', {type: 'datetime', order: 30}),
        field('version', {type: 'integer', order: 40}),
        field('guid', {type: 'guid', order: 50}),
      ]),
      {active: true, locked: false, created: '2026-11-12', version: 3, guid: 'abc'},
      format,
    )

    expect(facts).toEqual([
      {label: 'ACTIVE', value: 'Yes', mono: false, state: 'on'},
      {label: 'LOCKED', value: 'No', mono: false, state: 'off'},
      {label: 'CREATED', value: 'at 2026-11-12', mono: false},
      {label: 'VERSION', value: '3', mono: true},
      {label: 'GUID', value: 'abc', mono: true},
    ])
  })

  it('shows the label of a chosen option rather than its stored value', () => {
    const facts = recordFacts(
      model([field('kind', {type: 'integer', options: [{value: 2, label: 'Monthly'}]})]),
      {kind: 2},
      format,
    )

    expect(facts[0]?.value).toBe('Monthly')
  })

  it('orders by group then by field order', () => {
    const facts = recordFacts(
      model([
        field('createdUser', {group: 'Audit', order: 10}),
        field('second', {order: 20}),
        field('first', {order: 10}),
      ]),
      {createdUser: 'maker', second: 'b', first: 'a'},
      format,
    )

    expect(facts.map((fact) => fact.label)).toEqual(['FIRST', 'SECOND', 'CREATEDUSER'])
  })

  it('leaves out a value that is nothing but control characters', () => {
    const facts = recordFacts(model([field('interval')]), {interval: '\u0000'}, format)

    expect(facts).toEqual([])
  })

  it('shortens a long text', () => {
    const facts = recordFacts(model([field('note')]), {note: 'x'.repeat(300)}, format)

    expect(facts[0]?.value).toHaveLength(241)
    expect(facts[0]?.value.endsWith('…')).toBe(true)
  })

  it('groups the facts under the groups the form declares, ungrouped first', () => {
    const groups = recordFactGroups(
      model([
        field('createdUser', {group: 'Audit'}),
        field('name'),
        field('loose', {group: ''}),
        field('hidden', {group: 'Audit'}),
      ]),
      {createdUser: 'maker', name: 'alpha', loose: 'x'},
      format,
    )

    expect(groups.map((group) => [group.path, group.label])).toEqual([
      ['', ''],
      ['Identity', 'Identity'],
      ['Audit', 'Audit'],
    ])
  })

  it('keeps a nested group as a labelled section of its top group', () => {
    const form = model([
      field('retries', {group: 'Identity/Retry/Backoff'}),
      field('name', {order: 20}),
    ])

    form.groups.push({
      path: 'Identity/Retry',
      label: 'Retry policy',
      order: 20,
      collapsed: false,
      widget: '',
    })

    const groups = recordFactGroups(form, {retries: 3, name: 'alpha'}, format)

    expect(groups).toHaveLength(1)
    expect(groups[0]?.sections.map((section) => [section.label, section.facts[0]?.label])).toEqual([
      ['', 'NAME'],
      ['Retry policy / Backoff', 'RETRIES'],
    ])
  })

  const gated = (name: string, by: string) =>
    field(name, {
      group: 'Cache',
      order: 20,
      visibleWhen: [{property: by, op: 'equals', values: [true], matchMode: 'all'}],
    })

  const cache = () =>
    model([
      field('enableCache', {type: 'boolean', group: 'Cache'}),
      gated('cacheLimit', 'enableCache'),
    ])

  it('lifts the switch of a switched group out of its facts when it is on', () => {
    const groups = recordFactGroups(cache(), {enableCache: true, cacheLimit: 100}, format)

    expect(groups[0]?.feature).toBe('on')
    expect(groups[0]?.sections[0]?.facts.map((fact) => fact.label)).toEqual(['CACHELIMIT'])
  })

  it('keeps a switched-off group with nothing under it', () => {
    const groups = recordFactGroups(cache(), {enableCache: false, cacheLimit: 100}, format)

    expect(groups).toEqual([{path: 'Cache', label: 'Cache', feature: 'off', sections: []}])
  })

  it('does not treat a group with ungated fields as switched', () => {
    const groups = recordFactGroups(
      model([
        field('enableCache', {type: 'boolean', group: 'Cache'}),
        field('note', {group: 'Cache'}),
      ]),
      {enableCache: false, note: 'x'},
      format,
    )

    expect(groups[0]?.feature).toBeUndefined()
    expect(groups[0]?.sections[0]?.facts).toHaveLength(2)
  })
})

describe('optionLabel', () => {
  const kind = {options: [{value: 1, label: ' Identity link '}, {value: 2, label: 'Money flow'}]}

  it('names the option a stored value stands for', () => {
    expect(optionLabel(kind as never, 1)).toBe('Identity link')
    expect(optionLabel(kind as never, 2)).toBe('Money flow')
  })

  it('has nothing to say for a value outside the options or with no field', () => {
    expect(optionLabel(kind as never, 9)).toBeUndefined()
    expect(optionLabel(kind as never, null)).toBeUndefined()
    expect(optionLabel(undefined, 1)).toBeUndefined()
  })
})
