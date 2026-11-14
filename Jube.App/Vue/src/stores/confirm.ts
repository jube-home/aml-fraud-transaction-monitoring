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

import {ref} from 'vue'
import {defineStore} from 'pinia'

export interface ConfirmRequest {
  title: string
  body: string
  confirmLabel: string
  cancelLabel: string
}

export const useConfirmStore = defineStore('confirm', () => {
  const request = ref<ConfirmRequest>()
  let settle: ((answer: boolean) => void) | undefined

  function answer(value: boolean): void {
    const resolve = settle

    settle = undefined
    request.value = undefined
    resolve?.(value)
  }

  function ask(next: ConfirmRequest): Promise<boolean> {
    answer(false)

    return new Promise<boolean>((resolve) => {
      settle = resolve
      request.value = next
    })
  }

  return {request, ask, answer}
})
