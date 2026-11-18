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

import { test } from '@playwright/test'
import { capture, unsupported } from './actions'
import { readShots } from './shots'

const signedIn = Boolean(process.env.JUBE_DOCS_USER && process.env.JUBE_DOCS_PASSWORD)
const only = process.env.JUBE_DOCS_ONLY ?? ''

for (const shot of readShots().filter((candidate) => candidate.id.includes(only))) {
  test(shot.id, async ({ page }) => {
    const unknown = unsupported(shot)

    if (unknown.length > 0) throw new Error(`not in the vocabulary: ${unknown.join(', ')}`)

    test.skip(!signedIn, 'JUBE_DOCS_USER and JUBE_DOCS_PASSWORD are not set')

    await capture(page, shot)
  })
}
