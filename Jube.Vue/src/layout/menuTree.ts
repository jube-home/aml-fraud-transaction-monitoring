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

import type { Component } from 'vue'
import {
  Activity,
  Archive,
  BarChart3,
  Boxes,
  Building2,
  Download,
  Eye,
  FolderOpen,
  MessageSquare,
  Radio,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  Wrench,
} from 'lucide-vue-next'

export type LandlordRule = 'any' | 'only'

export interface MenuNode {
  key: string
  labelKey?: string
  label?: string
  icon?: Component
  legacyPath?: string
  routeName?: string
  permissions?: number[]
  landlord?: LandlordRule
  children?: MenuNode[]
}

export const menuTree: MenuNode[] = [
  {
    key: 'askJooby',
    labelKey: 'nav.askJooby',
    icon: MessageSquare,
    legacyPath: '/AskJooby',
    routeName: 'askJooby',
  },
  {
    key: 'cases',
    labelKey: 'nav.cases',
    icon: FolderOpen,
    routeName: 'cases',
    permissions: [1],
  },
  {
    key: 'override',
    labelKey: 'nav.override',
    icon: SlidersHorizontal,
    routeName: 'override',
    permissions: [2],
  },
  {
    key: 'models',
    labelKey: 'nav.models',
    icon: Boxes,
    routeName: 'models',
    permissions: [
      3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 37,
      45, 46, 47, 48, 60,
    ],
  },
  {
    key: 'visualisation',
    labelKey: 'nav.visualisation',
    icon: BarChart3,
    routeName: 'visualisation',
    permissions: [28],
  },
  {
    key: 'sanctions',
    labelKey: 'nav.sanctions',
    icon: ShieldCheck,
    routeName: 'sanctions',
    permissions: [29],
  },
  {
    key: 'watcher',
    labelKey: 'nav.watcher',
    icon: Eye,
    routeName: 'watcher',
    permissions: [30],
  },
  {
    key: 'admin',
    labelKey: 'nav.admin',
    icon: Settings,
    permissions: [31, 32, 33, 34, 35, 36, 38],
    landlord: 'any',
    children: [
      {
        key: 'admin-tenants',
        labelKey: 'nav.adminTenants',
        icon: Building2,
        routeName: 'adminTenants',
        landlord: 'only',
      },
      {
        key: 'admin-security',
        labelKey: 'nav.adminSecurity',
        icon: ShieldCheck,
        routeName: 'adminSecurity',
        permissions: [34, 35, 36],
        landlord: 'any',
      },
      {
        key: 'admin-performance',
        labelKey: 'nav.adminPerformance',
        icon: Activity,
        routeName: 'adminPerformance',
        landlord: 'only',
      },
      {
        key: 'admin-visualisations',
        labelKey: 'nav.adminVisualisations',
        icon: BarChart3,
        routeName: 'adminVisualisations',
        permissions: [31, 32, 33],
      },
      {
        key: 'admin-telemetry',
        labelKey: 'nav.adminTelemetry',
        icon: Radio,
        routeName: 'adminTelemetry',
        landlord: 'only',
      },
      {
        key: 'admin-sanctions-loader',
        labelKey: 'nav.adminSanctionsLoader',
        icon: Download,
        routeName: 'adminSanctionsLoader',
        landlord: 'only',
      },
      {
        key: 'admin-preservation',
        labelKey: 'nav.adminPreservation',
        icon: Archive,
        routeName: 'adminPreservation',
        permissions: [38],
      },
    ],
  },
  { key: 'tools', labelKey: 'nav.tools', icon: Wrench, routeName: 'tools' },
]
