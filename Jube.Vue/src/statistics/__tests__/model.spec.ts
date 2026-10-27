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
import { histogramBars, peakOf, statisticsRows, type PayloadStatistics } from '../model'

const statistics: PayloadStatistics = {
  columns: {
    zeta: {
      min: 0,
      max: 0,
      mean: 0,
      median: 0,
      standardDeviation: 0,
      histogram: [],
    },
    alpha: {
      min: 1,
      max: 9,
      mean: 5,
      median: 5,
      standardDeviation: 2,
      histogram: [
        { bin: 1, frequency: 2 },
        { bin: 5, frequency: 8 },
      ],
    },
  },
}

describe('statisticsRows', () => {
  it('sorts by column name and flags a column with no values', () => {
    const rows = statisticsRows(statistics)

    expect(rows.map((row) => row.column)).toEqual(['alpha', 'zeta'])
    expect(rows.map((row) => row.hasData)).toEqual([true, false])
  })

  it('blanks the figures of a column with no values, which the server reports as zeros', () => {
    const [alpha, zeta] = statisticsRows(statistics)

    expect(alpha!.mean).toBe(5)
    expect(zeta!.mean).toBeNull()
    expect(zeta!.standardDeviation).toBeNull()
  })

  it.each([[undefined], [null], [{ columns: {} }]])('gives no rows for %j', (value) => {
    expect(statisticsRows(value as never)).toEqual([])
  })

  it('treats a missing histogram as no data rather than failing', () => {
    const rows = statisticsRows({
      columns: { a: { min: 1, max: 1, mean: 1, median: 1, standardDeviation: 0 } as never },
    })

    expect(rows[0]!.hasData).toBe(false)
  })
})

describe('statisticsRows labels', () => {
  it('shows the supplied display name and falls back to the column name', () => {
    const rows = statisticsRows(statistics, { alpha: 'Alpha score' })

    expect(rows.map((row) => [row.column, row.label])).toEqual([
      ['alpha', 'Alpha score'],
      ['zeta', 'zeta'],
    ])
  })

  it.each([[''], ['   '], [null], [undefined]])('falls back for the label %j', (label) => {
    expect(statisticsRows(statistics, { alpha: label })[0]!.label).toBe('alpha')
  })

  it('sorts by what is displayed, not by the column name', () => {
    const rows = statisticsRows(statistics, { alpha: 'Zebra', zeta: 'Apple' })

    expect(rows.map((row) => row.column)).toEqual(['zeta', 'alpha'])
  })

  it('ignores labels for columns that are not present', () => {
    expect(statisticsRows(statistics, { nope: 'Nope' })).toHaveLength(2)
  })
})

describe('histogramBars', () => {
  it('scales to the tallest bar and leaves a gap between bars', () => {
    const bars = histogramBars(statistics.columns.alpha!.histogram, 20, 10)

    expect(bars).toHaveLength(2)
    expect(bars[1]).toMatchObject({ x: 10, y: 0, height: 10, width: 9 })
    expect(bars[0]!.height).toBeCloseTo(2.5)
  })

  it('keeps a non-zero bar visible and a zero bar flat', () => {
    const bars = histogramBars(
      [
        { bin: 0, frequency: 1 },
        { bin: 1, frequency: 1000 },
        { bin: 2, frequency: 0 },
      ],
      30,
      10,
    )

    expect(bars[0]!.height).toBe(1)
    expect(bars[2]!.height).toBe(0)
  })

  it('draws nothing for no buckets and copes with all-zero frequencies', () => {
    expect(histogramBars([], 10, 10)).toEqual([])
    expect(histogramBars([{ bin: 0, frequency: 0 }], 10, 10)[0]!.height).toBe(0)
  })
})

describe('peakOf', () => {
  it('finds the most frequent bucket, first one on a tie', () => {
    expect(
      peakOf([
        { bin: 1, frequency: 3 },
        { bin: 2, frequency: 3 },
      ]),
    ).toEqual({ bin: 1, frequency: 3 })
    expect(peakOf([])).toBeUndefined()
  })
})
