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
export interface HistogramBucket {
  bin: number
  frequency: number
}

export interface ColumnStatistics {
  min: number
  max: number
  mean: number
  median: number
  standardDeviation: number
  histogram: HistogramBucket[]
}

export interface PayloadStatistics {
  columns: Record<string, ColumnStatistics>
}

export type StatisticsLabels = Record<string, string | null | undefined>

export interface StatisticsRow {
  column: string
  label: string
  min: number | null
  max: number | null
  mean: number | null
  median: number | null
  standardDeviation: number | null
  histogram: HistogramBucket[]
  hasData: boolean
}

export function statisticsRows(
  statistics: PayloadStatistics | null | undefined,
  labels?: StatisticsLabels | null,
): StatisticsRow[] {
  if (!statistics || !statistics.columns) return []

  const labelOf = (column: string): string => labels?.[column]?.trim() || column

  return Object.keys(statistics.columns)
    .sort((a, b) => labelOf(a).localeCompare(labelOf(b)) || a.localeCompare(b))
    .map((column) => {
      const value = statistics.columns[column]!
      const histogram = Array.isArray(value.histogram) ? value.histogram : []

      const hasData = histogram.length > 0
      const figure = (number: number): number | null => (hasData ? number : null)

      return {
        column,
        label: labelOf(column),
        min: figure(value.min),
        max: figure(value.max),
        mean: figure(value.mean),
        median: figure(value.median),
        standardDeviation: figure(value.standardDeviation),
        histogram,
        hasData,
      }
    })
}

export interface Bar {
  x: number
  y: number
  width: number
  height: number
  bin: number
  frequency: number
}

export function histogramBars(
  buckets: HistogramBucket[],
  width: number,
  height: number,
  gap = 1,
): Bar[] {
  if (buckets.length === 0) return []

  const peak = Math.max(...buckets.map((bucket) => bucket.frequency), 0)
  const slot = width / buckets.length
  const barWidth = Math.max(slot - gap, 1)

  return buckets.map((bucket, index) => {
    const raw = peak === 0 ? 0 : (bucket.frequency / peak) * height
    const barHeight = bucket.frequency > 0 ? Math.max(raw, 1) : 0

    return {
      x: index * slot,
      y: height - barHeight,
      width: barWidth,
      height: barHeight,
      bin: bucket.bin,
      frequency: bucket.frequency,
    }
  })
}

export function peakOf(buckets: HistogramBucket[]): HistogramBucket | undefined {
  return buckets.reduce<HistogramBucket | undefined>(
    (best, bucket) => (best === undefined || bucket.frequency > best.frequency ? bucket : best),
    undefined,
  )
}
