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
import { Check, Circle } from 'lucide-vue-next'
import {
  passwordMaxLength,
  passwordMinLength,
  type PasswordRuleKey,
} from '../../auth/passwordPolicy'

const props = defineProps<{ failed: PasswordRuleKey[]; touched: boolean }>()
const { t } = useI18n()

const rules = computed<{ key: PasswordRuleKey; text: string }[]>(() => [
  { key: 'minLength', text: t('login.rules.minLength', { count: passwordMinLength }) },
  { key: 'maxLength', text: t('login.rules.maxLength', { count: passwordMaxLength }) },
  { key: 'uppercase', text: t('login.rules.uppercase') },
  { key: 'lowercase', text: t('login.rules.lowercase') },
  { key: 'number', text: t('login.rules.number') },
  { key: 'special', text: t('login.rules.special') },
  { key: 'repeat', text: t('login.rules.repeat') },
  { key: 'common', text: t('login.rules.common') },
])
</script>

<template>
  <section class="rounded-field border border-base-300 bg-base-200 p-3" data-testid="rules">
    <h2 class="mb-2 text-sm font-semibold">{{ t('login.rules.title') }}</h2>
    <ul class="flex flex-col gap-1 text-sm">
      <li v-for="rule in rules" :key="rule.key" class="flex items-start gap-2">
        <Check
          v-if="props.touched && !props.failed.includes(rule.key)"
          aria-hidden="true"
          class="mt-0.5 size-4 shrink-0"
        />
        <Circle v-else aria-hidden="true" class="mt-0.5 size-4 shrink-0 opacity-60" />
        <span>{{ rule.text }}</span>
        <span class="sr-only">
          {{
            props.touched && !props.failed.includes(rule.key)
              ? t('login.rules.met')
              : t('login.rules.unmet')
          }}
        </span>
      </li>
    </ul>
  </section>
</template>
