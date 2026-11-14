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

import {ref, watch, type Ref} from 'vue'

export interface SvgViewportOptions {
  maximum?: number
  step?: number
  fitZoom?: () => number
}

export const minimumFitZoom = 0.25

export const minimumZoom = 0.1

export const fineZoomStep = 0.25

export const defaultMaximumZoom = 6

export const defaultZoomStep = 0.5

export function nextZoom(
  current: number,
  direction: 1 | -1,
  step = defaultZoomStep,
  maximum = defaultMaximumZoom,
  minimum = 1,
): number {
  const fine = direction < 0 ? current <= 1 : current < 1
  const size = fine ? Math.min(step, fineZoomStep) : step
  const raw = current + direction * size
  const rounded = Math.round(raw / size) * size

  return Math.min(maximum, Math.max(minimum, Number(rounded.toFixed(4))))
}

export function containZoom(
  boxWidth: number,
  boxHeight: number,
  drawingWidth: number,
  drawingHeight: number,
): number {
  if (boxWidth <= 0 || boxHeight <= 0 || drawingWidth <= 0 || drawingHeight <= 0) return 1

  const naturalHeight = boxWidth * (drawingHeight / drawingWidth)

  return Math.max(minimumFitZoom, Math.min(1, Number((boxHeight / naturalHeight).toFixed(4))))
}

export function viewBoxSize(svg: string | null): { width: number; height: number } | null {
  const match = svg?.match(/viewBox="\s*[-\d.]+\s+[-\d.]+\s+([\d.]+)\s+([\d.]+)\s*"/)

  if (!match) return null

  return {width: Number(match[1]), height: Number(match[2])}
}

export function useSvgViewport(
  wrap: Ref<HTMLElement | null>,
  scroller: Ref<HTMLElement | null>,
  options: SvgViewportOptions = {},
) {
  const maximum = options.maximum ?? defaultMaximumZoom
  const step = options.step ?? defaultZoomStep
  const zoom = ref(1)
  const dragging = ref(false)

  function contained(): number {
    return Math.min(1, options.fitZoom?.() ?? 1)
  }

  let origin: { x: number; y: number; left: number; top: number } | null = null

  watch(
    zoom,
    (value) => {
      if (wrap.value) wrap.value.style.width = `${value * 100}%`
    },
    {flush: 'post'},
  )

  function zoomBy(direction: 1 | -1): void {
    zoom.value = nextZoom(zoom.value, direction, step, maximum, options.fitZoom ? minimumZoom : 1)
  }

  function zoomIn(): void {
    zoomBy(1)
  }

  function zoomOut(): void {
    zoomBy(-1)
  }

  function fit(): void {
    zoom.value = contained()
  }

  function onPointerDown(event: PointerEvent): void {
    const target = scroller.value

    if (!target || event.button !== 0) return

    origin = {x: event.clientX, y: event.clientY, left: target.scrollLeft, top: target.scrollTop}
    dragging.value = false
  }

  function onPointerMove(event: PointerEvent): void {
    const target = scroller.value

    if (!target || !origin || event.buttons === 0) return

    const dx = event.clientX - origin.x
    const dy = event.clientY - origin.y

    if (!dragging.value && Math.abs(dx) + Math.abs(dy) < 4) return

    dragging.value = true
    target.scrollLeft = origin.left - dx
    target.scrollTop = origin.top - dy
  }

  function onPointerUp(): void {
    origin = null
    queueMicrotask(() => {
      dragging.value = false
    })
  }

  function onKey(event: KeyboardEvent): void {
    if (event.target !== scroller.value) return

    if (event.key === '+' || event.key === '=') {
      event.preventDefault()
      zoomIn()
    } else if (event.key === '-' || event.key === '_') {
      event.preventDefault()
      zoomOut()
    } else if (event.key === '0') {
      event.preventDefault()
      fit()
    }
  }

  return {
    zoom,
    dragging,
    zoomIn,
    zoomOut,
    fit,
    step: zoomBy,
    onPointerDown,
    onPointerMove,
    onPointerUp,
    onKey,
  }
}
