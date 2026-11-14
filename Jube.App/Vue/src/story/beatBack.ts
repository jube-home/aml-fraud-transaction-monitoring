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

import {inject, onBeforeUnmount, provide, ref, watchEffect, type InjectionKey, type Ref} from 'vue'

export interface BeatBack {
  label: string
  testid: string
  run: () => void
}

const beatBackKey: InjectionKey<Ref<BeatBack | undefined>> = Symbol('beatBack')

export function provideBeatBack(): Ref<BeatBack | undefined> {
  const slot = ref<BeatBack>()

  provide(beatBackKey, slot)

  return slot
}

export function useBeatBack(source: () => BeatBack | undefined): boolean {
  const slot = inject(beatBackKey, undefined)

  if (!slot) return false

  let mine: BeatBack | undefined

  watchEffect(() => {
    const next = source()

    if (!next) return

    mine = next
    slot.value = next
  })

  onBeforeUnmount(() => {
    if (slot.value === mine) slot.value = undefined
  })

  return true
}
