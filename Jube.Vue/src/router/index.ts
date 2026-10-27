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

import { createRouter, createWebHistory, type RouterHistory } from 'vue-router'
import type { QueryClient } from '@tanstack/vue-query'
import { ApiError } from '../api/useApi'
import { queryClient } from '../api/queryClient'
import { meQueryOptions } from '../auth/me'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    titleKey?: string
    menuRoute?: string
  }
}

export function createAppRouter(history: RouterHistory, client: QueryClient = queryClient) {
  const router = createRouter({
    history,
    routes: [
      { path: '/', name: 'login', component: () => import('../pages/LoginPage.vue') },
      { path: '/login', redirect: (to) => ({ name: 'login', query: to.query }) },
      { path: '/home', redirect: { name: 'askJooby' } },
      {
        path: '/preservation/snapshots',
        redirect: { name: 'adminPreservation', query: { tab: 'snapshots' } },
      },
      {
        path: '/preservation/move',
        redirect: (to) => ({ name: 'adminPreservation', query: { ...to.query, tab: 'move' } }),
      },
      {
        path: '/app',
        redirect: { name: 'askJooby' },
        component: () => import('../components/AppLayout.vue'),
        meta: { requiresAuth: true },
        children: [
          {
            path: '/tools',
            name: 'tools',
            component: () => import('../pages/ToolsPage.vue'),
            meta: { titleKey: 'nav.tools' },
          },
          {
            path: '/models',
            name: 'models',
            component: () => import('../pages/ModelsPage.vue'),
            meta: { titleKey: 'nav.models' },
          },
          {
            path: '/override',
            name: 'override',
            component: () => import('../pages/OverridePage.vue'),
            meta: { titleKey: 'nav.override' },
          },
          {
            path: '/cases',
            name: 'cases',
            component: () => import('../pages/CasesPage.vue'),
            meta: { titleKey: 'nav.cases' },
          },
          {
            path: '/cases/:caseId(\\d+)',
            name: 'caseDetail',
            component: () => import('../pages/CaseDetailPage.vue'),
            meta: { titleKey: 'nav.cases', menuRoute: 'cases' },
          },
          {
            path: '/cases/queue-health',
            redirect: (to) => ({ path: '/cases', query: { ...to.query, tab: 'health' } }),
          },
          {
            path: '/cases/quality',
            redirect: (to) => ({ path: '/cases', query: { ...to.query, tab: 'quality' } }),
          },
          {
            path: '/cases/supervision',
            redirect: (to) => ({ path: '/cases', query: { ...to.query, tab: 'supervision' } }),
          },
          {
            path: '/sanctions',
            name: 'sanctions',
            component: () => import('../pages/SanctionsPage.vue'),
            meta: { titleKey: 'nav.sanctions' },
          },
          {
            path: '/watcher',
            name: 'watcher',
            component: () => import('../pages/WatcherPage.vue'),
            meta: { titleKey: 'nav.watcher' },
          },
          {
            path: '/visualisation',
            name: 'visualisation',
            component: () => import('../pages/VisualisationDirectoryPage.vue'),
            meta: { titleKey: 'nav.visualisation' },
          },
          {
            path: '/visualisation/:guid',
            name: 'visualisationView',
            component: () => import('../pages/VisualisationViewPage.vue'),
            meta: { titleKey: 'nav.visualisation' },
          },
          {
            path: '/administration/tenants',
            name: 'adminTenants',
            component: () => import('../pages/AdminTenantsPage.vue'),
            meta: { titleKey: 'nav.adminTenants' },
          },
          {
            path: '/administration/security',
            name: 'adminSecurity',
            component: () => import('../pages/AdminSecurityPage.vue'),
            meta: { titleKey: 'nav.adminSecurity' },
          },
          {
            path: '/administration/performance',
            name: 'adminPerformance',
            component: () => import('../pages/AdminPerformancePage.vue'),
            meta: { titleKey: 'nav.adminPerformance' },
          },
          {
            path: '/administration/visualisations',
            name: 'adminVisualisations',
            component: () => import('../pages/AdminVisualisationsPage.vue'),
            meta: { titleKey: 'nav.adminVisualisations' },
          },
          {
            path: '/administration/telemetry',
            name: 'adminTelemetry',
            component: () => import('../pages/AdminTelemetryPage.vue'),
            meta: { titleKey: 'nav.adminTelemetry' },
          },
          {
            path: '/administration/sanctions-loader',
            name: 'adminSanctionsLoader',
            component: () => import('../pages/AdminSanctionsLoaderPage.vue'),
            meta: { titleKey: 'nav.adminSanctionsLoader' },
          },
          {
            path: '/administration/preservation',
            name: 'adminPreservation',
            component: () => import('../pages/AdminPreservationPage.vue'),
            meta: { titleKey: 'nav.adminPreservation' },
          },
          {
            path: '/ask-jooby',
            name: 'askJooby',
            component: () => import('../pages/AskJoobyPage.vue'),
            meta: { titleKey: 'nav.askJooby' },
          },
        ],
      },
      {
        path: '/scaffold',
        component: () => import('../components/ScaffoldLayout.vue'),
        children: [
          { path: '', name: 'home', component: () => import('../pages/HomePage.vue') },
          { path: 'grid', name: 'grid', component: () => import('../pages/GridPage.vue') },
          { path: 'api-demo', name: 'api', component: () => import('../pages/ApiPage.vue') },
          { path: 'theme', name: 'theme', component: () => import('../pages/ThemePage.vue') },
        ],
      },
      {
        path: '/:pathMatch(.*)*',
        name: 'notFound',
        component: () => import('../pages/NotFoundPage.vue'),
      },
    ],
  })

  router.beforeEach(async (to) => {
    if (!to.matched.some((record) => record.meta.requiresAuth)) return true

    try {
      await client.fetchQuery(meQueryOptions)

      return true
    } catch (error) {
      if (error instanceof ApiError && error.kind === 'unauthorized') {
        return { name: 'login', query: { redirect: to.fullPath } }
      }

      throw error
    }
  })

  return router
}

export default createAppRouter(createWebHistory(import.meta.env.BASE_URL))
