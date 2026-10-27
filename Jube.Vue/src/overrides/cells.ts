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

import { fromLocalInput, isFuture, toLocalInput } from '../composables/localDateTime'

export interface ChoiceCell {
  value: number
  label: string
}

export function switchCell(
  checked: boolean,
  label: string,
  onChange: (value: boolean) => Promise<boolean>,
): HTMLElement {
  const input = document.createElement('input')

  input.type = 'checkbox'
  input.className = 'toggle toggle-sm toggle-primary'
  input.checked = checked
  input.setAttribute('aria-label', label)
  input.addEventListener('change', () => {
    const wanted = input.checked

    input.disabled = true
    void onChange(wanted).then((ok) => {
      input.disabled = false

      if (!ok) input.checked = !wanted
    })
  })

  return input
}

export function choiceCell(
  current: number,
  choices: ChoiceCell[],
  disabled: boolean,
  label: string,
  onChange: (value: number) => Promise<boolean>,
): HTMLElement {
  const select = document.createElement('select')

  select.className = 'select select-xs w-28'
  select.disabled = disabled
  select.setAttribute('aria-label', label)

  for (const choice of choices) {
    const option = document.createElement('option')

    option.value = String(choice.value)
    option.textContent = choice.label
    option.selected = choice.value === current
    select.append(option)
  }

  select.addEventListener('change', () => {
    const wanted = Number(select.value)

    select.disabled = true
    void onChange(wanted).then((ok) => {
      select.disabled = false

      if (!ok) select.value = String(current)
    })
  })

  return select
}

export function expiryCell(
  value: string | null,
  disabled: boolean,
  label: string,
  tooEarly: () => void,
  onChange: (value: Date | null) => Promise<boolean>,
): HTMLElement {
  const input = document.createElement('input')
  const shown = toLocalInput(value)

  input.type = 'datetime-local'
  input.className = 'input input-xs w-48 font-mono'
  input.value = shown
  input.disabled = disabled
  input.setAttribute('aria-label', label)
  input.addEventListener('change', () => {
    const wanted = fromLocalInput(input.value)

    if (!isFuture(wanted)) {
      tooEarly()
      input.value = shown

      return
    }

    input.disabled = true
    void onChange(wanted).then((ok) => {
      input.disabled = false

      if (!ok) input.value = shown
    })
  })

  return input
}
