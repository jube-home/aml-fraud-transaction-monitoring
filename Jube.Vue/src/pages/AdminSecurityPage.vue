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
import { useQuery } from '@tanstack/vue-query'
import { ShieldAlert } from 'lucide-vue-next'
import { hasPermission, meQueryOptions } from '../auth/me'
import type { AreaRoot } from '../areaStory/types'
import AreaStory from '../components/AreaStory.vue'
import { useStoryPath } from '../composables/useStoryPath'
import PayloadGrid from '../components/PayloadGrid.vue'
import PermissionsBeat from '../components/PermissionsBeat.vue'
import RolePermissionsCard from '../components/RolePermissionsCard.vue'
import UserAccessCard from '../components/UserAccessCard.vue'
import UserApiKeysCard from '../components/UserApiKeysCard.vue'
import UserRoleCard from '../components/UserRoleCard.vue'
import ApiKeyPanel from '../components/ApiKeyPanel.vue'
import {
  allowedRoots,
  securityPermissions,
  securityRoles,
  type SecurityRootId,
} from '../security/roots'
import { accessOf } from '../userAccess/model'

const { t } = useI18n()
const me = useQuery(meQueryOptions)

const { start, generation, remember } = useStoryPath()

const texts = computed<Record<SecurityRootId, { title: string; singular: string; hint: string }>>(
  () => ({
    users: {
      title: t('security.users.title'),
      singular: t('security.users.singular'),
      hint: t('security.users.hint'),
    },
    roles: {
      title: t('security.roles.title'),
      singular: t('security.roles.singular'),
      hint: t('security.roles.hint'),
    },
    permissions: {
      title: t('security.permissions.title'),
      singular: t('security.permissions.singular'),
      hint: t('security.permissions.hint'),
    },
    logins: {
      title: t('security.logins.title'),
      singular: t('security.logins.singular'),
      hint: t('security.logins.hint'),
    },
    logouts: {
      title: t('security.logouts.title'),
      singular: t('security.logouts.singular'),
      hint: t('security.logouts.hint'),
    },
  }),
)

const roots = computed<AreaRoot[]>(() =>
  allowedRoots(me.data.value).map((root) => ({
    id: root.id,
    area: root.area,
    extras: root.id === 'users' || root.id === 'roles',
    ...texts.value[root.id],
  })),
)

const views = computed<Record<string, string>>(() => ({
  permissions: t('security.permissions.title'),
  apiKeys: t('security.apiKeys.title'),
}))

const canPermissions = computed(() => hasPermission(me.data.value, securityPermissions))
const canRoles = computed(() => hasPermission(me.data.value, securityRoles))
</script>

<template>
  <section class="flex min-w-0 flex-col gap-4" data-testid="adminSecurity-page">
    <h1 class="sr-only">{{ t('nav.adminSecurity') }}</h1>

    <div
      v-if="me.isPending.value"
      key="loading"
      class="flex flex-col gap-3"
      role="status"
      data-testid="security-loading"
    >
      <span class="sr-only">{{ t('security.loading') }}</span>
      <div class="skeleton h-10 w-full"></div>
      <div class="skeleton h-48 w-full"></div>
    </div>

    <div
      v-else-if="me.isError.value || roots.length === 0"
      key="denied"
      role="alert"
      class="alert alert-error"
      data-testid="security-denied"
    >
      <ShieldAlert class="size-5" aria-hidden="true" />
      <span>{{ t('security.denied') }}</span>
    </div>

    <AreaStory
      v-else
      :key="generation"
      :path="start"
      :title="t('nav.adminSecurity')"
      :roots="roots"
      :views="views"
      @path="remember"
    >
      <template #extra="{ rootId, record, openView, goto }">
        <div class="grid grid-cols-[repeat(auto-fill,minmax(15rem,1fr))] gap-3">
          <RolePermissionsCard
            v-if="rootId === 'roles' && canPermissions"
            key="permissions"
            :role-registry-id="record.id"
            @open="openView('permissions')"
          />
          <UserRoleCard
            v-if="rootId === 'users'"
            key="role"
            :role-guid="
              typeof record.values.roleRegistryGuid === 'string'
                ? record.values.roleRegistryGuid
                : undefined
            "
            :can-open="canRoles"
            @open="(roleId) => goto('roles', roleId)"
          />
          <UserApiKeysCard
            v-if="rootId === 'users'"
            key="apiKeys"
            :user-registry-id="record.id"
            @open="openView('apiKeys')"
          />
          <UserAccessCard
            v-if="rootId === 'users'"
            key="access"
            :user-registry-id="record.id"
            :user-name="record.name"
            v-bind="accessOf(record.values)"
          />
        </div>
      </template>

      <template #view="{ rootId, view, record, setDirty }">
        <ApiKeyPanel
          v-if="rootId === 'users' && view === 'apiKeys' && record"
          :key="`apiKeys-${record.id}`"
          :user-registry-id="record.id"
        />
        <PermissionsBeat
          v-else-if="rootId === 'permissions' || (rootId === 'roles' && record)"
          :key="record?.id ?? 'all'"
          :role-registry-id="record?.id"
          @dirty="setDirty"
        />
        <PayloadGrid v-else-if="rootId === 'logins'" area="UserLogin" />
        <PayloadGrid v-else-if="rootId === 'logouts'" area="UserLogout" />
      </template>
    </AreaStory>
  </section>
</template>
