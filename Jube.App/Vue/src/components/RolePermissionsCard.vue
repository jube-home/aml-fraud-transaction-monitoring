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
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {useQuery} from '@tanstack/vue-query'
import {ShieldCheck} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {permissionApi} from '../permissions/api'
import HubTile from './HubTile.vue'

const props = defineProps<{ roleRegistryId: number }>()

const emit = defineEmits<{ open: [] }>()

const {t} = useI18n()

const grants = useQuery({
  queryKey: computed(() => queryKeys.permissionGrants(props.roleRegistryId)),
  queryFn: () => permissionApi.grants(props.roleRegistryId),
  retry: false,
})

const granted = computed(() => (grants.data.value ?? []).filter((grant) => grant.active).length)
</script>

<template>
  <HubTile
    id="permissions"
    :label="t('security.permissions.title')"
    :icon="ShieldCheck"
    :meta="t('security.permissionsCard.hint')"
    :count="grants.isSuccess.value ? granted : undefined"
    :pending="grants.isPending.value"
    @open="emit('open')"
  />
</template>
