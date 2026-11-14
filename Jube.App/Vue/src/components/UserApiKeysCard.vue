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
import {KeyRound} from 'lucide-vue-next'
import {queryKeys} from '../api/queryKeys'
import {apiKeyApi} from '../apiKeys/api'
import HubTile from './HubTile.vue'

const props = defineProps<{ userRegistryId: number }>()

const emit = defineEmits<{ open: [] }>()

const {t} = useI18n()

const keys = useQuery({
  queryKey: computed(() => queryKeys.apiKeys(props.userRegistryId)),
  queryFn: () => apiKeyApi.list(props.userRegistryId),
  retry: false,
})
</script>

<template>
  <HubTile
    id="apiKeys"
    :label="t('security.apiKeys.title')"
    :icon="KeyRound"
    :meta="t('security.apiKeys.cardHint')"
    :count="keys.isSuccess.value ? (keys.data.value?.length ?? 0) : undefined"
    :pending="keys.isPending.value"
    :failed="keys.isError.value ? t('security.apiKeys.unavailable') : undefined"
    @open="emit('open')"
  />
</template>
