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

import { expect, test } from '@playwright/test'
import { unsupported } from './actions'
import { problemsOf, readShots } from './shots'

const shots = readShots()

test('every shot comment is well formed', () => {
  expect(problemsOf(shots)).toEqual([])
})

test('every shot comment keeps to the vocabulary of steps and frames', () => {
  const unknown = shots
    .filter((shot) => unsupported(shot).length > 0)
    .map((shot) => `${shot.id}: ${unsupported(shot).join(', ')}`)

  expect(unknown).toEqual([])
})
