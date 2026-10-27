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
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { AlertCircle, ArrowLeft, KeyRound, User } from 'lucide-vue-next'
import { oauthAddress } from '../../auth/methods'
import { useSignInWizard, type SignInProblem } from '../../composables/useSignInWizard'
import PasswordChecklist from './PasswordChecklist.vue'
import PasswordField from './PasswordField.vue'

const props = defineProps<{ returnAddress: string }>()
const emit = defineEmits<{ signedIn: [] }>()
const { t } = useI18n()

const {
  form,
  step,
  path,
  busy,
  problem,
  failedRules,
  wirePasswordHash,
  checking,
  redirecting,
  negotiated,
  blocked,
  start,
  submit,
  startAgain,
} = useSignInWizard(() => emit('signedIn'))

onMounted(() => {
  void start(props.returnAddress)
})

const formElement = ref<HTMLFormElement>()

const stepLabels = computed(() => ({
  credentials: t('login.steps.credentials'),
  mfa: t('login.steps.mfa'),
  password: t('login.steps.password'),
}))

const heading = computed(() => {
  if (step.value === 'mfa') return t('login.mfa.title')
  if (step.value === 'password') return t('login.change.title')

  return t('login.title')
})

const submitLabel = computed(() => {
  if (busy.value) return t('login.submitting')
  if (step.value === 'mfa') return t('login.mfa.submit')
  if (step.value === 'password') return t('login.change.submit')

  return t('login.submit')
})

function problemText(value: SignInProblem): string {
  switch (value.kind) {
    case 'invalid':
      return t('login.errors.invalid')
    case 'mismatch':
      return t('login.errors.mismatch')
    case 'weak':
      return t('login.errors.weak')
    case 'rejected':
      return t('login.errors.rejected')
    default:
      return t('login.errors.unavailable')
  }
}

watch(step, async () => {
  await nextTick()
  formElement.value?.querySelector<HTMLInputElement>('input')?.focus()
})

watch(busy, async (working) => {
  if (working || problem.value?.kind !== 'invalid') return

  await nextTick()
  formElement.value
    ?.querySelector<HTMLInputElement>(step.value === 'mfa' ? '#login-mfa' : '#login-password')
    ?.focus()
})
</script>

<template>
  <div v-if="checking" class="flex flex-col gap-4" role="status" data-testid="login-checking">
    <h1 class="text-2xl font-semibold text-primary">{{ t('login.title') }}</h1>
    <p>{{ redirecting ? t('login.redirecting') : t('login.checking') }}</p>
    <div class="skeleton h-10 w-full"></div>
    <a
      v-if="redirecting"
      class="btn btn-primary btn-sm"
      :href="oauthAddress(props.returnAddress)"
      data-testid="login-oauth-link"
    >
      {{ t('login.continueToProvider') }}
    </a>
  </div>
  <div v-else-if="blocked" class="flex flex-col gap-4" data-testid="login-blocked">
    <h1 class="text-2xl font-semibold text-primary">{{ t('login.title') }}</h1>
    <div role="alert" class="alert alert-error">
      <AlertCircle aria-hidden="true" class="size-5 shrink-0" />
      <span>{{ t('login.negotiateFailed') }}</span>
    </div>
    <button
      type="button"
      class="btn btn-primary btn-sm self-start"
      data-testid="login-retry"
      @click="start(props.returnAddress)"
    >
      {{ t('login.retry') }}
    </button>
  </div>
  <div v-else class="flex flex-col gap-5">
    <ul class="steps w-full" :aria-label="t('login.stepsLabel')">
      <li
        v-for="(id, index) in path"
        :key="id"
        class="step step-primary text-sm"
        :aria-current="index === path.length - 1 ? 'step' : undefined"
      >
        {{ stepLabels[id] }}
      </li>
    </ul>

    <h1 class="text-2xl font-semibold text-primary">{{ heading }}</h1>

    <p v-if="step === 'credentials'">{{ t('login.subtitle') }}</p>
    <p v-else-if="step === 'mfa'">{{ t('login.mfa.intro') }}</p>
    <p v-else>{{ t('login.change.intro') }}</p>

    <div
      v-if="problem"
      role="alert"
      class="alert alert-error alert-vertical items-start sm:alert-horizontal"
      data-testid="login-problem"
    >
      <AlertCircle aria-hidden="true" class="size-5 shrink-0" />
      <div class="flex flex-col gap-1">
        <span>{{ problemText(problem) }}</span>
        <ul v-if="problem.kind === 'rejected' && problem.messages.length" class="list-disc ps-5">
          <li v-for="message in problem.messages" :key="message">{{ message }}</li>
        </ul>
      </div>
    </div>

    <form ref="formElement" class="flex flex-col gap-4" @submit.prevent="submit">
      <fieldset v-if="step === 'credentials'" class="fieldset gap-4 p-0" :disabled="busy">
        <div class="flex flex-col gap-1">
          <label class="label" for="login-user-name">{{ t('login.userName') }}</label>
          <label class="input input-sm w-full">
            <User aria-hidden="true" class="size-4 opacity-70" />
            <input
              id="login-user-name"
              v-model="form.userName"
              type="text"
              autocomplete="username"
              class="grow"
              required
              spellcheck="false"
              autocapitalize="off"
            />
          </label>
        </div>
        <PasswordField
          id="login-password"
          v-model="form.password"
          :label="t('login.password')"
          autocomplete="current-password"
        />
      </fieldset>

      <fieldset v-else-if="step === 'mfa'" class="fieldset gap-1 p-0" :disabled="busy">
        <label class="label" for="login-mfa">{{ t('login.mfa.label') }}</label>
        <label class="input input-lg validator w-full">
          <KeyRound aria-hidden="true" class="size-5 opacity-70" />
          <input
            id="login-mfa"
            v-model="form.mfa"
            type="text"
            inputmode="numeric"
            autocomplete="one-time-code"
            maxlength="8"
            pattern="[0-9]{4,8}"
            class="grow font-mono tracking-[0.4em] tabular-nums"
            required
            :title="t('login.mfa.hint')"
          />
        </label>
        <p class="validator-hint">{{ t('login.mfa.hint') }}</p>
      </fieldset>

      <fieldset v-else class="fieldset gap-4 p-0" :disabled="busy">
        <PasswordField
          id="login-new-password"
          v-model="form.newPassword"
          :label="t('login.change.newPassword')"
          autocomplete="new-password"
        />
        <PasswordChecklist
          v-if="wirePasswordHash"
          :failed="failedRules"
          :touched="form.newPassword.length > 0"
        />
        <PasswordField
          id="login-repeat-password"
          v-model="form.repeatNewPassword"
          :label="t('login.change.repeatPassword')"
          autocomplete="new-password"
        />
      </fieldset>

      <div class="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
        <button
          v-if="step !== 'credentials' && !negotiated"
          type="button"
          class="btn btn-sm"
          :disabled="busy"
          @click="startAgain"
        >
          <ArrowLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
          {{ t('login.back') }}
        </button>
        <button type="submit" class="btn btn-primary btn-sm sm:ms-auto" :disabled="busy">
          <span v-if="busy" class="loading loading-spinner loading-xs" aria-hidden="true"></span>
          {{ submitLabel }}
        </button>
      </div>
    </form>
  </div>
</template>
