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
  caseFilterSets,
  caseSelectOperators,
  caseWhereOperators,
  exhaustiveFilters,
  exhaustiveOperators,
  type FilterLabels,
} from '../filters'
import type { Completion } from '../../ruleBuilder/types'

const labels: FilterLabels = {
  yes: 'Yes',
  no: 'No',
  ascending: 'Ascending',
  descending: 'Descending',
  closedStatus: ['Open', 'Suspend Open', 'Suspend Closed', 'Closed', 'Suspend Bypass'],
  priority: ['Ultra High', 'High', 'Normal', 'Low', 'Ultra Low'],
  diaryType: ['Review', 'Customer response', 'Regulatory deadline', 'Payment hold'],
}

function completion(name: string, dataType: Completion['dataType'], group = 'Payload'): Completion {
  return {
    name,
    value: name,
    meta: `${name}:${dataType}`,
    score: 1000,
    dataType,
    group,
    field: `"Json" ->> '${name}'`,
  }
}

describe('exhaustiveFilters', () => {
  const filters = exhaustiveFilters([
    completion('Payload.Name', 'string'),
    completion('Payload.Count', 'integer'),
    completion('Payload.Amount', 'double'),
    completion('Payload.Flag', 'boolean'),
    completion('Payload.At', 'datetime'),
    completion('Payload.Name', 'string'),
  ])

  it('offers strings, numbers and booleans and skips other types and duplicates', () => {
    expect(filters.map((filter) => filter.id)).toEqual([
      'Payload.Name',
      'Payload.Count',
      'Payload.Amount',
      'Payload.Flag',
    ])
  })

  it('treats a double column as a real number rather than an integer', () => {
    expect(filters.find((filter) => filter.id === 'Payload.Amount')!.type).toBe('double')
  })

  it('filters on the SQL path the backend gave, not on the display name', () => {
    expect(filters[0]!.field).toBe(`"Json" ->> 'Payload.Name'`)
  })

  it('renders booleans as a Yes or No radio that only offers equals', () => {
    const flag = filters.find((filter) => filter.id === 'Payload.Flag')!

    expect(flag).toMatchObject({
      type: 'string',
      input: 'radio',
      default_value: 'True',
      values: { True: 'Yes', False: 'No' },
      operators: ['equal'],
    })
  })

  it('lists exactly the operators the old page allowed', () => {
    expect(exhaustiveOperators.map((operator) => operator.type)).toEqual([
      'equal',
      'less',
      'less_or_equal',
      'greater',
      'begins_with',
      'contains',
      'ends_with',
    ])
  })
})

describe('caseFilterSets', () => {
  const statuses = [
    { guid: 'a-1', name: 'New' },
    { guid: 'b-2', name: 'Review' },
  ]
  const sets = caseFilterSets(
    [completion('Payload.Name', 'string'), completion('Payload.At', 'datetime')],
    statuses,
    labels,
  )

  it('has the same sixteen case columns as filters and as sort columns, before the payload ones', () => {
    expect(sets.filters.slice(0, 16).map((filter) => filter.optgroup)).toEqual(
      Array(16).fill('Case'),
    )
    expect(sets.selects.slice(0, 16)).toHaveLength(16)
    expect(sets.filters[16]!.id).toBe('Payload.Name')
  })

  it('filters status by guid but sorts it by name', () => {
    const filter = sets.filters.find((item) => item.id === 'CaseWorkflowStatusGuid')!
    const select = sets.selects.find((item) => item.id === 'CaseWorkflowStatus')!

    expect(filter.field).toBe('"CaseWorkflowStatus"."Guid"')
    expect(filter.values).toEqual({ 'a-1': 'New', 'b-2': 'Review' })
    expect(select.field).toBe('"CaseWorkflowStatus"."Name"')
  })

  it('labels the enumerations from the labels it is given', () => {
    expect(sets.filters.find((item) => item.id === 'ClosedStatusId')!.values).toMatchObject({
      0: 'Open',
      4: 'Suspend Bypass',
    })
    expect(sets.filters.find((item) => item.id === 'Priority')!.values).toMatchObject({
      1: 'Ultra High',
      5: 'Ultra Low',
    })
    expect(sets.filters.find((item) => item.id === 'Locked')!.values).toEqual({ 1: 'Yes', 0: 'No' })
    expect(sets.filters.find((item) => item.id === 'DiaryTypeId')!.values).toEqual({
      1: 'Review',
      2: 'Customer response',
      3: 'Regulatory deadline',
      4: 'Payment hold',
    })
  })

  it('makes every sort column an ascending or descending radio that only offers order', () => {
    for (const select of sets.selects) {
      expect(select).toMatchObject({
        type: 'string',
        input: 'radio',
        default_value: 'ASC',
        operators: ['order'],
        values: { ASC: 'Ascending', DESC: 'Descending' },
      })
    }
  })

  it('can sort by a payload column that cannot be filtered on', () => {
    expect(sets.selects.some((select) => select.id === 'Payload.At')).toBe(true)
    expect(sets.filters.some((filter) => filter.id === 'Payload.At')).toBe(false)
  })

  it('has unique ids in each list', () => {
    expect(new Set(sets.filters.map((f) => f.id)).size).toBe(sets.filters.length)
    expect(new Set(sets.selects.map((f) => f.id)).size).toBe(sets.selects.length)
  })

  it('keeps the old operators, with like only on text', () => {
    expect(caseWhereOperators.map((operator) => operator.type)).toEqual([
      'equal',
      'not_equal',
      'less',
      'less_or_equal',
      'greater',
      'greater_or_equal',
      'like',
      'not_like',
    ])
    expect(caseWhereOperators.at(-1)).toMatchObject({ nb_inputs: 1, apply_to: ['string'] })
    expect(caseSelectOperators).toHaveLength(1)
  })
})
