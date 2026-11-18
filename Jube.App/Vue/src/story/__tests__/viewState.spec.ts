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

import {beforeEach, describe, expect, it} from 'vitest'
import {forgetViewStates, viewStateFor} from '../viewState'

describe('viewStateFor', () => {
  beforeEach(forgetViewStates)

  it('hands back the same state for the same place and a fresh one for another', () => {
    viewStateFor('crud:Rule:1').search = 'velocity'

    expect(viewStateFor('crud:Rule:1').search).toBe('velocity')
    expect(viewStateFor('crud:Rule:2').search).toBe('')
  })
})
