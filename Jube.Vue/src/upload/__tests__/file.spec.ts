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
import { extensionOf, uploadProblem } from '../file'

describe('extensionOf', () => {
  it.each([
    ['list.csv', '.csv'],
    ['LIST.CSV', '.csv'],
    ['archive.tar.gz', '.gz'],
    ['noextension', ''],
    ['.hidden', '.hidden'],
  ])('%s is %s', (name, expected) => {
    expect(extensionOf(name)).toBe(expected)
  })
})

describe('uploadProblem', () => {
  const csv = ['.csv']

  it('asks for a file when none is chosen', () => {
    expect(uploadProblem(null, csv)).toBe('none')
  })

  it('refuses a file of another type, whatever its case', () => {
    expect(uploadProblem({ name: 'list.xlsx', size: 10 }, csv)).toBe('type')
    expect(uploadProblem({ name: 'list.CSV', size: 10 }, csv)).toBeNull()
  })

  it('refuses an empty file', () => {
    expect(uploadProblem({ name: 'list.csv', size: 0 }, csv)).toBe('empty')
  })

  it('accepts any type when none are listed', () => {
    expect(uploadProblem({ name: 'anything', size: 1 }, [])).toBeNull()
  })
})
