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
import {effectScope, nextTick, ref} from 'vue'
import {
  containZoom,
  minimumFitZoom,
  minimumZoom,
  nextZoom,
  useSvgViewport,
  viewBoxSize,
} from '../useSvgViewport'

describe('useSvgViewport', () => {
  it('steps the zoom in halves between one and six', () => {
    expect(nextZoom(1, 1)).toBe(1.5)
    expect(nextZoom(1, -1)).toBe(1)
    expect(nextZoom(5.75, 1)).toBe(6)
    expect(nextZoom(6, 1)).toBe(6)
    expect(nextZoom(2, -1, 0.25, 4)).toBe(1.75)
  })

  it('applies the zoom as the wrapper width and resets with fit', async () => {
    const wrap = ref<HTMLElement | null>(document.createElement('div'))
    const scroller = ref<HTMLElement | null>(document.createElement('div'))
    const scope = effectScope()
    const viewport = scope.run(() => useSvgViewport(wrap, scroller))!

    viewport.zoomIn()
    viewport.zoomIn()
    await nextTick()

    expect(viewport.zoom.value).toBe(2)
    expect(wrap.value!.style.width).toBe('200%')

    viewport.fit()
    await nextTick()

    expect(wrap.value!.style.width).toBe('100%')
    scope.stop()
  })

  it('pans the scroller by dragging and reports the drag so clicks can be ignored', () => {
    const wrap = ref<HTMLElement | null>(document.createElement('div'))
    const scroller = ref<HTMLElement | null>(document.createElement('div'))
    const scope = effectScope()
    const viewport = scope.run(() => useSvgViewport(wrap, scroller))!

    viewport.onPointerDown({button: 0, clientX: 100, clientY: 100} as PointerEvent)
    viewport.onPointerMove({clientX: 60, clientY: 90, buttons: 1} as PointerEvent)

    expect(viewport.dragging.value).toBe(true)
    expect(scroller.value!.scrollLeft).toBe(40)
    expect(scroller.value!.scrollTop).toBe(10)

    viewport.onPointerDown({button: 0, clientX: 0, clientY: 0} as PointerEvent)
    viewport.onPointerMove({clientX: 1, clientY: 1, buttons: 1} as PointerEvent)

    expect(viewport.dragging.value).toBe(false)
    scope.stop()
  })

  it('zooms from the keyboard only when the scroller itself has focus', () => {
    const wrap = ref<HTMLElement | null>(document.createElement('div'))
    const scroller = ref<HTMLElement | null>(document.createElement('div'))
    const scope = effectScope()
    const viewport = scope.run(() => useSvgViewport(wrap, scroller))!

    viewport.onKey({
      key: '+',
      target: scroller.value,
      preventDefault: () => undefined,
    } as unknown as KeyboardEvent)
    viewport.onKey({
      key: '+',
      target: document.createElement('button'),
      preventDefault: () => undefined,
    } as unknown as KeyboardEvent)

    expect(viewport.zoom.value).toBe(1.5)

    viewport.onKey({
      key: '0',
      target: scroller.value,
      preventDefault: () => undefined,
    } as unknown as KeyboardEvent)

    expect(viewport.zoom.value).toBe(1)
    scope.stop()
  })
})

describe('containZoom and viewBoxSize', () => {
  it('reads the drawing size from the viewBox', () => {
    expect(viewBoxSize('<svg viewBox="0 0 1200 800"></svg>')).toEqual({width: 1200, height: 800})
    expect(viewBoxSize('<svg></svg>')).toBeNull()
    expect(viewBoxSize(null)).toBeNull()
  })

  it('shrinks a tall drawing so it fits the box and never grows past the width', () => {
    expect(containZoom(1000, 500, 1000, 1000)).toBe(0.5)
    expect(containZoom(1000, 2000, 1000, 1000)).toBe(1)
    expect(containZoom(1000, 10, 1000, 1000)).toBe(minimumFitZoom)
    expect(containZoom(0, 500, 1000, 1000)).toBe(1)
  })

  it('steps by a quarter below full size down to the floor and back up', () => {
    expect(nextZoom(1, -1, 0.5, 6, minimumZoom)).toBe(0.75)
    expect(nextZoom(0.25, -1, 0.5, 6, minimumZoom)).toBe(minimumZoom)
    expect(nextZoom(minimumZoom, 1, 0.5, 6, minimumZoom)).toBe(0.25)
    expect(nextZoom(0.75, 1, 0.5, 6, minimumZoom)).toBe(1)
    expect(nextZoom(1, 1)).toBe(1.5)
    expect(nextZoom(1, -1)).toBe(1)
  })
})
