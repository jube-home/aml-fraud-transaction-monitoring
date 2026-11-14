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

import type {Component} from 'vue'
import {
  Activity,
  Archive,
  Eraser,
  ClipboardCheck,
  GitMerge,
  History,
  Inbox,
  ShieldAlert,
  BarChart3,
  Boxes,
  Building2,
  Download,
  Eye,
  FolderOpen,
  MessageSquare,
  Radar,
  Radio,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  Waypoints,
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
      45, 46, 47, 48, 60, 73, 74,
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
    key: 'graph',
    labelKey: 'nav.graph',
    icon: Waypoints,
    routeName: 'graph',
    permissions: [49],
  },
  {
    key: 'sanctions',
    labelKey: 'nav.sanctions',
    icon: ShieldCheck,
    routeName: 'sanctions',
    permissions: [29],
  },
  {
    key: 'svirfd',
    labelKey: 'nav.svirfd',
    icon: Radar,
    routeName: 'svirfd',
    permissions: [72],
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
    permissions: [6, 18, 31, 32, 33, 34, 35, 36, 38, 49, 51, 78, 79, 80],
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
        key: 'admin-graphs',
        labelKey: 'nav.adminGraphs',
        icon: Waypoints,
        routeName: 'adminGraphs',
        permissions: [50],
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
      {
        key: 'admin-governance',
        labelKey: 'nav.adminGovernance',
        icon: ClipboardCheck,
        routeName: 'adminGovernance',
        permissions: [6],
      },
      {
        key: 'admin-queue',
        labelKey: 'nav.adminQueue',
        icon: Inbox,
        routeName: 'adminQueue',
        permissions: [6],
      },
      {
        key: 'admin-replay',
        labelKey: 'nav.adminReplay',
        icon: History,
        routeName: 'adminReplay',
        permissions: [18],
      },
      {
        key: 'admin-resolution',
        labelKey: 'nav.adminResolution',
        icon: GitMerge,
        routeName: 'adminResolution',
        permissions: [49, 51],
      },
      {
        key: 'admin-entity-risk',
        labelKey: 'nav.adminEntityRisk',
        icon: ShieldAlert,
        routeName: 'adminEntityRisk',
        permissions: [18, 80],
      },
      {
        key: 'admin-erasure',
        labelKey: 'nav.adminErasure',
        icon: Eraser,
        routeName: 'adminErasure',
        permissions: [78, 79],
      },
    ],
  },
]
