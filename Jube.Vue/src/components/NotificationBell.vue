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
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Bell, CheckCheck } from 'lucide-vue-next'
import { formatTimestamp } from '../askjooby/format'
import { useNotificationStore } from '../notifications/notifications'

const { t } = useI18n()
const notifications = useNotificationStore()

const severityLabels = computed(() => ({
  info: t('notifications.severity.info'),
  warning: t('notifications.severity.warning'),
  error: t('notifications.severity.error'),
}))
</script>

<template>
  <div class="indicator">
    <span
      v-if="notifications.unread > 0"
      class="indicator-item badge badge-xs badge-error"
      data-testid="unread"
      >{{ notifications.unread }}</span
    >
    <button
      type="button"
      class="btn btn-ghost btn-sm btn-square"
      popovertarget="notifications-menu"
      style="anchor-name: --notifications-menu"
      :aria-label="
        notifications.unread > 0
          ? t('notifications.labelUnread', { count: notifications.unread }, notifications.unread)
          : t('notifications.label')
      "
      data-testid="notifications-button"
    >
      <Bell aria-hidden="true" class="size-5" />
    </button>
  </div>
  <div
    id="notifications-menu"
    popover
    class="dropdown dropdown-end card card-sm z-50 mt-2 w-80 bg-base-100 shadow-md"
    style="position-anchor: --notifications-menu"
  >
    <div class="card-body gap-2">
      <div class="flex items-center justify-between">
        <h2 class="font-semibold">{{ t('notifications.title') }}</h2>
        <button
          v-if="notifications.unread > 0"
          type="button"
          class="btn btn-ghost btn-xs"
          @click="notifications.markAllRead()"
        >
          <CheckCheck aria-hidden="true" class="size-4" />
          {{ t('notifications.markAllRead') }}
        </button>
      </div>
      <p v-if="notifications.items.length === 0" class="text-sm" data-testid="notifications-empty">
        {{ t('notifications.empty') }}
      </p>
      <ul v-else class="flex max-h-80 flex-col gap-2 overflow-y-auto">
        <li
          v-for="notification in notifications.items"
          :key="notification.id"
          class="rounded-field border border-base-300 p-2 text-sm"
        >
          <button
            type="button"
            class="w-full text-start"
            @click="notifications.markRead(notification.id)"
          >
            <span class="flex items-center justify-between gap-2">
              <strong :class="{ 'font-normal': notification.read }">{{
                notification.title
              }}</strong>
              <span class="flex items-center gap-1">
                <span
                  class="badge badge-xs font-mono"
                  :class="{
                    'badge-error': notification.severity === 'error',
                    'badge-warning': notification.severity === 'warning',
                    'badge-ghost': notification.severity === 'info',
                  }"
                  >{{ severityLabels[notification.severity] }}</span
                >
                <time class="badge badge-ghost badge-xs font-mono">{{
                  formatTimestamp(notification.at)
                }}</time>
              </span>
            </span>
            <span v-if="notification.body" class="block">{{ notification.body }}</span>
            <span v-if="!notification.read" class="sr-only">{{ t('notifications.unread') }}</span>
          </button>
        </li>
      </ul>
    </div>
  </div>
</template>
