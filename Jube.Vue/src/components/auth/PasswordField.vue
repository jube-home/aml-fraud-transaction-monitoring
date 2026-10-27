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
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Eye, EyeOff, LockKeyhole } from 'lucide-vue-next'

defineProps<{
  id: string
  label: string
  autocomplete: 'current-password' | 'new-password'
}>()

const model = defineModel<string>({ required: true })
const { t } = useI18n()
const revealed = ref(false)
</script>

<template>
  <div class="flex flex-col gap-1">
    <label class="label" :for="id">{{ label }}</label>
    <label class="input input-sm validator w-full">
      <LockKeyhole aria-hidden="true" class="size-4 opacity-70" />
      <input
        :id="id"
        v-model="model"
        :type="revealed ? 'text' : 'password'"
        :autocomplete="autocomplete"
        class="grow"
        required
        spellcheck="false"
        autocapitalize="off"
      />
      <button
        type="button"
        class="btn btn-ghost btn-xs btn-square"
        :aria-pressed="revealed"
        :aria-label="revealed ? t('login.hide') : t('login.show')"
        @click="revealed = !revealed"
      >
        <EyeOff v-if="revealed" aria-hidden="true" class="size-4" />
        <Eye v-else aria-hidden="true" class="size-4" />
      </button>
    </label>
  </div>
</template>
