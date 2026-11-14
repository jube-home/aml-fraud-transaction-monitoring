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
import {Cog} from 'lucide-vue-next'
import {computed} from 'vue'
import {useI18n} from 'vue-i18n'
import {avatarOf} from './noteAvatar'
import type {CaseNoteEntry} from './types'

const props = defineProps<{ note: CaseNoteEntry }>()

const {t} = useI18n()

const avatar = computed(() => avatarOf(props.note))

const label = computed(() =>
  avatar.value.kind === 'askJooby'
    ? t('caseDetail.notes.avatar.askJooby')
    : avatar.value.kind === 'system'
      ? t('caseDetail.notes.avatar.system')
      : (avatar.value.name ?? t('caseDetail.notes.avatar.unknown')),
)
</script>

<template>
  <div
    v-if="avatar.kind === 'askJooby'"
    key="askJooby"
    class="avatar"
    role="img"
    :aria-label="label"
    :title="label"
    data-testid="case-note-avatar"
    data-kind="askJooby"
  >
    <div class="w-8 rounded-full bg-base-300">
      <img
        src="/images/askjooby-avatar.png"
        alt=""
        class="size-full object-cover"
        width="32"
        height="32"
      />
    </div>
  </div>
  <div
    v-else
    key="placeholder"
    class="avatar avatar-placeholder"
    role="img"
    :aria-label="label"
    :title="label"
    data-testid="case-note-avatar"
    :data-kind="avatar.kind"
  >
    <div class="w-8 rounded-full bg-base-300 text-base-content">
      <Cog v-if="avatar.kind === 'system'" class="size-4" aria-hidden="true"/>
      <span v-else class="text-xs font-semibold" aria-hidden="true">{{ avatar.letter }}</span>
    </div>
  </div>
</template>
