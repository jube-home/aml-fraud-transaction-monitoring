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
import { joobyApi } from '../askjooby/api'
import { welcomeFor } from '../askjooby/welcome'
import { useJoobyContextStore } from '../askjooby/pageContext'
import ConversationList from '../components/askjooby/ConversationList.vue'
import JoobyChat from '../components/askjooby/JoobyChat.vue'
import { useJooby } from '../components/askjooby/useJooby'

const { t } = useI18n()
const context = useJoobyContextStore()

context.set({ pageName: 'AskJooby' })

const { controller, offline, landlord, guard } = useJooby(() => '', 'static')

const { data: stats } = useQuery({
  queryKey: ['askjooby', 'startupStats'],
  queryFn: () => joobyApi.startupStats(),
  staleTime: 5 * 60_000,
  retry: false,
})

const welcome = computed(() => welcomeFor(stats.value, offline))
const state = controller.state

const open = (id: number): Promise<void> => guard(() => controller.openConversation(id))
const create = (): Promise<void> => guard(() => controller.newConversation(true))
const remove = (id: number): Promise<void> => guard(() => controller.removeConversation(id))
</script>

<template>
  <section class="flex h-full min-h-0 flex-col gap-3">
    <h1 class="sr-only">{{ t('nav.askJooby') }}</h1>
    <p v-if="offline" role="note" class="alert alert-info alert-soft" data-testid="offline">
      {{ t('askJooby.offline') }}
    </p>
    <div class="grid min-h-0 flex-1 gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <ConversationList
        :selected-id="state.conversationId"
        :can-create="state.conversationId !== null && !state.empty"
        @select="open"
        @create="create"
        @remove="remove"
      />
      <JoobyChat
        :controller="controller"
        :landlord="landlord"
        :offline="offline"
        :attachments="true"
        :welcome="welcome"
      />
    </div>
  </section>
</template>
