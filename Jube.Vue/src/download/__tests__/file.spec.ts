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
import { filenameFromDisposition } from '../file'

describe('filenameFromDisposition', () => {
  it.each([
    ['attachment; filename="sample.csv"', 'sample.csv'],
    ['attachment; filename=sample.csv; size=3', 'sample.csv'],
    ["attachment; filename*=UTF-8''na%C3%AFve%20file.csv", 'naïve file.csv'],
    ['attachment; filename="plain.csv"; filename*=UTF-8\'\'real.csv', 'real.csv'],
    ['attachment', 'fallback.csv'],
    ['attachment; filename=""', 'fallback.csv'],
    ["attachment; filename*=UTF-8''%E0%A4%A", 'fallback.csv'],
    [null, 'fallback.csv'],
  ])('%s gives %s', (header, expected) => {
    expect(filenameFromDisposition(header, 'fallback.csv')).toBe(expected)
  })
})
