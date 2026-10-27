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

import { beforeEach, describe, expect, it } from 'vitest'
import { readPanes, usePanes } from '../usePanes'

describe('usePanes', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('starts with both panes open', () => {
    expect(readPanes('case')).toEqual({ left: true, right: true })
  })

  it('remembers what was closed, per page', () => {
    const panes = usePanes('case')

    panes.toggleLeft()
    panes.toggleRight()

    expect(readPanes('case')).toEqual({ left: false, right: false })
    expect(readPanes('integrity')).toEqual({ left: true, right: true })

    panes.toggleRight()

    expect(usePanes('case').left.value).toBe(false)
    expect(usePanes('case').right.value).toBe(true)
  })

  it('ignores what it cannot read', () => {
    localStorage.setItem('jube.panes.case', '{not json')
    expect(readPanes('case')).toEqual({ left: true, right: true })

    localStorage.setItem('jube.panes.case', '"text"')
    expect(readPanes('case')).toEqual({ left: true, right: true })

    localStorage.setItem('jube.panes.case', JSON.stringify({ left: 'no', right: false }))
    expect(readPanes('case')).toEqual({ left: true, right: false })
  })
})
