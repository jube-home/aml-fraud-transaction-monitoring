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
import type {FormField, FormModel} from '../types'
import {withUnsetChoices} from '../values'

function choice(name: string, values: (string | number)[], patch: Partial<FormField> = {}): FormField {
  return {
    name,
    nullable: false,
    options: values.map((value) => ({value, label: String(value)})),
    newDefault: values[0],
    ...patch,
  } as FormField
}

function model(...fields: FormField[]): FormModel {
  return {fields} as unknown as FormModel
}

describe('withUnsetChoices', () => {
  it('gives an unset choice its declared default', () => {
    const values = withUnsetChoices(
      model(
        choice('interval', ['n', 'h', 'd'], {newDefault: 'h'}),
        choice('kind', [1, 2]),
        choice('unit', ['s', 'h']),
        choice('blank', ['a', 'b']),
        choice('missing', [3, 4]),
      ),
      {interval: '\u0000', kind: 0, unit: null, blank: ''},
    )

    expect(values).toEqual({interval: 'h', kind: 1, unit: 's', blank: 'a', missing: 3})
  })

  it('leaves a chosen value alone', () => {
    const record = {interval: 'd', kind: 2}

    expect(
      withUnsetChoices(model(choice('interval', ['n', 'h', 'd']), choice('kind', [1, 2])), record),
    ).toEqual(record)
  })

  it('leaves zero alone when zero is one of the choices', () => {
    expect(withUnsetChoices(model(choice('kind', [0, 1], {newDefault: 1})), {kind: 0})).toEqual({
      kind: 0,
    })
  })

  it('never replaces a real value the choices do not list', () => {
    expect(withUnsetChoices(model(choice('kind', [1, 2, 3])), {kind: 5})).toEqual({kind: 5})
  })

  it('leaves a choice that may be empty, or one with no usable default, unset', () => {
    const values = withUnsetChoices(
      model(
        choice('optional', [1, 2], {nullable: true}),
        choice('undeclared', [1, 2], {newDefault: null}),
        choice('stray', [1, 2], {newDefault: 9}),
      ),
      {optional: null, undeclared: 0, stray: 0},
    )

    expect(values).toEqual({optional: null, undeclared: 0, stray: 0})
  })

  it('does not touch fields that are not choices or the record passed in', () => {
    const record = {name: '', count: 0}
    const values = withUnsetChoices(
      model({name: 'name', nullable: false, options: [], newDefault: 'x'} as unknown as FormField),
      record,
    )

    expect(values).toEqual(record)
    expect(values).not.toBe(record)
  })
})
