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
const realignDelays = [350, 900, 1600]
const interruptions = ['wheel', 'touchstart', 'keydown', 'pointerdown'] as const

export function scrollToSection(id: string): void {
  const reduced =
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  const target = () => document.getElementById(id)

  target()?.scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', block: 'start' })

  let interrupted = false
  const interrupt = () => {
    interrupted = true
  }

  for (const name of interruptions) {
    window.addEventListener(name, interrupt, { once: true, passive: true })
  }

  const timers = realignDelays.map((delay) =>
    setTimeout(() => {
      if (!interrupted) target()?.scrollIntoView({ behavior: 'auto', block: 'start' })
    }, delay),
  )

  setTimeout(
    () => {
      timers.forEach(clearTimeout)

      for (const name of interruptions) window.removeEventListener(name, interrupt)
    },
    realignDelays[realignDelays.length - 1]! + 100,
  )
}
