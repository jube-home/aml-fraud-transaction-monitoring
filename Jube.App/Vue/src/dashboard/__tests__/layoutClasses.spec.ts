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
import dashboardSource from '../VisualisationDashboard.vue?raw'
import tileSource from '../DashboardTile.vue?raw'
import markupSource from '../../datasource/JubeMarkup.vue?raw'

describe('dashboard layout follows the width of the pane that hosts it', () => {
  it('measures the grid by its own container rather than the viewport', () => {
    expect(dashboardSource).toContain('@container/dash')
    expect(dashboardSource).toContain('@2xl/dash:grid-cols-(--dash-columns)')
    expect(dashboardSource).not.toMatch(/\bmd:grid-cols-\(--dash-columns\)/)
  })

  it('lets rows grow with their content so a tile never spills over its neighbour', () => {
    expect(dashboardSource).toContain('auto-rows-[minmax(var(--dash-rows),auto)]')
    expect(dashboardSource).not.toMatch(/auto-rows-\(--dash-rows\)/)
  })

  it('steps a tile through stacked, half and designed widths by container width', () => {
    expect(tileSource).toContain('@2xl/dash:col-[span_var(--tile-columns-mid)]')
    expect(tileSource).toContain('@5xl/dash:col-[span_var(--tile-columns)]')
    expect(tileSource).not.toMatch(/\bmd:col-\[span_var/)
  })

  it('spaces a badge from its description and stacks stats when the tile is narrow', () => {
    expect(markupSource).toContain('[&_.badge]:me-1.5')
    expect(markupSource).toContain('@max-sm:[&_.stats]:[grid-auto-flow:row]')
  })
})
