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
import {UserRoundCog} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {formModelApi} from '../formModel/api'
import HubTile from './HubTile.vue'

const props = defineProps<{ roleGuid: string | undefined; canOpen: boolean }>()

const emit = defineEmits<{ open: [roleId: number] }>()

const {t} = useI18n()

const model = useQuery({
  queryKey: computed(() => queryKeys.formModel('RoleRegistry')),
  queryFn: () => formModelApi.model('RoleRegistry'),
  staleTime: 30_000,
  retry: false,
})

const roles = useQuery({
  queryKey: computed(() => queryKeys.formRecords('RoleRegistry', undefined)),
  queryFn: () => formModelApi.records(model.data.value!, undefined),
  enabled: computed(() => model.data.value !== undefined),
  retry: false,
})

const role = computed(() => (roles.data.value ?? []).find((row) => row.guid === props.roleGuid))

const roleName = computed(() =>
  typeof role.value?.name === 'string' ? role.value.name : t('security.roleCard.none'),
)
</script>

<template>
  <HubTile
    id="role"
    :label="t('security.roleCard.title')"
    :icon="UserRoundCog"
    :meta="t('security.roleCard.hint')"
    :pending="roles.isPending.value"
    :pill="roles.isPending.value ? undefined : roleName"
    :openable="role !== undefined && canOpen"
    @open="role && emit('open', role.id as number)"
  />
</template>
