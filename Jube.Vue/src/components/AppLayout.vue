<!-- Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { BookOpen, Menu, TriangleAlert } from 'lucide-vue-next'
import { meQueryOptions } from '../auth/me'
import { useSessionActions } from '../auth/session'
import { useSessionWarning } from '../auth/useSessionWarning'
import { startAppEvents, stopAppEvents } from '../events/appEvents'
import { parseNotification, useNotificationStore } from '../notifications/notifications'
import { visibleMenu } from '../layout/menu'
import { useJoobyContextStore } from '../askjooby/pageContext'
import { noteSessionExpiry, sessionExpiresAt } from '../auth/sessionExpiry'
import { legacyUrl } from '../layout/legacyUrl'
import AccountMenu from './AccountMenu.vue'
import SideMenuItem from './SideMenuItem.vue'
import NotificationBell from './NotificationBell.vue'
import TenantSwitcher from './TenantSwitcher.vue'
import JoobyLauncher from './askjooby/JoobyLauncher.vue'
import JoobyWidget from './askjooby/JoobyWidget.vue'

const { t } = useI18n()
const { expire } = useSessionActions()
const { data: me } = useQuery(meQueryOptions)
const route = useRoute()
const client = useQueryClient()
const notifications = useNotificationStore()
const joobyContext = useJoobyContextStore()
const session = useSessionWarning()

const showWidget = computed(() => me.value !== undefined && route.name !== 'askJooby')
const pageLabel = computed(() => {
  const key = route.meta.titleKey

  return key ? t(key) : ''
})

watch(
  () => route.name,
  () => {
    if (route.name !== 'askJooby') joobyContext.set({ pageName: String(route.name ?? 'default') })
  },
  { immediate: true },
)

watch(
  pageLabel,
  (label) => {
    document.title = label ? `${label} - ${t('app.name')}` : t('app.name')
  },
  { immediate: true },
)

onMounted(() => {
  startAppEvents({
    client,
    onNotification: (payload) => {
      const notification = parseNotification(payload)

      if (notification) notifications.receive(notification)
    },
    onRevoked: () => void expire(),
  })
})

const items = computed(() => visibleMenu(me.value))

const sidebarKey = 'jube.sidebar'
const readSidebar = (): boolean => {
  try {
    return localStorage.getItem(sidebarKey) !== 'closed'
  } catch {
    return true
  }
}
const sidebarOpen = ref(readSidebar())
const mobileOpen = ref(false)

function toggleSidebar(): void {
  if (typeof matchMedia === 'function' && matchMedia('(min-width: 64rem)').matches) {
    sidebarOpen.value = !sidebarOpen.value

    try {
      localStorage.setItem(sidebarKey, sidebarOpen.value ? 'open' : 'closed')
    } catch {
      return
    }

    return
  }

  mobileOpen.value = !mobileOpen.value
}

watch(
  () => route.fullPath,
  () => {
    mobileOpen.value = false
  },
)
const maxTimer = 2 ** 31 - 1

let timer: ReturnType<typeof setTimeout> | undefined

watch(
  me,
  (value) => {
    noteSessionExpiry(value?.sessionExpiry)
  },
  { immediate: true },
)

watch(
  sessionExpiresAt,
  (at) => {
    clearTimeout(timer)

    if (at === undefined) return

    timer = setTimeout(() => void expire(), Math.min(Math.max(at - Date.now(), 0), maxTimer))
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  clearTimeout(timer)
  stopAppEvents()
})
</script>

<template>
  <div class="drawer" :class="{ 'lg:drawer-open': sidebarOpen }" @keydown.esc="mobileOpen = false">
    <input
      id="app-drawer"
      type="checkbox"
      class="drawer-toggle"
      aria-hidden="true"
      tabindex="-1"
      :checked="mobileOpen"
      @change="mobileOpen = ($event.target as HTMLInputElement).checked"
    />
    <div class="drawer-content flex h-dvh min-h-0 flex-col bg-base-200 text-base-content">
      <a
        href="#main"
        class="btn btn-primary btn-sm absolute -translate-y-full focus:translate-y-0 focus:z-50"
      >
        {{ t('app.skipToContent') }}
      </a>
      <header class="navbar gap-2 border-b-2 border-accent bg-base-100 px-4">
        <div class="navbar-start w-auto flex-none gap-1">
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-square"
            :aria-label="t('shell.toggleMenu')"
            :aria-expanded="mobileOpen || sidebarOpen"
            aria-controls="app-sidebar"
            data-testid="sidebar-toggle"
            @click="toggleSidebar"
          >
            <Menu aria-hidden="true" class="size-5" />
          </button>
          <RouterLink :to="{ name: 'askJooby' }" class="px-1 py-2">
            <span
              role="img"
              :aria-label="t('app.name')"
              class="block aspect-[1640/616] h-6 bg-(--color-logo) mask-[url(/jube-wordmark.png)] mask-contain mask-center mask-no-repeat"
            ></span>
          </RouterLink>
        </div>
        <span
          v-if="pageLabel"
          class="badge badge-soft badge-primary hidden font-mono md:inline-flex"
          data-testid="page-label"
          >{{ pageLabel }}</span
        >
        <div class="navbar-end flex-1 gap-1">
          <JoobyLauncher v-if="showWidget" />
          <NotificationBell />
          <button
            type="button"
            class="btn btn-ghost btn-sm btn-square"
            popovertarget="resources-menu"
            style="anchor-name: --resources-menu"
            :aria-label="t('shell.resources')"
          >
            <BookOpen aria-hidden="true" class="size-5" />
          </button>
          <ul
            id="resources-menu"
            popover
            class="dropdown dropdown-end menu z-50 mt-2 w-52 rounded-box bg-base-100 p-2 shadow-md"
            style="position-anchor: --resources-menu"
          >
            <li>
              <a href="https://www.jube.io" target="_blank" rel="noopener">{{
                t('shell.documentation')
              }}</a>
            </li>
            <li>
              <a :href="legacyUrl('/Copyright')">{{ t('shell.copyright') }}</a>
            </li>
          </ul>
          <AccountMenu v-if="me" :me="me" />
          <TenantSwitcher v-if="me?.landlord" :current="me.tenantRegistryId" />
        </div>
      </header>
      <div
        v-if="session.warning.value"
        role="alert"
        class="alert alert-warning rounded-none"
        data-testid="session-warning"
      >
        <TriangleAlert aria-hidden="true" class="size-5 shrink-0" />
        <span>{{ t('shell.sessionWarning') }}</span>
        <button
          type="button"
          class="btn btn-sm"
          :disabled="session.extending.value"
          @click="session.extend()"
        >
          {{ t('shell.extendSession') }}
        </button>
      </div>
      <div class="flex min-h-0 flex-1">
        <main id="main" class="min-h-0 min-w-0 flex-1 overflow-y-auto p-4">
          <RouterView />
        </main>
        <JoobyWidget v-if="showWidget" />
      </div>
    </div>
    <div class="drawer-side z-40 transition-none **:transition-none">
      <div
        v-show="mobileOpen"
        class="drawer-overlay"
        aria-hidden="true"
        @click="mobileOpen = false"
      ></div>
      <aside id="app-sidebar" class="min-h-full w-72 border-e border-base-300 bg-base-100">
        <nav :aria-label="t('nav.label')">
          <ul class="menu w-full p-2">
            <SideMenuItem v-for="node in items" :key="node.key" :node="node" />
          </ul>
        </nav>
      </aside>
    </div>
  </div>
</template>
