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

import {afterEach, describe, expect, it, vi} from 'vitest'
import {defineComponent, h} from 'vue'
import {mountWith, type Harness} from '../../cases/checklist/__tests__/support'
import AdminTelemetryPage from '../AdminTelemetryPage.vue'

const me = vi.fn()
const replace = vi.fn()
let areaQuery: string | undefined

vi.mock('vue-router', () => ({
  useRoute: () => ({query: {area: areaQuery}}),
  useRouter: () => ({replace}),
}))

vi.mock('../../auth/me', () => ({
  meQueryOptions: {
    queryKey: ['me-telemetry-test'],
    queryFn: () => me(),
  },
}))

vi.mock('../../components/CrudPanel.vue', () => ({
  default: defineComponent({
    props: {area: {type: String, default: ''}},
    setup: (props) => () => h('div', {'data-testid': 'crud-panel', 'data-area': props.area}),
  }),
}))

let harness: Harness | undefined

afterEach(() => {
  harness?.unmount()
  harness = undefined
  vi.clearAllMocks()
  areaQuery = undefined
})

describe('AdminTelemetryPage', () => {
  it('shows the tabs and the first area to the landlord', async () => {
    me.mockResolvedValue({landlord: true, permissions: []})
    harness = await mountWith(AdminTelemetryPage, {})

    expect(harness.byTestId('adminTelemetry-denied')).toBeNull()
    expect(harness.byTestId('crud-panel')?.getAttribute('data-area')).toBe(
      'OpenTelemetryLogCounter',
    )
  })

  it('opens the area named in the address for the landlord', async () => {
    areaQuery = 'OpenTelemetryExclude'
    me.mockResolvedValue({landlord: true, permissions: []})
    harness = await mountWith(AdminTelemetryPage, {})

    expect(harness.byTestId('crud-panel')?.getAttribute('data-area')).toBe('OpenTelemetryExclude')
  })

  it('denies a tenant user even with every permission and never mounts the panel', async () => {
    me.mockResolvedValue({
      landlord: false,
      permissions: Array.from({length: 45}, (_, i) => i + 1),
    })
    harness = await mountWith(AdminTelemetryPage, {})

    expect(harness.byTestId('adminTelemetry-denied')).not.toBeNull()
    expect(harness.byTestId('crud-panel')).toBeNull()
  })

  it('denies when the identity cannot be read', async () => {
    me.mockRejectedValue(new Error('offline'))
    harness = await mountWith(AdminTelemetryPage, {})

    expect(harness.byTestId('adminTelemetry-denied')).not.toBeNull()
    expect(harness.byTestId('crud-panel')).toBeNull()
  })
})
