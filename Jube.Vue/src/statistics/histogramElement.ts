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
import { histogramBars, type HistogramBucket } from './model'

const svgNamespace = 'http://www.w3.org/2000/svg'

export interface HistogramText {
  label: string
  bar: (bin: number, frequency: number) => string
}

export function histogramElement(
  buckets: HistogramBucket[],
  text: HistogramText,
  width = 140,
  height = 28,
): SVGSVGElement {
  const svg = document.createElementNS(svgNamespace, 'svg')

  svg.setAttribute('viewBox', `0 0 ${width} ${height}`)
  svg.setAttribute('width', String(width))
  svg.setAttribute('height', String(height))
  svg.setAttribute('role', 'img')
  svg.setAttribute('aria-label', text.label)

  for (const bar of histogramBars(buckets, width, height)) {
    const rect = document.createElementNS(svgNamespace, 'rect')

    rect.setAttribute('class', 'fill-primary/55 stroke-primary')
    rect.setAttribute('stroke-width', '0.75')
    rect.setAttribute('x', bar.x.toFixed(2))
    rect.setAttribute('y', bar.y.toFixed(2))
    rect.setAttribute('width', bar.width.toFixed(2))
    rect.setAttribute('height', bar.height.toFixed(2))

    const title = document.createElementNS(svgNamespace, 'title')

    title.textContent = text.bar(bar.bin, bar.frequency)
    rect.append(title)
    svg.append(rect)
  }

  return svg
}
